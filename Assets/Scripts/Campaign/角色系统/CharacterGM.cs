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
        player.GetComponent<PlayerCharacter>()?.SetSafePosition();
    }

    // 按场景中明确配置的角色注册交互，不再用名字覆盖对话和赛事。
    public void CreateWorldActors()
    {
        if (actors.Count > 0) return;
        foreach (WorldInteractionActor actor in FindObjectsOfType<WorldInteractionActor>(true))
            if (actor.gameObject.scene == gameObject.scene) actors.Add(actor);
    }

    // 把主角摆进场景并配好移动参数
    private void SetupPlayer()
    {
        PlayerCharacter character = FindObjectOfType<PlayerCharacter>(true);
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

}
