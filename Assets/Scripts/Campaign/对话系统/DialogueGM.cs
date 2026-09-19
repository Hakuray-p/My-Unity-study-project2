using System;
using UnityEngine;
using UnityEngine.UI;

// 所有 NPC 对话的统一管理器，负责对话框显示、按钮行为和事件分支。
public sealed class DialogueGM : MonoBehaviour
{
    public const float InteractionDistance = 2.2f; // 交谈和指引共用的交互距离
    [SerializeField] private Button tutorialButton; // 猫姬画布中预置的重学按钮
    [SerializeField] private float dialogueCloseDistance = 3.5f; // 走开多远自动关掉对话
    private CharacterGM characterGM; // 角色管理器
    private CampaignSession session; // 当前存档会话
    private EventGM eventGM; // 事件管理器
    private Action<string> startMatchAction; // 开比赛的入口，由场景 GM 提供
    private Action openShopAction; // 打开商店的入口，由场景 GM 提供
    private Action<string> showStatusAction; // 显示状态提示的回调
    private Action closePanelsAction; // 关闭其它面板的回调
    private WorldInteractionActor dialogueActor; // 当前交谈对象，不随说话者切换
    private bool dialogueOpen; // 对话框是否打开
    private DialogueState dialogueState; // 当前事件对话处在哪个阶段
    private DialogueManager dialogueManager; // 当前场景共用的逐句文字播放器

    // 事件对话当前处在哪个阶段。
    private enum DialogueState
    {
        Normal, // 普通对话
        EventPrompt, // 事件待接取
        EventActive, // 事件进行中
        EventResolved // 事件已完成
    }

    public bool IsOpen => dialogueOpen; // 当前是否正在交谈
    public WorldInteractionActor NearbyActor { get; private set; } // 当前按 E 可以交谈的最近角色

    // 接入场景模块并绑定已经制作好的对话界面。
    public void Initialize(CharacterGM characterManager, CampaignSession campaignSession, EventGM eventManager,
        Action<string> startMatch, Action openShop, Action<string> showStatus, Action closePanels)
    {
        characterGM = characterManager;
        session = campaignSession;
        eventGM = eventManager;
        startMatchAction = startMatch;
        openShopAction = openShop;
        showStatusAction = showStatus;
        closePanelsAction = closePanels;
        foreach (DialogueManager manager in FindObjectsOfType<DialogueManager>(true))
        {
            if (manager.gameObject.scene != gameObject.scene) continue;
            dialogueManager = manager;
            break;
        }
        BindDialogueCanvases();
        tutorialButton.onClick.AddListener(ReplayTutorial);
        dialogueManager.gameObject.SetActive(true);
    }

    // 胜利回城后自动接上刚才对手的回应，不重复播放已消耗的战后对话。
    public void ResumeVictoryDialogue()
    {
        if (!session.HasPendingBattleReaction || session.LastBattleOutcome != BattleOutcome.PlayerWin) return;
        foreach (WorldInteractionActor actor in characterGM.Actors)
        {
            if (!actor.gameObject.activeInHierarchy) continue;
            if (!Array.Exists(actor.dialogue.matches, challenge => challenge.matchId == session.LastResolvedMatchId)) continue;
            OpenDialogue(actor);
            return;
        }
    }

