using System.Collections.Generic;
using UnityEngine;

// 城市场景里玩家和 NPC 的统一管理器，负责生成玩家、恢复位置和创建世界交互点
public sealed class CharacterGM : MonoBehaviour
{
    private CampaignSession session; // 当前存档会话
    private Camera gameplayCamera; // 场景主相机
    private Transform player; // 玩家角色
    private Vector3 playerInitialPosition; // 玩家进场景时的初始位置
    private bool playerInitialPositionReady; // 初始位置是否已经记录过
    private readonly List<WorldInteractionActor> actors = new List<WorldInteractionActor>(); // 场景里的世界交互点

    public Transform Player => player;
    public Vector3 PlayerInitialPosition => playerInitialPosition;
    public IReadOnlyList<WorldInteractionActor> Actors => actors;
    public bool IsReady => player != null;

    // 接手存档和相机，玩家还没生成时补上
    public void Initialize(CampaignSession campaignSession, Camera camera)
    {
        session = campaignSession;
        gameplayCamera = camera;
        if (player == null) SetupPlayer();
    }

    // 把玩家放回存档记录的位置，存档位置接近原点时不动
    public void RestorePlayer()
    {
        if (session.State.playerPosition.sqrMagnitude <= 0.25f) return;
        CharacterController controller = player.GetComponent<CharacterController>();
        if (controller != null) controller.enabled = false;
        player.position = session.State.playerPosition;
        if (controller != null) controller.enabled = true;
        player.GetComponent<AmiyaCharacter>()?.SetSafePosition();
    }

    // 按 NPC 名字绑定交互点，场景里没找到就近生成几个默认的
    public void CreateWorldActors()
    {
        if (actors.Count > 0) return;

        HDNpcCharacter[] sceneNpcs = FindObjectsOfType<HDNpcCharacter>(true);
        for (int i = 0; i < sceneNpcs.Length; i++)
        {
            HDNpcCharacter npc = sceneNpcs[i];
            string npcName = npc.name;
            bool isShopNpc = npcName.Contains("商人");
            bool isEventNpc = npcName.Contains("黑猫少女");
            string matchId = npcName.Contains("猫姬") ? GetCatMatchId() :
                npcName.Contains("企鹅") ? "first_light_public_02" :
                npcName.Contains("神秘弓兵") ? "first_light_public_03" :
                isShopNpc ? "first_light_public_04" :
                isEventNpc ? "first_light_champion" : null;
            string eventId = isEventNpc ? "first_light_event_01" : null;
            WorldInteractionType actorType = isShopNpc ? WorldInteractionType.Shop :
                isEventNpc ? WorldInteractionType.Event : WorldInteractionType.Match;
            Collider collider = npc.GetComponent<Collider>();
            if (collider == null) collider = npc.gameObject.AddComponent<SphereCollider>();
            collider.isTrigger = true;
            WorldInteractionActor actor = npc.GetComponent<WorldInteractionActor>();
            if (actor == null) actor = npc.gameObject.AddComponent<WorldInteractionActor>();
            actor.Bind(this, matchId, eventId, npc.name, actorType);
            actors.Add(actor);
        }

        if (actors.Count == 0)
        {
            CreateActor("练习赛 / 正式赛1", WorldInteractionType.Match, GetCatMatchId(), null, new Vector3(-7f, 1f, -1f));
            CreateActor("正式赛2 / 正式赛3", WorldInteractionType.Match,
                session.IsMatchComplete("first_light_public_02") ? "first_light_public_03" : "first_light_public_02",
                null, new Vector3(-7f, 1f, 10f));
            CreateActor("城市冠军赛", WorldInteractionType.Match, "first_light_champion", null, new Vector3(0f, 1f, 20f));
        }
        CreateActor("卡牌商店", WorldInteractionType.Shop, "first_light_public_04", null, player.position + new Vector3(3f, 0f, 2f));
    }

    // 猫姬的挑战：练习赛打过之后换成正式赛1
    private string GetCatMatchId()
    {
        return session.IsMatchComplete("first_light_practice") ? "first_light_public_01" : "first_light_practice";
    }

    // 把主角摆进场景并配好移动参数
    private void SetupPlayer()
    {
        AmiyaCharacter character = FindObjectOfType<AmiyaCharacter>(true);
        if (character == null) return;
        character.gameObject.SetActive(true);
        character.gameplayCamera = gameplayCamera;
        character.moveSpeed = 4f;
        CharacterController controller = character.GetComponent<CharacterController>();
        if (controller == null) controller = character.gameObject.AddComponent<CharacterController>();
        controller.height = 1.8f;
        controller.radius = 0.32f;
        controller.center = Vector3.up * 0.9f;
        controller.skinWidth = 0.03f;
        controller.stepOffset = 0.25f;
        SpriteRenderer renderer = character.GetComponent<SpriteRenderer>();
        if (renderer != null) renderer.sortingOrder = 50;
        HD2DBuildingOcclusion occlusion = character.GetComponent<HD2DBuildingOcclusion>();
        if (occlusion == null) occlusion = character.gameObject.AddComponent<HD2DBuildingOcclusion>();
        occlusion.Bind(gameplayCamera, character.transform);
        player = character.transform;
        if (!playerInitialPositionReady)
        {
            playerInitialPosition = player.position;
            playerInitialPositionReady = true;
        }
    }

    // 在场景里造一个可交互的 NPC
    private void CreateActor(string label, WorldInteractionType type, string matchId, string eventId, Vector3 position)
    {
        var actorObject = new GameObject("NPC - " + label);
        actorObject.transform.position = position;
        SphereCollider trigger = actorObject.AddComponent<SphereCollider>();
        trigger.isTrigger = true;
        trigger.radius = 0.65f;
        SpriteRenderer renderer = actorObject.AddComponent<SpriteRenderer>();
        renderer.color = type == WorldInteractionType.Match ? new Color(0.95f, 0.75f, 0.25f) : Color.white;
        WorldInteractionActor actor = actorObject.AddComponent<WorldInteractionActor>();
        actor.Bind(this, matchId, eventId, label, type);
        actors.Add(actor);
    }
}
