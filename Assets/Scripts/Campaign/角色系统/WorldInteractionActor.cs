using UnityEngine;

public sealed class WorldInteractionActor : MonoBehaviour
{
    private CharacterGM characterGM;
    internal string id;
    internal string displayName;
    internal WorldInteractionType type;
    internal string alternateId;
    public Sprite[] idleFrames;
    public float animationFPS = 8f;
    private SpriteRenderer spriteRenderer;
    private int frame;
    private float timer;

    internal void Bind(CharacterGM characterManager, string actorId, string label, WorldInteractionType actorType)
    {
        characterGM = characterManager;
        id = actorId;
        displayName = label;
        type = actorType;
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    internal void SetIdleFrames(Sprite[] frames, float fps)
    {
        idleFrames = frames;
        animationFPS = Mathf.Max(1f, fps);
        frame = 0;
        timer = 0f;
        if (spriteRenderer != null && idleFrames != null && idleFrames.Length > 0)
            spriteRenderer.sprite = idleFrames[0];
    }

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
