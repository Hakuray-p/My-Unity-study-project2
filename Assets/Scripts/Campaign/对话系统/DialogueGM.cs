using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 所有 NPC 对话的统一管理器，负责对话框显示、按钮行为和事件分支
public sealed class DialogueGM : MonoBehaviour
{
    [SerializeField] private float dialogueCloseDistance = 3.5f; // 走开多远自动关掉对话

    private CharacterGM characterGM; // 角色管理器
    private CampaignSession session; // 当前存档会话
    private EventGM eventGM; // 事件管理器
    private Action<string> startMatchAction; // 开比赛的入口，由场景 GM 提供
    private Action openShopAction; // 打开商店的入口，由场景 GM 提供
    private Action<string> showStatusAction; // 显示状态提示的回调
    private Action closePanelsAction; // 关闭其它面板的回调
    private readonly Dictionary<string, Canvas> dialogueCanvases = new Dictionary<string, Canvas>(); // 每个 NPC 的对话框，按角色名索引
    private readonly HashSet<Button> dialogueButtonsBound = new HashSet<Button>(); // 已经挂过点击的按钮，避免重复绑定
    private WorldInteractionActor dialogueActor; // 当前正在对话的角色
    private bool dialogueOpen; // 对话框是否打开
    private DialogueState dialogueState; // 当前事件对话处在哪个阶段

    // 事件对话当前处在哪个阶段
    private enum DialogueState
    {
        Normal, // 普通对话
        EventPrompt, // 事件待接取
        EventActive, // 事件进行中
        EventResolved // 事件已完成
    }

