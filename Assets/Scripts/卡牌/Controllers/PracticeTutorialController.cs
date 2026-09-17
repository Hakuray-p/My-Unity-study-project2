using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 用真实战斗操作推进猫姬教学，并在稳定步骤保存续教状态。
public sealed class PracticeTutorialController : MonoBehaviour
{
    public PracticeTutorialData data; // 教学文案和临时卡组
    [SerializeField] private CanvasGroup display; // 场景内预制的教学界面
    [SerializeField] private RectTransform panel; // 教学说明面板
    [SerializeField] private RectTransform canvasRect; // 教学画布
    [SerializeField] private TMP_Text titleText; // 步骤标题
    [SerializeField] private TMP_Text bodyText; // 教学正文
    [SerializeField] private Button continueButton; // 说明页继续按钮
    [SerializeField] private Button skipButton; // 跳过本次指导
    [SerializeField] private RectTransform firstHighlight; // 当前操作对象的边框
    [SerializeField] private RectTransform secondHighlight; // 操作目标的边框
    [SerializeField] private RectTransform arrow; // 当前操作的箭头
    [SerializeField] private Transform turnEndButton; // 原有结束回合按钮
    private BattleManager battle; // 当前战斗
    private bool tutorialBattle; // 本局是否使用教学资源
    private bool settling; // 是否正在等动作完成
    private bool skipRequested; // 是否在结算后跳过指导
    public PracticeTutorialStep Step { get; private set; } // 当前教学步骤
    public bool IsGuiding => tutorialBattle && Step != PracticeTutorialStep.FreePlay; // 是否仍需指导
    public bool BlocksPointer => IsGuiding && !battle.IsPaused &&
        RectTransformUtility.RectangleContainsScreenPoint(panel, Input.mousePosition, null); // 鼠标是否在教学面板上

    // 接入战斗上下文，普通比赛不启用教学。
    public void Initialize(BattleManager manager)
    {
        battle = manager;
        tutorialBattle = battle.LaunchContext != null && battle.LaunchContext.isTutorial;
        Step = tutorialBattle ? (battle.LaunchContext.snapshot == null ? PracticeTutorialStep.Health :
            battle.LaunchContext.snapshot.tutorialStep) : PracticeTutorialStep.None;
        continueButton.onClick.AddListener(Continue);
        skipButton.onClick.AddListener(Skip);
        ShowStep();
    }

    // 等开局或读档就绪，再保存当前教学检查点。
    public void Begin()
    {
        if (IsGuiding) StartCoroutine(AdvanceWhenSettled(Step));
    }

    // 教学完成后恢复普通操作；教学中只开放当前需要的动作。
    public bool Allows(TutorialAction action, CardController card = null, CardController target = null)
    {
        if (!IsGuiding) return true;
        if (action == TutorialAction.SelectTarget)
            return (Step == PracticeTutorialStep.Discard || Step == PracticeTutorialStep.Revive) && card.cardData.index == 1204;
        if (settling || skipRequested || !battle.IsSettled) return false;
        int cardId = card == null ? 0 : card.cardData.index;
        switch (Step)
        {
            case PracticeTutorialStep.Summon:
                return action == TutorialAction.Summon && cardId == 1203;
            case PracticeTutorialStep.EndFirstTurn:
            case PracticeTutorialStep.EndSecondTurn:
                return action == TutorialAction.EndTurn;
            case PracticeTutorialStep.AttackGuard:
                return action == TutorialAction.AttackCard && cardId == 1203 && target != null && target.cardData.index == data.guardCard.index;
            case PracticeTutorialStep.CastSpell:
                return action == TutorialAction.Spell && cardId == 1205;
            case PracticeTutorialStep.ViewGrave:
                return action == TutorialAction.ViewGrave;
            case PracticeTutorialStep.SummonCaster:
                return action == TutorialAction.Summon && cardId == 1211;
            case PracticeTutorialStep.CastAbility:
                return action == TutorialAction.Ability && cardId == 1211;
            case PracticeTutorialStep.AttackHero:
                return action == TutorialAction.AttackPlayer && cardId == 1203;
            default:
                return false;
        }
    }

    // 只允许拿起当前教学步骤需要的卡牌。
    public bool CanDrag(CardController card)
    {
        if (!IsGuiding) return true;
        if (settling || !battle.IsSettled) return false;
        int cardId = card.cardData.index;
        if (card.cardState == CardState.Hand)
            return (Step == PracticeTutorialStep.Summon && cardId == 1203) ||
                (Step == PracticeTutorialStep.CastSpell && cardId == 1205) ||
                (Step == PracticeTutorialStep.SummonCaster && cardId == 1211);
        return (Step == PracticeTutorialStep.AttackGuard || Step == PracticeTutorialStep.AttackHero) && cardId == 1203 ||
            Step == PracticeTutorialStep.CastAbility && cardId == 1211;
    }

