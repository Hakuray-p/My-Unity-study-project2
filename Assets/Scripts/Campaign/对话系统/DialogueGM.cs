using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class DialogueGM : MonoBehaviour
{
    [SerializeField] private float dialogueCloseDistance = 3.5f;

    private CharacterGM characterGM;
    private CampaignSession session;
    private EventGM eventGM;
    private Action<string> startMatchAction;
    private Action openShopAction;
    private Action<string> showStatusAction;
    private Action closePanelsAction;
    private readonly Dictionary<string, Canvas> dialogueCanvases = new Dictionary<string, Canvas>();
    private readonly HashSet<Button> dialogueButtonsBound = new HashSet<Button>();
    private WorldInteractionActor dialogueActor;
    private bool dialogueOpen;
    private DialogueState dialogueState;

    private enum DialogueState
    {
        Normal,
        EventPrompt,
        EventActive,
        EventResolved
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

    public void Close()
    {
        dialogueActor = null;
        dialogueOpen = false;
        dialogueState = DialogueState.Normal;
        foreach (Canvas dialogueCanvas in dialogueCanvases.Values)
            if (dialogueCanvas != null) dialogueCanvas.gameObject.SetActive(false);
    }

    private WorldInteractionActor FindNearestActor()
    {
        WorldInteractionActor nearestActor = null;
        float bestDistance = 2.2f;
        foreach (WorldInteractionActor actor in characterGM.Actors)
        {
            if (actor == null) continue;
            float distance = Vector3.Distance(characterGM.Player.position, actor.transform.position);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                nearestActor = actor;
            }
        }
        return nearestActor;
    }

    private void BindDialogueCanvases()
    {
        dialogueCanvases.Clear();
        Canvas[] canvases = FindObjectsOfType<Canvas>(true);
        foreach (Canvas dialogueCanvas in canvases)
        {
            if (dialogueCanvas == null) continue;
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
            if (dialogueCanvas == null) continue;
            if (dialogueCanvas.GetComponent<GraphicRaycaster>() == null)
                dialogueCanvas.gameObject.AddComponent<GraphicRaycaster>();
            dialogueCanvas.sortingOrder = Mathf.Max(dialogueCanvas.sortingOrder, 200);
            dialogueCanvas.gameObject.SetActive(false);
            BindDialogueButtons(dialogueCanvas);
        }
    }

    private void BindDialogueButtons(Canvas dialogueCanvas)
    {
        Button[] buttons = dialogueCanvas.GetComponentsInChildren<Button>(true);
        foreach (Button button in buttons)
        {
            if (button == null) continue;
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
            string matchId = dialogueActor.id;
            if (activeCanvas != null)
            {
                Button[] challengeButtons = activeCanvas.GetComponentsInChildren<Button>(true);
                int challengeIndex = 0;
                foreach (Button challengeButton in challengeButtons)
                {
                    if (challengeButton == null || !IsChallengeButtonName(challengeButton.gameObject.name)) continue;
                    if (challengeButton == button) break;
                    challengeIndex++;
                }
                if (challengeIndex > 0) matchId = dialogueActor.alternateId;
            }
            if (string.IsNullOrEmpty(matchId))
            {
                if (showStatusAction != null) showStatusAction("这个挑战暂未配置");
                return;
            }
            if (startMatchAction != null) startMatchAction(matchId);
        }
        else if (IsShopButtonName(buttonName))
        {
            Close();
            if (openShopAction != null) openShopAction();
        }
    }

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
            if (dialogueCanvas != null) dialogueCanvas.gameObject.SetActive(false);
        activeCanvas.gameObject.SetActive(true);
        SetDialogueText(activeCanvas, "SpeakerName", actor.displayName);
        SetDialogueText(activeCanvas, "DialogueText", GetChatText(actor.displayName));
        ConfigureNormalButtons(activeCanvas, actor);
        dialogueOpen = true;
    }

    private void HandleEventButton(Canvas activeCanvas)
    {
        if (eventGM == null || dialogueActor == null) return;
        CampaignEventData eventData = eventGM.GetEvent(dialogueActor.id);
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

    private void ConfigureNormalButtons(Canvas dialogueCanvas, WorldInteractionActor actor)
    {
        dialogueState = DialogueState.Normal;
        Button challengeButton = FindButton(dialogueCanvas, IsChallengeButtonName);
        Button shopButton = FindButton(dialogueCanvas, IsShopButtonName);
        Button eventButton = FindButton(dialogueCanvas, IsEventButtonName);
        Button chatButton = FindButton(dialogueCanvas, IsChatButtonName);
        Button leaveButton = FindButton(dialogueCanvas, IsLeaveButtonName);
        bool hasEvent = actor.type == WorldInteractionType.Event && eventGM != null && eventGM.GetEvent(actor.id) != null;

        SetButtonVisible(challengeButton, actor.type == WorldInteractionType.Match);
        SetButtonVisible(shopButton, actor.type == WorldInteractionType.Shop);
        SetButtonVisible(eventButton, hasEvent);
        SetButtonVisible(chatButton, true);
        SetButtonVisible(leaveButton, true);
        SetButtonText(eventButton, hasEvent && eventGM.IsResolved(actor.id) ? "事件回顾" :
            hasEvent && eventGM.IsActive(actor.id) ? "查看位置" : "事件");
        SetButtonText(leaveButton, "离开");
        SetButtonInteractable(eventButton, true);
    }

    private void ConfigureEventPrompt(Canvas dialogueCanvas)
    {
        SetButtonVisible(FindButton(dialogueCanvas, IsChallengeButtonName), false);
        SetButtonVisible(FindButton(dialogueCanvas, IsShopButtonName), false);
        SetButtonVisible(FindButton(dialogueCanvas, IsChatButtonName), false);
        Button eventButton = FindButton(dialogueCanvas, IsEventButtonName);
        Button leaveButton = FindButton(dialogueCanvas, IsLeaveButtonName);
        SetButtonVisible(eventButton, true);
        SetButtonVisible(leaveButton, true);
        SetButtonText(eventButton, "一起调查");
        SetButtonText(leaveButton, "暂时离开");
        SetButtonInteractable(eventButton, true);
    }

    private void ConfigureEventActive(Canvas dialogueCanvas)
    {
        SetButtonVisible(FindButton(dialogueCanvas, IsChallengeButtonName), false);
        SetButtonVisible(FindButton(dialogueCanvas, IsShopButtonName), false);
        SetButtonVisible(FindButton(dialogueCanvas, IsChatButtonName), false);
        Button eventButton = FindButton(dialogueCanvas, IsEventButtonName);
        Button leaveButton = FindButton(dialogueCanvas, IsLeaveButtonName);
        SetButtonVisible(eventButton, true);
        SetButtonVisible(leaveButton, true);
        SetButtonText(eventButton, "查看位置");
        SetButtonText(leaveButton, "返回");
        SetButtonInteractable(eventButton, true);
    }

    private void ConfigureEventResolved(Canvas dialogueCanvas)
    {
        SetButtonVisible(FindButton(dialogueCanvas, IsChallengeButtonName), false);
        SetButtonVisible(FindButton(dialogueCanvas, IsShopButtonName), false);
        SetButtonVisible(FindButton(dialogueCanvas, IsChatButtonName), false);
        Button eventButton = FindButton(dialogueCanvas, IsEventButtonName);
        Button leaveButton = FindButton(dialogueCanvas, IsLeaveButtonName);
        SetButtonVisible(eventButton, true);
        SetButtonVisible(leaveButton, true);
        SetButtonText(eventButton, "事件已完成");
        SetButtonText(leaveButton, "返回");
        SetButtonInteractable(eventButton, false);
    }

    private static Button FindButton(Canvas dialogueCanvas, Func<string, bool> namePredicate)
    {
        if (dialogueCanvas == null) return null;
        foreach (Button button in dialogueCanvas.GetComponentsInChildren<Button>(true))
            if (button != null && namePredicate(button.gameObject.name)) return button;
        return null;
    }

    private static void SetButtonVisible(Button button, bool visible)
    {
        if (button != null) button.gameObject.SetActive(visible);
    }

    private static void SetButtonInteractable(Button button, bool interactable)
    {
        if (button != null) button.interactable = interactable;
    }

    private static void SetButtonText(Button button, string value)
    {
        if (button == null) return;
        TMP_Text tmpText = button.GetComponentInChildren<TMP_Text>(true);
        if (tmpText != null)
        {
            tmpText.text = value;
            return;
        }

        Text legacyText = button.GetComponentInChildren<Text>(true);
        if (legacyText != null) legacyText.text = value;
    }

    private Canvas GetDialogueCanvas(WorldInteractionActor actor)
    {
        string actorName = actor != null ? actor.displayName ?? string.Empty : string.Empty;
        foreach (KeyValuePair<string, Canvas> item in dialogueCanvases)
            if (actorName.Contains(item.Key)) return item.Value;
        return null;
    }

    private static bool ContainsName(string objectName, string value)
    {
        return !string.IsNullOrEmpty(objectName) && objectName.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static bool IsChallengeButtonName(string objectName)
    {
        return ContainsName(objectName, "挑战Button") || ContainsName(objectName, "ChallengeButton");
    }

    private static bool IsShopButtonName(string objectName)
    {
        return ContainsName(objectName, "商店Button") || ContainsName(objectName, "购买类Buttom") ||
            ContainsName(objectName, "购买类Button") || ContainsName(objectName, "购买Button") ||
            ContainsName(objectName, "ShopButton");
    }

    private static bool IsEventButtonName(string objectName)
    {
        return ContainsName(objectName, "事件类Button") || ContainsName(objectName, "事件类Buttom") ||
            ContainsName(objectName, "事件Button") || ContainsName(objectName, "事件Buttom") || ContainsName(objectName, "EventButton");
    }

    private static bool IsChatButtonName(string objectName)
    {
        return ContainsName(objectName, "闲聊Button") || ContainsName(objectName, "闲聊Buttom") || ContainsName(objectName, "ChatButton");
    }

    private static bool IsLeaveButtonName(string objectName)
    {
        return ContainsName(objectName, "离开Button") || ContainsName(objectName, "离开Buttom") || ContainsName(objectName, "LeaveButton");
    }

    private static string GetChatText(string npcName)
    {
        if (ContainsName(npcName, "猫姬")) return "先熟悉一下规则吧。真正的比赛开始后，每一步都要谨慎选择。";
        if (ContainsName(npcName, "企鹅")) return "河岸边的赛事马上就要开始了，记得先准备好你的卡组。";
        if (ContainsName(npcName, "黑猫少女")) return "夜灯亮起之前，还有时间再检查一次你的战术。";
        if (ContainsName(npcName, "神秘弓兵")) return "冠军之路不会因为一次胜利就结束，继续保持专注。";
        if (ContainsName(npcName, "阿米娅")) return "我们一起探索这座城市吧。";
        return "今天也要加油。";
    }

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

    private static void ShowChatDialogue(Canvas dialogueCanvas, WorldInteractionActor actor)
    {
        SetDialogueText(dialogueCanvas, "SpeakerName", actor.displayName);
        SetDialogueText(dialogueCanvas, "DialogueText", GetChatText(actor.displayName));
        Debug.Log("显示闲聊内容：" + actor.displayName);
    }
}
