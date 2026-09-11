using UnityEngine;

// 世界里可以互动的 NPC，负责朝向相机和播放待机动画
public sealed class WorldInteractionActor : MonoBehaviour
{
    private CharacterGM characterGM; // 所属的角色管理器
    internal string id; // 绑定的比赛 / 商店 / 事件 ID
    internal string displayName; // 显示名
    internal WorldInteractionType type; // 交互类型
    internal string alternateId; // 备用的交互 ID，比如打完练习赛后的正式赛
    public Sprite[] idleFrames; // 待机动画帧
    public float animationFPS = 8f; // 待机动画帧率
    private SpriteRenderer spriteRenderer; // 角色渲染器
    private int frame; // 当前动画帧
    private float timer; // 帧计时

    // 记下所属管理器和自己的身份
    internal void Bind(CharacterGM characterManager, string actorId, string label, WorldInteractionType actorType)
    {
        characterGM = characterManager;
        id = actorId;
        displayName = label;
        type = actorType;
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    // 设置待机动画帧
    internal void SetIdleFrames(Sprite[] frames, float fps)
    {
        idleFrames = frames;
        animationFPS = Mathf.Max(1f, fps);
        frame = 0;
        timer = 0f;
        if (spriteRenderer != null && idleFrames != null && idleFrames.Length > 0)
            spriteRenderer.sprite = idleFrames[0];
    }

    // 每帧面向相机并播放待机动画
    private void Update()
    {
        Camera camera = HD2DSceneGM.GameplayCamera;
        if (camera != null)
        {
            Vector3 towardCamera = Vector3.ProjectOnPlane(camera.transform.position - transform.position, Vector3.up);
            if (towardCamera.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(towardCamera, Vector3.up);
        }
        if (spriteRenderer == null || idleFrames == null || idleFrames.Length == 0) return;
        timer += Time.deltaTime * animationFPS;
        if (timer >= 1f)
        {
            timer = 0f;
            frame = (frame + 1) % idleFrames.Length;
        }
        spriteRenderer.sprite = idleFrames[frame];
    }
}