    // 成功接受操作后推进说明，实际结算完成之前不保存下一步。
    public void ActionAccepted(TutorialAction action)
    {
        if (!IsGuiding) return;
        switch (action)
        {
            case TutorialAction.Summon:
                StartCoroutine(AdvanceWhenSettled(Step == PracticeTutorialStep.Summon ?
                    PracticeTutorialStep.EndFirstTurn : PracticeTutorialStep.CastAbility));
                break;
            case TutorialAction.EndTurn:
                SetStep(Step == PracticeTutorialStep.EndFirstTurn ? PracticeTutorialStep.EnemyGuard : PracticeTutorialStep.EnemyPass, false);
                break;
            case TutorialAction.AttackCard:
                StartCoroutine(AdvanceWhenSettled(PracticeTutorialStep.CastSpell));
                break;
            case TutorialAction.Spell:
                SetStep(PracticeTutorialStep.Discard, false);
                break;
            case TutorialAction.Ability:
                SetStep(PracticeTutorialStep.Revive, false);
                break;
            case TutorialAction.SelectTarget:
                StartCoroutine(AdvanceWhenSettled(Step == PracticeTutorialStep.Discard ?
                    PracticeTutorialStep.ViewGrave : PracticeTutorialStep.AttackHero));
                break;
            case TutorialAction.ViewGrave:
                SetStep(PracticeTutorialStep.CloseGrave, false);
                break;
            case TutorialAction.AttackPlayer:
                StartCoroutine(AdvanceWhenSettled(PracticeTutorialStep.Completed));
                break;
        }
    }

    // 墓地查看收起后继续教学。
    public void GraveClosed()
    {
        if (IsGuiding && Step == PracticeTutorialStep.CloseGrave)
            StartCoroutine(AdvanceWhenSettled(PracticeTutorialStep.EndSecondTurn));
    }

    // 教学期间让猫姬召唤守卫或让过，不改变正式 AI 策略。
    public void TakeEnemyTurn(AIController enemy)
    {
        if (skipRequested || settling) return;
        battle.SaveTutorialCheckpoint();
        if (Step == PracticeTutorialStep.EnemyGuard)
        {
            CardController guard = enemy.hands.handCards.Find(card => card.cardData.index == data.guardCard.index);
            if (guard != null)
            {
                battle.SummonCard(guard);
                return;
            }
        }
        battle.OnClickTurnEnd(enemy.playerId);
    }

    // 回合真正交回玩家后才开放下一步操作。
    private void Update()
    {
        if (!IsGuiding || battle.IsPaused || settling || !battle.IsSettled) return;
        if (skipRequested)
        {
            SetStep(PracticeTutorialStep.FreePlay, true);
            return;
        }
        if (!battle.GetMainPlayer.isInTurn) return;
        if (Step == PracticeTutorialStep.EnemyGuard) SetStep(PracticeTutorialStep.AttackGuard, true);
        else if (Step == PracticeTutorialStep.EnemyPass) SetStep(PracticeTutorialStep.SummonCaster, true);
    }

    // 说明页由继续按钮翻页，操作页必须实际完成。
    private void Continue()
    {
        if (settling || skipRequested || !battle.IsSettled) return;
        if (Step >= PracticeTutorialStep.Health && Step <= PracticeTutorialStep.Stats)
            SetStep(Step + 1, true);
        else if (Step == PracticeTutorialStep.Completed) SetStep(PracticeTutorialStep.FreePlay, true);
    }

    // 不清场、不判胜，在当前结算完成后恢复自由对战。
    private void Skip()
    {
        skipRequested = true;
        skipButton.interactable = false;
        if (battle.TM.IsViewing) battle.TM.CloseViewList();
        else if (battle.IsSettled && !settling) SetStep(PracticeTutorialStep.FreePlay, true);
    }

    // 保留上一稳定检查点，等动作与效果完整结束后再推进。
    private IEnumerator AdvanceWhenSettled(PracticeTutorialStep next)
    {
        settling = true;
        yield return null;
        yield return new WaitUntil(() => battle.IsSettled && !battle.IsPaused);
        settling = false;
        SetStep(skipRequested ? PracticeTutorialStep.FreePlay : next, true);
    }

    // 更新步骤，并将稳定步骤与实际战况一起保存。
    private void SetStep(PracticeTutorialStep next, bool checkpoint)
    {
        Step = next;
        if (next == PracticeTutorialStep.Completed || next == PracticeTutorialStep.FreePlay)
            CampaignSession.Instance.State.practiceTutorialHandled = true;
        ShowStep();
        if (checkpoint) battle.SaveTutorialCheckpoint();
    }

    // 显示场景中已存在的教学面板。
    private void ShowStep()
    {
        display.alpha = IsGuiding ? 1f : 0f;
        display.interactable = IsGuiding;
        display.blocksRaycasts = IsGuiding;
        if (!IsGuiding) return;
        TutorialLesson lesson = Array.Find(data.lessons, item => item.step == Step);
        titleText.text = "猫姬 · " + lesson.title;
        bodyText.text = lesson.text;
        continueButton.gameObject.SetActive(Step >= PracticeTutorialStep.Health && Step <= PracticeTutorialStep.Stats || Step == PracticeTutorialStep.Completed);
    }

