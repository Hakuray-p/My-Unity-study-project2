using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 负责事件的接取、目标点状态和奖励结算。
/// </summary>
public sealed class EventGM : MonoBehaviour
{
    [Min(0.1f)][SerializeField] private float interactionDistance = 2.2f; // 事件点交互距离

    private CampaignSession session; // 当前存档会话
    private CharacterGM characterGM; // 角色管理器，用来取玩家位置
    private Action<string> showStatusAction; // 显示状态提示的回调，由场景 GM 提供
    private readonly Dictionary<string, WorldEventPoint> eventPoints = new Dictionary<string, WorldEventPoint>(); // 场景里的事件点，按事件 id 索引
    private bool initialized; // 是否已经初始化
    public float InteractionDistance => interactionDistance; // 事件指引使用的真实交互距离

    /// <summary>
    /// 初始化事件管理器并绑定场景中的事件点。
    /// </summary>
    public void Initialize(CampaignSession campaignSession, CharacterGM characterManager, Action<string> showStatus)
    {
        session = campaignSession;
        characterGM = characterManager;
        showStatusAction = showStatus;
        eventPoints.Clear();
        foreach (WorldEventPoint point in FindObjectsOfType<WorldEventPoint>(true))
            eventPoints[point.EventId] = point;
        initialized = true;
        RefreshEventPoints();
    }

    /// <summary>
    /// 检查当前已接取事件的地图目标。
    /// </summary>
    public void Tick()
    {
        if (!initialized || session == null || session.State == null || characterGM == null || characterGM.Player == null) return;

        foreach (KeyValuePair<string, WorldEventPoint> item in eventPoints)
        {
            if (item.Value == null || !IsActive(item.Key) || item.Value.IsResolving) continue;
            float distance = Vector3.Distance(characterGM.Player.position, item.Value.transform.position);
            if (distance <= interactionDistance && Input.GetKeyDown(KeyCode.E))
            {
                string eventId = item.Key;
                item.Value.PlayResolve(() => ResolveEvent(eventId, out bool cardAdded));
                return;
            }
        }
    }

    // 按 id 取事件数据
    public CampaignEventData GetEvent(string eventId)
    {
        return CampaignCatalog.GetEvent(eventId);
    }

    // 事件是否已经接取、还没完成
    public bool IsActive(string eventId)
    {
        CampaignEventData eventData = GetEvent(eventId);
        return eventData != null && session != null && session.IsEventActive(eventData.cityId, eventId);
    }

    // 事件是不是已经完成
    public bool IsResolved(string eventId)
    {
        CampaignEventData eventData = GetEvent(eventId);
        return eventData != null && session != null && session.IsEventResolved(eventData.cityId, eventId);
    }

    /// <summary>
    /// 接取事件并显示地图目标提示。
    /// </summary>
    public void StartEvent(string eventId)
    {
        CampaignEventData eventData = GetEvent(eventId);
        if (eventData == null) return;
        if (IsResolved(eventId))
        {
            ShowStatus(eventData.reviewText);
            return;
        }

        session.StartEvent(eventData);
        RefreshEventPoints();
        ShowStatus(eventData.objectiveText);
    }

    /// <summary>
    /// 完成事件并发放一次性奖励。
    /// </summary>
    public bool ResolveEvent(string eventId, out bool cardAdded)
    {
        cardAdded = false;
        CampaignEventData eventData = GetEvent(eventId);
        if (eventData == null || !session.ResolveEvent(eventData, out cardAdded)) return false;

        RefreshEventPoints();
        string rewardMessage = cardAdded
            ? "获得" + eventData.goldReward + "金币和卡牌《" + eventData.cardRewardName + "》。"
            : "获得" + eventData.goldReward + "金币；卡牌《" + eventData.cardRewardName + "》已经在收藏中。";
        ShowStatus(eventData.completeText + rewardMessage);
        return true;
    }

    // 只让进行中且没完成的事件点显示出来
    private void RefreshEventPoints()
    {
        foreach (WorldEventPoint point in eventPoints.Values)
        {
            point.SetVisible(IsActive(point.EventId) && !IsResolved(point.EventId));
        }
    }

    // 把提示文字交给外部显示
    private void ShowStatus(string message)
    {
        if (showStatusAction != null) showStatusAction(message);
    }
}
