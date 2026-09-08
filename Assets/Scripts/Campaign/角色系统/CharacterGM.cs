using System.Collections.Generic;
using UnityEngine;

public sealed class CharacterGM : MonoBehaviour
{
    private CampaignSession session;
    private Camera gameplayCamera;
    private Transform player;
    private Vector3 playerInitialPosition;
    private bool playerInitialPositionReady;
    private readonly List<WorldInteractionActor> actors = new List<WorldInteractionActor>();

    public Transform Player => player;
    public Vector3 PlayerInitialPosition => playerInitialPosition;
    public IReadOnlyList<WorldInteractionActor> Actors => actors;
    public bool IsReady => player != null;

    public void Initialize(CampaignSession campaignSession, Camera camera)
    {
        session = campaignSession;
        gameplayCamera = camera;
        if (player == null) SetupPlayer();
    }

    public void RestorePlayer()
    {
        if (session.State.playerPosition.sqrMagnitude <= 0.25f) return;
        CharacterController controller = player.GetComponent<CharacterController>();
        if (controller != null) controller.enabled = false;
        player.position = session.State.playerPosition;
        if (controller != null) controller.enabled = true;
        player.GetComponent<AmiyaCharacter>()?.SetSafePosition();
    }

    public void CreateWorldActors()
    {
        if (actors.Count > 0) return;

        HDNpcCharacter[] sceneNpcs = FindObjectsOfType<HDNpcCharacter>(true);
        for (int i = 0; i < sceneNpcs.Length; i++)
        {
            HDNpcCharacter npc = sceneNpcs[i];
            if (npc == null) continue;
            string npcName = npc.name;
            bool isMatchNpc = npcName.Contains("猫姬");
            bool isShopNpc = npcName.Contains("Mrs商人") || npcName.Contains("商人");
            bool isEventNpc = npcName.Contains("黑猫少女");
            string actorId = isMatchNpc ? "first_light_practice" :
                isShopNpc ? "first_light_shop" : isEventNpc ? "first_light_event_01" : null;
            string alternateId = npcName.Contains("猫姬") ? "first_light_public_01" : null;
            WorldInteractionType actorType = isShopNpc ? WorldInteractionType.Shop :
                isMatchNpc ? WorldInteractionType.Match : WorldInteractionType.Event;
            Collider collider = npc.GetComponent<Collider>();
            if (collider == null) collider = npc.gameObject.AddComponent<SphereCollider>();
            collider.isTrigger = true;
            WorldInteractionActor actor = npc.GetComponent<WorldInteractionActor>();
            if (actor == null) actor = npc.gameObject.AddComponent<WorldInteractionActor>();
            actor.Bind(this, actorId, npc.name, actorType);
            actor.alternateId = alternateId;
            actors.Add(actor);
        }

        if (actors.Count == 0)
        {
            CreateActor("练习赛 / 正式赛1", WorldInteractionType.Match, "first_light_practice", "first_light_public_01", new Vector3(-7f, 1f, -1f));
            CreateActor("正式赛2 / 正式赛3", WorldInteractionType.Match, "first_light_public_02", "first_light_public_03", new Vector3(-7f, 1f, 10f));
            CreateActor("城市冠军赛", WorldInteractionType.Match, "first_light_champion", null, new Vector3(0f, 1f, 20f));
        }
        CreateActor("卡牌商店", WorldInteractionType.Shop, "first_light_shop", null, player.position + new Vector3(3f, 0f, 2f));
    }

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

    private void CreateActor(string label, WorldInteractionType type, string id, string alternateId, Vector3 position)
    {
        GameObject actorObject = new GameObject("NPC - " + label);
        actorObject.transform.position = position;
        SphereCollider trigger = actorObject.AddComponent<SphereCollider>();
        trigger.isTrigger = true;
        trigger.radius = 0.65f;
        SpriteRenderer renderer = actorObject.AddComponent<SpriteRenderer>();
        renderer.color = type == WorldInteractionType.Match ? new Color(0.95f, 0.75f, 0.25f) : Color.white;
        WorldInteractionActor actor = actorObject.AddComponent<WorldInteractionActor>();
        actor.Bind(this, id, label, type);
        actor.alternateId = alternateId;
        actors.Add(actor);
    }
}