    // 跟随当前手牌、目标或界面控件显示高亮。
    private void LateUpdate()
    {
        if (battle == null) return;
        bool show = IsGuiding && !battle.IsPaused && !SceneFlowService.IsLoading;
        display.alpha = show ? 1f : 0f;
        display.interactable = show;
        display.blocksRaycasts = show;
        firstHighlight.gameObject.SetActive(false);
        secondHighlight.gameObject.SetActive(false);
        arrow.gameObject.SetActive(false);
        if (!show) return;
        continueButton.interactable = !settling && battle.IsSettled;
        PlayerController player = battle.GetMainPlayer;
        PlayerController enemy = battle.GetEnemyPlayer(player.playerId);
        Transform focus = null;
        Transform destination = null;
        switch (Step)
        {
            case PracticeTutorialStep.Health: focus = player.healthText.transform; destination = enemy.healthText.transform; break;
            case PracticeTutorialStep.Cost: focus = player.costText.transform; break;
            case PracticeTutorialStep.Hand: focus = player.hands.transform; break;
            case PracticeTutorialStep.Stats:
            case PracticeTutorialStep.Summon: focus = FindCard(player, 1203); destination = player.field.transform; break;
            case PracticeTutorialStep.EndFirstTurn:
            case PracticeTutorialStep.EndSecondTurn: focus = turnEndButton; break;
            case PracticeTutorialStep.EnemyGuard: focus = enemy.field.transform; break;
            case PracticeTutorialStep.AttackGuard: focus = FindCard(player, 1203); destination = FindCard(enemy, data.guardCard.index); break;
            case PracticeTutorialStep.CastSpell: focus = FindCard(player, 1205); destination = player.field.transform; break;
            case PracticeTutorialStep.Discard:
            case PracticeTutorialStep.Revive: focus = FindCard(player, 1204); break;
            case PracticeTutorialStep.ViewGrave: focus = player.gravePos; break;
            case PracticeTutorialStep.SummonCaster: focus = FindCard(player, 1211); destination = player.field.transform; break;
            case PracticeTutorialStep.CastAbility: focus = FindCard(player, 1211); break;
            case PracticeTutorialStep.AttackHero: focus = FindCard(player, 1203); destination = enemy.iconPos; break;
        }
        if (focus != null)
        {
            Highlight(firstHighlight, focus);
            arrow.gameObject.SetActive(true);
            arrow.anchoredPosition = firstHighlight.anchoredPosition + Vector2.up * (firstHighlight.sizeDelta.y * 0.5f + 28f);
        }
        if (destination != null) Highlight(secondHighlight, destination);
    }

    // 优先找到场上或待选的教学卡，而不是牌堆中的同名副本。
    private Transform FindCard(PlayerController player, int cardId)
    {
        CardController card = player.field.cards.Find(item => item.cardData.index == cardId);
        if (card == null) card = player.hands.handCards.Find(item => item.cardData.index == cardId);
        if (card == null) card = player.graveCards.Find(item => item.cardData.index == cardId);
        return card == null ? null : card.transform;
    }

    // 将界面或立体卡牌的范围投影到教学画布。
    private void Highlight(RectTransform frame, Transform focus)
    {
        Camera camera = Camera.main;
        var points = new Vector3[8];
        int count;
        if (focus is RectTransform rectangle)
        {
            var corners = new Vector3[4];
            rectangle.GetWorldCorners(corners);
            Canvas sourceCanvas = focus.GetComponentInParent<Canvas>();
            count = 4;
            for (int index = 0; index < count; index++)
                points[index] = sourceCanvas != null && sourceCanvas.renderMode == RenderMode.ScreenSpaceOverlay ?
                    corners[index] : camera.WorldToScreenPoint(corners[index]);
        }
        else
        {
            Collider collider = focus.GetComponent<Collider>();
            Bounds bounds = collider != null ? collider.bounds : new Bounds(focus.position, new Vector3(1.8f, 0.3f, 1.8f));
            count = 8;
            for (int index = 0; index < count; index++)
            {
                Vector3 corner = bounds.center + Vector3.Scale(bounds.extents,
                    new Vector3((index & 1) == 0 ? -1f : 1f, (index & 2) == 0 ? -1f : 1f, (index & 4) == 0 ? -1f : 1f));
                points[index] = camera.WorldToScreenPoint(corner);
            }
        }
        var minimum = new Vector2(float.MaxValue, float.MaxValue);
        var maximum = new Vector2(float.MinValue, float.MinValue);
        for (int index = 0; index < count; index++)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, points[index], null, out Vector2 point);
            minimum = Vector2.Min(minimum, point);
            maximum = Vector2.Max(maximum, point);
        }
        frame.gameObject.SetActive(true);
        frame.anchoredPosition = (minimum + maximum) * 0.5f;
        frame.sizeDelta = maximum - minimum + Vector2.one * 20f;
    }
}
