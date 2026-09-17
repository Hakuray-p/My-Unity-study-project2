using System;
using TMPro;
using UnityEngine;

// 根据第一城进度显示当前目标和屏幕方向标记。
[DefaultExecutionOrder(100)]
public sealed class CityGuideController : MonoBehaviour
{
    [SerializeField] private CanvasGroup display; // 指引的整体显示
    [SerializeField] private TMP_Text objectiveText; // 当前目标说明
    [SerializeField] private RectTransform marker; // 目标方向标记
    [SerializeField] private RectTransform canvasRect; // 屏幕坐标所在画布
    [SerializeField] private WorldEventPoint riftPoint; // 已有裂痕调查位置
    [SerializeField] private float headOffset = 2.5f; // 目标头顶标记高度
    [SerializeField] private float edgePadding = 55f; // 屏幕边缘留白
    private readonly string[] matchIds = { "first_light_practice", "first_light_public_01", "first_light_public_02", "first_light_public_03" }; // 主线赛事顺序
    private CampaignSession session; // 当前进度
    private CharacterGM characters; // 场景人物
    private EventGM events; // 事件交互
    private Transform target; // 当前目标位置
    private string objective; // 当前目标文字
    private bool investigating; // 当前是否引导调查
    private bool visible; // 场景是否允许显示指引

    // 接入场景并根据已有进度选择目标。
    public void Initialize(CampaignSession campaignSession, CharacterGM characterGM, EventGM eventGM)
    {
        session = campaignSession;
        characters = characterGM;
        events = eventGM;
        session.ProgressChanged += RefreshObjective;
        RefreshObjective();
    }

    // 在对话、菜单和转场时隐藏指引。
    public void SetVisible(bool show)
    {
        visible = show;
    }

    // 优先处理已接事件，其余按主线赛事顺序推进。
    private void RefreshObjective()
    {
        investigating = session.IsEventActive("first_light", riftPoint.EventId);
        if (investigating)
        {
            target = riftPoint.transform;
            objective = "调查裂痕\n前往南侧桥前的光点";
            return;
        }
        foreach (string matchId in matchIds)
        {
            if (session.IsMatchComplete(matchId)) continue;
            WorldInteractionActor actor = FindMatchActor(matchId);
            target = actor.transform;
            objective = matchId == "first_light_practice" ? "新手练习 · 猫姬\n与猫姬交谈，选择挑战" :
                "城市公开赛 · " + actor.dialogue.displayName + "\n与对手交谈，选择挑战";
            return;
        }
        if (!session.IsEventResolved("first_light", riftPoint.EventId))
        {
            target = FindMatchActor("first_light_champion").transform;
            objective = "未完成的约定\n与黑猫少女交谈，选择事件";
            return;
        }
        if (!session.IsMatchComplete("first_light_champion"))
        {
            target = FindMatchActor("first_light_champion").transform;
            objective = "城市冠军赛 · 黑猫少女\n与她交谈，选择挑战";
            return;
        }
        target = null;
        objective = "本城主线已完成\n可自由探索，与伙伴交谈";
    }

    // 通过角色配置的赛事列表定位对手。
    private WorldInteractionActor FindMatchActor(string matchId)
    {
        foreach (WorldInteractionActor actor in characters.Actors)
            if (Array.Exists(actor.dialogue.matches, challenge => challenge.matchId == matchId)) return actor;
        throw new InvalidOperationException("主线赛事缺少场景角色：" + matchId);
    }

    // 在相机跟随之后更新目标投影和交互提示。
    private void LateUpdate()
    {
        if (session == null) return;
        display.alpha = visible && !riftPoint.IsResolving ? 1f : 0f;
        marker.gameObject.SetActive(target != null);
        objectiveText.text = objective;
        if (target == null) return;
        float distance = Vector3.Distance(characters.Player.position, target.position);
        float interactionDistance = investigating ? events.InteractionDistance : DialogueGM.InteractionDistance;
        objectiveText.text += distance <= interactionDistance ?
            (investigating ? "\n按 E 调查" : "\n按 E 交谈") : "\n距离 " + Mathf.CeilToInt(distance) + " 米";
        Camera camera = HD2DSceneGM.GameplayCamera;
        Vector3 screen = camera.WorldToScreenPoint(target.position + Vector3.up * headOffset);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screen, null, out Vector2 point);
        Vector2 limit = canvasRect.rect.size * 0.5f - Vector2.one * edgePadding;
        bool onScreen = screen.z > 0f && Mathf.Abs(point.x) <= limit.x && Mathf.Abs(point.y) <= limit.y;
        if (onScreen)
        {
            marker.anchoredPosition = point + Vector2.up * (Mathf.Sin(Time.time * 3f) * 5f);
            marker.localRotation = Quaternion.identity;
            return;
        }
        if (screen.z < 0f) point = -point;
        if (point.sqrMagnitude < 0.01f) point = Vector2.down;
        float edge = Mathf.Max(Mathf.Abs(point.x) / limit.x, Mathf.Abs(point.y) / limit.y);
        marker.anchoredPosition = point / edge;
        marker.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(point.y, point.x) * Mathf.Rad2Deg + 90f);
    }

    // 离开场景时解除进度订阅。
    private void OnDestroy()
    {
        if (session != null) session.ProgressChanged -= RefreshObjective;
    }
}