    // 每帧找最近的可对话角色，按 E 开对话，走远或按 E / Esc 关对话。
    public void Tick()
    {
        if (dialogueOpen)
        {
            bool movedAway = Vector3.Distance(characterGM.Player.position, dialogueActor.transform.position) > dialogueCloseDistance;
            if (movedAway || Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Escape)) Close();
            return;
        }
        NearbyActor = FindNearestActor();
        if (NearbyActor != null && Input.GetKeyDown(KeyCode.E)) OpenDialogue(NearbyActor);
    }

    // 关闭对话并复位状态，取消尚未完成的战前确认。
    public void Close()
    {
        if (!dialogueOpen) return;
        HideOptions();
        dialogueManager.CloseView();
        dialogueActor = null;
        NearbyActor = null;
        dialogueOpen = false;
        dialogueState = DialogueState.Normal;
    }

    // 找玩家附近 2.2 米内最近的交互角色。
    private WorldInteractionActor FindNearestActor()
    {
        WorldInteractionActor nearestActor = null;
        float bestDistance = InteractionDistance;
        foreach (WorldInteractionActor actor in characterGM.Actors)
        {
            if (!actor.gameObject.activeInHierarchy) continue;
            float distance = Vector3.Distance(characterGM.Player.position, actor.transform.position);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                nearestActor = actor;
            }
        }
        return nearestActor;
    }

    // 按明确的角色引用收集对话框并先隐藏。
    private void BindDialogueCanvases()
    {
        foreach (DialogueViewBinding view in dialogueManager.views)
        {
            view.canvas.gameObject.SetActive(false);
            BindDialogueButtons(view.canvas);
        }
    }

    // 给对话框里的功能按钮挂上统一的点击处理。
    private void BindDialogueButtons(Canvas canvas)
    {
        foreach (Button button in canvas.GetComponentsInChildren<Button>(true))
        {
            string name = button.name;
            if (!IsChallengeButtonName(name) && !IsShopButtonName(name) && !IsEventButtonName(name) &&
                !IsLeaveButtonName(name) && !IsChatButtonName(name)) continue;
            Button capturedButton = button;
            button.onClick.AddListener(() => HandleDialogueButton(capturedButton));
        }
    }

    // 按按钮种类分派：离开 / 闲聊 / 事件 / 挑战 / 购买。
    private void HandleDialogueButton(Button button)
    {
        if (!dialogueOpen || dialogueManager.IsBusy) return;
        if (IsLeaveButtonName(button.name))
        {
            closePanelsAction();
            return;
        }
        Canvas canvas = GetDialogueCanvas(dialogueActor);
        if (IsChatButtonName(button.name)) ShowChatDialogue();
        else if (IsEventButtonName(button.name)) HandleEventButton(canvas);
        else if (IsChallengeButtonName(button.name)) StartChallenge();
        else if (IsShopButtonName(button.name))
        {
            Close();
            openShopAction();
        }
    }

    // 打开这个角色对应的对话框。
    private void OpenDialogue(WorldInteractionActor actor)
    {
        NearbyActor = null;
        dialogueActor = actor;
        dialogueOpen = true;
        dialogueState = DialogueState.Normal;
        NpcChallenge lastMatch = Array.Find(actor.dialogue.matches, challenge => challenge.matchId == session.LastResolvedMatchId);
        if (lastMatch != null && session.ConsumeBattleReaction(lastMatch.matchId))
        {
            PlayConversation(actor.dialogue.GetBattleReaction(lastMatch, session.LastBattleOutcome), ReturnToOptions);
            return;
        }
        PlayConversation(actor.dialogue.GetInteractionConversation(session), ReturnToOptions);
    }

    // 隐藏所有业务按钮及其装饰，文字播放期间只接受继续或关闭。
    private void HideOptions()
    {
        foreach (DialogueViewBinding view in dialogueManager.views)
            foreach (Button button in view.canvas.GetComponentsInChildren<Button>(true))
                if (button.gameObject != view.continueArrow) SetDialogueButtonVisible(button, false);
    }

    // 将完整段落交给播放器，只有读完才执行后续行为。
    private void PlayConversation(DialogueLine[] lines, Action finished)
    {
        HideOptions();
        dialogueManager.PlayLines(lines, finished);
    }

    // 把闲聊内容写进对话框。
    private void ShowChatDialogue()
    {
        PlayConversation(dialogueActor.dialogue.GetConversation(session), ReturnToOptions);
    }

    // 回到原 NPC 的业务选项并显示当前挑战及解锁条件。
    private void ReturnToOptions()
    {
        NpcChallenge challenge = dialogueActor.dialogue.GetChallenge(session);
        string message = "还有什么想聊的吗？";
        if (session.HasPendingBattle)
        {
            MatchData pendingMatch = CampaignCatalog.GetMatch(session.State.pendingBattle.matchId);
            message = "未结束对局：" + pendingMatch.displayName + "\n选择继续战斗，保留原来的回合和教学进度。";
        }
        else if (challenge != null)
        {
            MatchData match = CampaignCatalog.GetMatch(challenge.matchId);
            string reason = session.GetMatchLockReason(match);
            message = "可挑战：" + match.displayName + "\n" + (reason == "" ? "准备好了就来一场吧。" : reason);
        }
        ShowOptions(message, () => ConfigureNormalButtons(GetDialogueCanvas(dialogueActor), dialogueActor));
    }

    // 显示当前交谈对象的选项，不把雫误当成业务对象。
    private void ShowOptions(string message, Action shown)
    {
        HideOptions();
        dialogueManager.ShowOptions(dialogueActor.dialogue.speakerId, message, shown);
    }

    // 播放战前对话，读完且未取消时才进入已有赛事流程。
    private void StartChallenge()
    {
        if (TryResumePendingBattle()) return;
        NpcChallenge challenge = dialogueActor.dialogue.GetChallenge(session);
        string reason = session.GetMatchLockReason(CampaignCatalog.GetMatch(challenge.matchId));
        if (reason != "")
        {
            showStatusAction(reason);
            ReturnToOptions();
            return;
        }
        PlayConversation(challenge.beforeMatch, () =>
        {
            string matchId = challenge.matchId;
            Close();
            startMatchAction(matchId);
        });
    }

    // 重学始终进入猫姬练习赛，不改变普通挑战选择的赛事。
    private void ReplayTutorial()
    {
        if (!dialogueOpen || dialogueManager.IsBusy) return;
        if (TryResumePendingBattle()) return;
        NpcChallenge practice = Array.Find(dialogueActor.dialogue.matches, challenge => challenge.matchId == "first_light_practice");
        string reason = session.GetMatchLockReason(CampaignCatalog.GetMatch(practice.matchId));
        if (reason != "")
        {
            showStatusAction(reason);
            return;
        }
        PlayConversation(practice.beforeMatch, () =>
        {
            Close();
            SceneFlowService.StartMatch(practice.matchId, characterGM.Player.position, true);
        });
    }

    // 有未结束的对局时先续战，不重新创建比赛或覆盖教学进度。
    private bool TryResumePendingBattle()
    {
        if (!session.HasPendingBattle) return false;
        string matchId = session.State.pendingBattle.matchId;
        Close();
        startMatchAction(matchId);
        return true;
    }

    // 事件按钮，按待接取 / 进行中 / 已完成三种状态切换文案和按钮。
    private void HandleEventButton(Canvas canvas)
    {
        CampaignEventData data = eventGM.GetEvent(dialogueActor.dialogue.eventId);
        if (dialogueState == DialogueState.EventPrompt)
        {
            eventGM.StartEvent(data.eventId);
            closePanelsAction();
            return;
        }
        if (dialogueState == DialogueState.EventActive)
        {
            if (data.dialogueOnly)
            {
                PlayPersonalEvent(canvas, data);
                return;
            }
            showStatusAction(data.objectiveText);
            Close();
            return;
        }
        if (eventGM.IsResolved(data.eventId))
        {
            dialogueState = DialogueState.EventResolved;
            PlayActorText(data.reviewText, () => ConfigureEventResolved(canvas));
        }
        else if (eventGM.IsActive(data.eventId))
        {
            dialogueState = DialogueState.EventActive;
            PlayActorText(data.objectiveText, () => ConfigureEventActive(canvas));
        }
        else
        {
            dialogueState = DialogueState.EventPrompt;
            if (data.dialogueOnly)
            {
                eventGM.StartEvent(data.eventId);
                PlayPersonalEvent(canvas, data);
                return;
            }
            PlayActorText(data.startText, () => ConfigureEventPrompt(canvas));
        }
    }

    // 播放 NPC 的专属事件并在完整读完后结算事件。
    private void PlayPersonalEvent(Canvas canvas, CampaignEventData data)
    {
        PlayConversation(dialogueActor.dialogue.eventDialogue, () =>
        {
            eventGM.ResolveDialogueEvent(data.eventId);
            dialogueState = DialogueState.EventResolved;
            ConfigureEventResolved(canvas);
        });
    }

    // 按对象名找文字组件写入内容，事件文字也使用同一个逐句播放器。
    private void PlayActorText(string message, Action shown)
    {
        var line = new DialogueLine { speakerId = dialogueActor.dialogue.speakerId, text = message };
        PlayConversation(new[] { line }, () => ShowOptions(message, shown));
    }

    // 普通对话下按角色配置显示挑战、购买、事件等按钮。
    private void ConfigureNormalButtons(Canvas canvas, WorldInteractionActor actor)
    {
        dialogueState = DialogueState.Normal;
        NpcDialogueData data = actor.dialogue;
        tutorialButton.gameObject.SetActive(!session.HasPendingBattle &&
            Array.Exists(data.matches, challenge => challenge.matchId == "first_light_practice"));
        bool hasEvent = !string.IsNullOrEmpty(data.eventId);
        SetDialogueButtonVisible(FindButton(canvas, IsChallengeButtonName), data.matches.Length > 0);
        UiTool.SetButtonText(FindButton(canvas, IsChallengeButtonName), session.HasPendingBattle ? "继续战斗" : "挑战");
        SetDialogueButtonVisible(FindButton(canvas, IsShopButtonName), data.interactionType == WorldInteractionType.Shop);
        SetDialogueButtonVisible(FindButton(canvas, IsEventButtonName), hasEvent);
        SetDialogueButtonVisible(FindButton(canvas, IsChatButtonName), true);
        SetDialogueButtonVisible(FindButton(canvas, IsLeaveButtonName), true);
        Button eventButton = FindButton(canvas, IsEventButtonName);
        UiTool.SetButtonText(eventButton, hasEvent && eventGM.IsResolved(data.eventId) ? "事件回顾" :
            hasEvent && eventGM.IsActive(data.eventId) ? "查看位置" : "事件");
        UiTool.SetButtonInteractable(eventButton, true);
        UiTool.SetButtonText(FindButton(canvas, IsLeaveButtonName), "离开");
    }

    // 事件待接取时的按钮布局。
    private void ConfigureEventPrompt(Canvas canvas) => ConfigureEventButtons(canvas, "一起调查", true);

    // 事件进行中时的按钮布局。
    private void ConfigureEventActive(Canvas canvas) => ConfigureEventButtons(canvas, "查看位置", true);

    // 事件完成后的按钮布局。
    private void ConfigureEventResolved(Canvas canvas) => ConfigureEventButtons(canvas, "事件已完成", false);

    // 显示事件确认与离开选项。
    private void ConfigureEventButtons(Canvas canvas, string label, bool interactable)
    {
        Button eventButton = FindButton(canvas, IsEventButtonName);
        Button leaveButton = FindButton(canvas, IsLeaveButtonName);
        SetDialogueButtonVisible(eventButton, true);
        SetDialogueButtonVisible(leaveButton, true);
        UiTool.SetButtonText(eventButton, label);
        UiTool.SetButtonInteractable(eventButton, interactable);
        UiTool.SetButtonText(leaveButton, "离开");
    }

    // 显示或隐藏对话按钮，旁边的「XX选项框框」跟着一起切换。
    private static void SetDialogueButtonVisible(Button button, bool visible)
    {
        UiTool.SetButtonVisible(button, visible);
        if (button == null) return;
        Transform frame = button.transform.parent.Find(GetOptionFrameName(button.name));
        if (frame != null) frame.gameObject.SetActive(visible);
    }

    // 按钮旁边的装饰框名字。
    private static string GetOptionFrameName(string name)
    {
        if (name == "重学教程Button") return "重学教程选项框框";
        if (IsChallengeButtonName(name)) return "挑战选项框框";
        if (IsShopButtonName(name)) return "购买选项框框";
        if (IsEventButtonName(name)) return "事件选项框框";
        if (IsChatButtonName(name)) return "闲聊选项框框";
        return "离开选项框框";
    }

    // 按名字条件在对话画布里找按钮。
    private static Button FindButton(Canvas canvas, Func<string, bool> predicate)
    {
        foreach (Button button in canvas.GetComponentsInChildren<Button>(true))
            if (predicate(button.name)) return button;
        return null;
    }

    // 按明确的角色标识匹配对应的对话框。
    private Canvas GetDialogueCanvas(WorldInteractionActor actor) => dialogueManager.GetView(actor.dialogue.speakerId).canvas;

    // 下面几个按钮名都要和场景里的对象名一致，改名要同步这里。
    private static bool IsChallengeButtonName(string name) => name.Contains("挑战Button");

    // 按名字判断是不是商店按钮。
    private static bool IsShopButtonName(string name) => name.Contains("购买类Buttom");

    // 按名字判断是不是事件按钮。
    private static bool IsEventButtonName(string name) => name.Contains("事件类Buttom");

    // 按名字判断是不是闲聊按钮。
    private static bool IsChatButtonName(string name) => name.Contains("闲聊Buttom");

    // 按名字判断是不是离开按钮。
    private static bool IsLeaveButtonName(string name) => name.Contains("离开Buttom");
}