    public bool IsOpen => dialogueOpen;

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
        BindDialogueCanvases();
    }

    // 每帧找最近的可对话角色，按 E 开对话，走远或按 E / Esc 关对话
    public void Tick()
    {
        if (characterGM == null || characterGM.Player == null) return;

        WorldInteractionActor nearestActor = FindNearestActor();
        if (dialogueOpen)
        {
            bool movedAway = dialogueActor == null ||
                Vector3.Distance(characterGM.Player.position, dialogueActor.transform.position) > dialogueCloseDistance;
            if (movedAway || Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Escape)) Close();
            return;
        }

        if (nearestActor != null && Input.GetKeyDown(KeyCode.E)) OpenDialogue(nearestActor);
    }

    // 关闭对话并复位状态
    public void Close()
    {
        dialogueActor = null;
        dialogueOpen = false;
        dialogueState = DialogueState.Normal;
        foreach (Canvas dialogueCanvas in dialogueCanvases.Values)
            dialogueCanvas.gameObject.SetActive(false);
    }

    // 找玩家附近 2.2 米内最近的交互角色
    private WorldInteractionActor FindNearestActor()
    {
        WorldInteractionActor nearestActor = null;
        float bestDistance = 2.2f;
        foreach (WorldInteractionActor actor in characterGM.Actors)
        {
            float distance = Vector3.Distance(characterGM.Player.position, actor.transform.position);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                nearestActor = actor;
            }
        }
        return nearestActor;
    }

    // 按名字收集各个 NPC 的对话框，补齐射线组件并先隐藏
    private void BindDialogueCanvases()
    {
        dialogueCanvases.Clear();
        Canvas[] canvases = FindObjectsOfType<Canvas>(true);
        foreach (Canvas dialogueCanvas in canvases)
        {
            string canvasName = dialogueCanvas.gameObject.name;
            if (canvasName.Contains("猫姬")) dialogueCanvases["猫姬"] = dialogueCanvas;
            else if (canvasName.Contains("企鹅")) dialogueCanvases["企鹅"] = dialogueCanvas;
            else if (canvasName.Contains("神秘弓兵")) dialogueCanvases["神秘弓兵"] = dialogueCanvas;
            else if (canvasName.Contains("黑猫少女")) dialogueCanvases["黑猫少女"] = dialogueCanvas;
            else if (canvasName.Contains("商人")) dialogueCanvases["商人"] = dialogueCanvas;
            else if (canvasName.Contains("阿米娅")) dialogueCanvases["阿米娅"] = dialogueCanvas;
        }

        foreach (Canvas dialogueCanvas in dialogueCanvases.Values)
        {
            if (dialogueCanvas.GetComponent<GraphicRaycaster>() == null)
                dialogueCanvas.gameObject.AddComponent<GraphicRaycaster>();
            dialogueCanvas.sortingOrder = Mathf.Max(dialogueCanvas.sortingOrder, 200);
            dialogueCanvas.gameObject.SetActive(false);
            BindDialogueButtons(dialogueCanvas);
        }
    }

    // 给对话框里的功能按钮挂上统一的点击处理
    private void BindDialogueButtons(Canvas dialogueCanvas)
    {
        Button[] buttons = dialogueCanvas.GetComponentsInChildren<Button>(true);
        foreach (Button button in buttons)
        {
            string buttonName = button.gameObject.name;
            if (!IsChallengeButtonName(buttonName) && !IsShopButtonName(buttonName) &&
                !IsEventButtonName(buttonName) && !IsLeaveButtonName(buttonName) && !IsChatButtonName(buttonName)) continue;
            if (dialogueButtonsBound.Contains(button)) continue;
            dialogueButtonsBound.Add(button);
            Button capturedButton = button;
            Graphic[] graphics = button.GetComponentsInChildren<Graphic>(true);
            foreach (Graphic graphic in graphics)
                if (graphic != null && graphic.gameObject != button.gameObject) graphic.raycastTarget = false;
            button.onClick.AddListener(() => HandleDialogueButton(capturedButton));
        }
    }

    // 按按钮种类分派：离开 / 闲聊 / 事件 / 挑战 / 购买
    private void HandleDialogueButton(Button button)
    {
        if (button == null) return;
        string buttonName = button.gameObject.name;
        if (IsLeaveButtonName(buttonName))
        {
            if (closePanelsAction != null) closePanelsAction();
            else Close();
            return;
        }
        if (dialogueActor == null) return;

        Canvas activeCanvas = button.GetComponentInParent<Canvas>();
        if (IsChatButtonName(buttonName))
        {
            ShowChatDialogue(activeCanvas, dialogueActor);
            return;
        }
        if (IsEventButtonName(buttonName))
        {
            HandleEventButton(activeCanvas);
            return;
        }
        if (IsChallengeButtonName(buttonName))
        {
            if (string.IsNullOrEmpty(dialogueActor.matchId))
            {
                if (showStatusAction != null) showStatusAction("这个挑战暂未配置");
                return;
            }
            if (startMatchAction != null) startMatchAction(dialogueActor.matchId);
        }
        else if (IsShopButtonName(buttonName))
        {
            Close();
            if (openShopAction != null) openShopAction();
        }
    }

    // 打开这个角色对应的对话框
    private void OpenDialogue(WorldInteractionActor actor)
    {
        Canvas activeCanvas = GetDialogueCanvas(actor);
        if (activeCanvas == null)
        {
            Debug.LogWarning("没有找到 NPC 对应的 DialogueCanvas：" + actor.displayName);
            return;
        }

        dialogueActor = actor;
        foreach (Canvas dialogueCanvas in dialogueCanvases.Values)
            dialogueCanvas.gameObject.SetActive(false);
        activeCanvas.gameObject.SetActive(true);
        SetDialogueText(activeCanvas, "SpeakerName", actor.displayName);
        SetDialogueText(activeCanvas, "DialogueText", GetChatText(actor.displayName));
        ConfigureNormalButtons(activeCanvas, actor);
        dialogueOpen = true;
    }

    // 事件按钮，按待接取 / 进行中 / 已完成三种状态切换文案和按钮
    private void HandleEventButton(Canvas activeCanvas)
    {
        if (eventGM == null || dialogueActor == null) return;
        CampaignEventData eventData = eventGM.GetEvent(dialogueActor.eventId);
        if (eventData == null)
        {
            if (showStatusAction != null) showStatusAction("这个事件暂未配置");
            return;
        }

        if (dialogueState == DialogueState.EventPrompt)
        {
            eventGM.StartEvent(eventData.eventId);
            if (closePanelsAction != null) closePanelsAction();
            else Close();
            return;
        }

        if (eventGM.IsResolved(eventData.eventId))
        {
            dialogueState = DialogueState.EventResolved;
            SetDialogueText(activeCanvas, "DialogueText", eventData.reviewText);
            ConfigureEventResolved(activeCanvas);
            return;
        }

        if (eventGM.IsActive(eventData.eventId))
        {
            dialogueState = DialogueState.EventActive;
            SetDialogueText(activeCanvas, "DialogueText", eventData.objectiveText);
            ConfigureEventActive(activeCanvas);
            return;
        }

        dialogueState = DialogueState.EventPrompt;
        SetDialogueText(activeCanvas, "DialogueText", eventData.startText);
        ConfigureEventPrompt(activeCanvas);
    }

    // 普通对话下按角色类型显示挑战、购买、事件等按钮
    private void ConfigureNormalButtons(Canvas dialogueCanvas, WorldInteractionActor actor)
    {
        dialogueState = DialogueState.Normal;
        Button challengeButton = FindButton(dialogueCanvas, IsChallengeButtonName);
        Button shopButton = FindButton(dialogueCanvas, IsShopButtonName);
        Button eventButton = FindButton(dialogueCanvas, IsEventButtonName);
        Button chatButton = FindButton(dialogueCanvas, IsChatButtonName);
        Button leaveButton = FindButton(dialogueCanvas, IsLeaveButtonName);
        bool isShop = actor.type == WorldInteractionType.Shop;
        bool hasEvent = eventGM != null && eventGM.GetEvent(actor.eventId) != null;

        SetDialogueButtonVisible(challengeButton, true);
        SetDialogueButtonVisible(shopButton, isShop);
        SetDialogueButtonVisible(eventButton, !isShop);
        SetDialogueButtonVisible(chatButton, true);
        SetDialogueButtonVisible(leaveButton, true);
        UiTool.SetButtonText(eventButton, hasEvent && eventGM.IsResolved(actor.eventId) ? "事件回顾" :
            hasEvent && eventGM.IsActive(actor.eventId) ? "查看位置" : "事件");
        UiTool.SetButtonText(leaveButton, "离开");
        UiTool.SetButtonInteractable(eventButton, true);
    }

    // 事件待接取时的按钮布局
    private void ConfigureEventPrompt(Canvas dialogueCanvas)
    {
        SetDialogueButtonVisible(FindButton(dialogueCanvas, IsChallengeButtonName), false);
        SetDialogueButtonVisible(FindButton(dialogueCanvas, IsShopButtonName), false);
        SetDialogueButtonVisible(FindButton(dialogueCanvas, IsChatButtonName), false);
        Button eventButton = FindButton(dialogueCanvas, IsEventButtonName);
        Button leaveButton = FindButton(dialogueCanvas, IsLeaveButtonName);
        SetDialogueButtonVisible(eventButton, true);
        SetDialogueButtonVisible(leaveButton, true);
        UiTool.SetButtonText(eventButton, "一起调查");
        UiTool.SetButtonText(leaveButton, "暂时离开");
        UiTool.SetButtonInteractable(eventButton, true);
    }

    // 事件进行中时的按钮布局
    private void ConfigureEventActive(Canvas dialogueCanvas)
    {
        SetDialogueButtonVisible(FindButton(dialogueCanvas, IsChallengeButtonName), false);
        SetDialogueButtonVisible(FindButton(dialogueCanvas, IsShopButtonName), false);
        SetDialogueButtonVisible(FindButton(dialogueCanvas, IsChatButtonName), false);
        Button eventButton = FindButton(dialogueCanvas, IsEventButtonName);
        Button leaveButton = FindButton(dialogueCanvas, IsLeaveButtonName);
        SetDialogueButtonVisible(eventButton, true);
        SetDialogueButtonVisible(leaveButton, true);
        UiTool.SetButtonText(eventButton, "查看位置");
        UiTool.SetButtonText(leaveButton, "返回");
        UiTool.SetButtonInteractable(eventButton, true);
    }

    // 事件完成后的按钮布局
    private void ConfigureEventResolved(Canvas dialogueCanvas)
    {
        SetDialogueButtonVisible(FindButton(dialogueCanvas, IsChallengeButtonName), false);
        SetDialogueButtonVisible(FindButton(dialogueCanvas, IsShopButtonName), false);
        SetDialogueButtonVisible(FindButton(dialogueCanvas, IsChatButtonName), false);
        Button eventButton = FindButton(dialogueCanvas, IsEventButtonName);
        Button leaveButton = FindButton(dialogueCanvas, IsLeaveButtonName);
        SetDialogueButtonVisible(eventButton, true);
        SetDialogueButtonVisible(leaveButton, true);
        UiTool.SetButtonText(eventButton, "事件已完成");
        UiTool.SetButtonText(leaveButton, "返回");
        UiTool.SetButtonInteractable(eventButton, false);
    }

    // 显示或隐藏对话按钮，旁边的「XX选项框框」跟着一起切换
    private static void SetDialogueButtonVisible(Button button, bool visible)
    {
        UiTool.SetButtonVisible(button, visible);
        if (button == null) return;
        Transform frame = button.transform.parent.Find(GetOptionFrameName(button.gameObject.name));
        if (frame != null) frame.gameObject.SetActive(visible);
    }

    // 按钮旁边的装饰框名字
    private static string GetOptionFrameName(string buttonName)
    {
        if (IsChallengeButtonName(buttonName)) return "挑战选项框框";
        if (IsShopButtonName(buttonName)) return "购买选项框框";
        if (IsEventButtonName(buttonName)) return "事件选项框框";
        if (IsChatButtonName(buttonName)) return "闲聊选项框框";
        return "离开选项框框";
    }
    // 按名字条件在对话画布里找按钮
    private static Button FindButton(Canvas dialogueCanvas, Func<string, bool> namePredicate)
    {
        if (dialogueCanvas == null) return null;
        foreach (Button button in dialogueCanvas.GetComponentsInChildren<Button>(true))
            if (namePredicate(button.gameObject.name)) return button;
        return null;
    }

    // 按角色显示名匹配对应的对话框
    private Canvas GetDialogueCanvas(WorldInteractionActor actor)
    {
        string actorName = actor != null ? actor.displayName ?? string.Empty : string.Empty;
        foreach (KeyValuePair<string, Canvas> item in dialogueCanvases)
            if (actorName.Contains(item.Key)) return item.Value;
        return null;
    }

    // 下面几个按钮名都要和场景里的对象名一致，改名要同步这里
    private static bool IsChallengeButtonName(string objectName)
    {
        return objectName.Contains("挑战Button");
    }

    // 按名字判断是不是商店按钮
    private static bool IsShopButtonName(string objectName)
    {
        return objectName.Contains("购买类Buttom");
    }

    // 按名字判断是不是事件按钮
    private static bool IsEventButtonName(string objectName)
    {
        return objectName.Contains("事件类Buttom");
    }

    // 按名字判断是不是闲聊按钮
    private static bool IsChatButtonName(string objectName)
    {
        return objectName.Contains("闲聊Buttom");
    }

    // 按名字判断是不是离开按钮
    private static bool IsLeaveButtonName(string objectName)
    {
        return objectName.Contains("离开Buttom");
    }

    // 每个 NPC 的闲聊文本
    private static string GetChatText(string npcName)
    {
        if (npcName.Contains("猫姬")) return "先熟悉一下规则吧。真正的比赛开始后，每一步都要谨慎选择。";
        if (npcName.Contains("企鹅")) return "河岸边的赛事马上就要开始了，记得先准备好你的卡组。";
        if (npcName.Contains("黑猫少女")) return "夜灯亮起之前，还有时间再检查一次你的战术。";
        if (npcName.Contains("神秘弓兵")) return "冠军之路不会因为一次胜利就结束，继续保持专注。";
        if (npcName.Contains("阿米娅")) return "我们一起探索这座城市吧。";
        return "今天也要加油。";
    }

    // 按对象名找文字组件写入内容，TMP 和旧版 Text 都试一次
    private static void SetDialogueText(Canvas dialogueCanvas, string objectName, string value)
    {
        TMP_Text[] tmpTexts = dialogueCanvas.GetComponentsInChildren<TMP_Text>(true);
        foreach (TMP_Text text in tmpTexts)
        {
            if (text != null && text.gameObject.name == objectName)
            {
                text.text = value;
                return;
            }
        }

        Text[] legacyTexts = dialogueCanvas.GetComponentsInChildren<Text>(true);
        foreach (Text text in legacyTexts)
        {
            if (text != null && text.gameObject.name == objectName)
            {
                text.text = value;
                return;
            }
        }
    }

    // 把闲聊内容写进对话框
    private static void ShowChatDialogue(Canvas dialogueCanvas, WorldInteractionActor actor)
    {
        SetDialogueText(dialogueCanvas, "SpeakerName", actor.displayName);
        SetDialogueText(dialogueCanvas, "DialogueText", GetChatText(actor.displayName));
        Debug.Log("显示闲聊内容：" + actor.displayName);
    }
}
