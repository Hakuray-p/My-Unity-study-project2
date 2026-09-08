using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
[DisallowMultipleComponent]
public sealed class HDNpcCharacter : MonoBehaviour
{
    [SerializeField] private string resourcePath = "Ark Image/NPC/npc_archer_idle";
    [SerializeField] private int columns = 3;
    [SerializeField] private int rows = 3;
    [SerializeField] private int frameCount = 8;
    [SerializeField] private float animationFPS = 8f;
    [SerializeField] private float pixelsPerUnit = 225.45f;
    [SerializeField] private float pivotY = 0.08f;
    [SerializeField] private int sortingOrder = 45;
    [SerializeField] private bool autoFaceCamera;

    private SpriteRenderer spriteRenderer;
    private Sprite[] frames;
    private Texture2D loadedSheet;
    private int currentFrame;
    private float frameTimer;

    private void OnEnable()
    {
        EnsureRenderer();
        RebuildFrames();
        ApplyFrame();
#if UNITY_EDITOR
        if (!Application.isPlaying) ScheduleEditorFrameApply();
#endif
    }

    private void OnValidate()
    {
        columns = Mathf.Max(1, columns);
        rows = Mathf.Max(1, rows);
        frameCount = Mathf.Clamp(frameCount, 1, columns * rows);
        animationFPS = Mathf.Max(1f, animationFPS);
        pixelsPerUnit = Mathf.Max(1f, pixelsPerUnit);
        pivotY = Mathf.Clamp01(pivotY);
        EnsureRenderer();
        RebuildFrames();
#if UNITY_EDITOR
        ScheduleEditorFrameApply();
#else
        ApplyFrame();
#endif
    }

    private void Update()
    {
        if (autoFaceCamera) FaceCamera();
        if (!Application.isPlaying || frames == null || frames.Length == 0) return;
        frameTimer += Time.deltaTime * animationFPS;
        while (frameTimer >= 1f)
        {
            frameTimer -= 1f;
            currentFrame = (currentFrame + 1) % frames.Length;
        }
        ApplyFrame();
    }

    private void EnsureRenderer()
    {
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null) spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sortingOrder = sortingOrder;
        spriteRenderer.color = Color.white;
    }

    private void RebuildFrames()
    {
        if (string.IsNullOrEmpty(resourcePath)) return;
        Texture2D sheet = Resources.Load<Texture2D>(resourcePath);
        if (sheet == null || sheet == loadedSheet && frames != null && frames.Length == frameCount) return;

        loadedSheet = sheet;
        frames = new Sprite[frameCount];
        int cellWidth = sheet.width / columns;
        int cellHeight = sheet.height / rows;
        if (cellWidth < 1 || cellHeight < 1) return;

        for (int index = 0; index < frameCount; index++)
        {
            int rowFromTop = index / columns;
            int column = index % columns;
            int textureRow = rows - 1 - rowFromTop;
            Rect rect = new Rect(column * cellWidth, textureRow * cellHeight, cellWidth, cellHeight);
            frames[index] = Sprite.Create(sheet, rect, new Vector2(0.5f, pivotY), pixelsPerUnit);
            frames[index].name = name + "_Idle_" + index;
        }
    }

    private void ApplyFrame()
    {
        if (spriteRenderer == null || frames == null || frames.Length == 0) return;
        currentFrame = Mathf.Clamp(currentFrame, 0, frames.Length - 1);
        spriteRenderer.sprite = frames[currentFrame];
    }

#if UNITY_EDITOR
    private void ScheduleEditorFrameApply()
    {
        EditorApplication.delayCall -= ApplyEditorFrame;
        EditorApplication.delayCall += ApplyEditorFrame;
    }

    private void ApplyEditorFrame()
    {
        if (this == null || Application.isPlaying) return;
        EnsureRenderer();
        RebuildFrames();
        ApplyFrame();
        EditorUtility.SetDirty(this);
    }
#endif

    private void FaceCamera()
    {
        Camera camera = HD2DSceneGM.GameplayCamera;
        if (camera == null) camera = Camera.main;
        if (camera == null) return;
        Vector3 towardCamera = Vector3.ProjectOnPlane(camera.transform.position - transform.position, Vector3.up);
        if (towardCamera.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(towardCamera, Vector3.up);
    }
}
