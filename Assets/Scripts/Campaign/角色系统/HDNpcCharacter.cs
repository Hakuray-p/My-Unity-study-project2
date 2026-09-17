using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
[DisallowMultipleComponent]
// HD 场景里的 NPC，从一张拼图贴图里切帧播放待机动画
public sealed class HDNpcCharacter : MonoBehaviour
{
    [SerializeField] private string resourcePath = "Ark Image/NPC/npc_archer_idle"; // 拼图贴图的 Resources 路径
    [SerializeField] private int columns = 3; // 拼图列数
    [SerializeField] private int rows = 3; // 拼图行数
    [SerializeField] private int frameCount = 8; // 实际使用的帧数
    [SerializeField] private float animationFPS = 8f; // 待机动画帧率
    [SerializeField] private float pixelsPerUnit = 225.45f; // 每单位像素数
    [SerializeField] private float pivotY = 0.08f; // 轴心在单帧里的纵向位置
    [SerializeField] private int sortingOrder = 45; // 渲染排序层
    [SerializeField] private bool autoFaceCamera; // 是否自动面向相机
    [SerializeField] private Texture2D leftIdleTexture; // 朝左待机贴图
    [SerializeField] private Texture2D rightIdleTexture; // 朝右待机贴图
    [SerializeField] private Texture2D standTexture; // 正面站姿贴图
    [SerializeField, Min(0.1f)] private float authoredCharacterHeight = 2.2f; // 独立贴图的统一世界高度

    private SpriteRenderer spriteRenderer; // 角色渲染器
    private Sprite[] frames; // 切好的动画帧
    private Texture2D loadedSheet; // 已加载的拼图贴图
    private Sprite leftIdleSprite; // 朝左待机图片
    private Sprite rightIdleSprite; // 朝右待机图片
    private Sprite standSprite; // 正面站姿图片
    private Transform player; // 场景玩家
    private int currentFrame; // 当前帧序号
    private float frameTimer; // 帧计时

    // 补齐渲染器并重建动画帧
    private void OnEnable()
    {
        EnsureRenderer();
        RebuildFrames();
        ApplyFrame();
#if UNITY_EDITOR
        if (!Application.isPlaying) ScheduleEditorFrameApply();
#endif
    }

    // 编辑器里改参数时夹紧数值并重建帧
    private void OnValidate()
    {
        columns = Mathf.Max(1, columns);
        rows = Mathf.Max(1, rows);
        frameCount = Mathf.Clamp(frameCount, 1, columns * rows);
        animationFPS = Mathf.Max(1f, animationFPS);
        pixelsPerUnit = Mathf.Max(1f, pixelsPerUnit);
        pivotY = Mathf.Clamp01(pivotY);
        authoredCharacterHeight = Mathf.Max(0.1f, authoredCharacterHeight);
        EnsureRenderer();
        RebuildFrames();
#if UNITY_EDITOR
        ScheduleEditorFrameApply();
#else
        ApplyFrame();
#endif
    }

    // 每帧转向相机并推进动画
    private void Update()
    {
        if (autoFaceCamera) FaceCamera();
        if (leftIdleSprite != null && rightIdleSprite != null)
        {
            ApplyDirectionalIdle();
            return;
        }

        if (!Application.isPlaying || frames == null || frames.Length == 0) return;
        frameTimer += Time.deltaTime * animationFPS;
        while (frameTimer >= 1f)
        {
            frameTimer -= 1f;
            currentFrame = (currentFrame + 1) % frames.Length;
        }
        ApplyFrame();
    }

    // 没有渲染器就补一个
    private void EnsureRenderer()
    {
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null) spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sortingOrder = sortingOrder;
        spriteRenderer.color = Color.white;
    }

    // 从整张图切出动画帧
    private void RebuildFrames()
    {
        if (leftIdleTexture != null && rightIdleTexture != null && standTexture != null)
        {
            leftIdleSprite = CreateStandaloneSprite(leftIdleTexture);
            rightIdleSprite = CreateStandaloneSprite(rightIdleTexture);
            standSprite = CreateStandaloneSprite(standTexture);
            loadedSheet = null;
            frames = null;
            return;
        }

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
            var rect = new Rect(column * cellWidth, textureRow * cellHeight, cellWidth, cellHeight);
            frames[index] = Sprite.Create(sheet, rect, new Vector2(0.5f, pivotY), pixelsPerUnit);
            frames[index].name = name + "_Idle_" + index;
        }
    }

    // 把独立角色贴图转换成统一大小并对齐脚底的图片
    private Sprite CreateStandaloneSprite(Texture2D texture)
    {
        Color32[] pixels = texture.GetPixels32();
        int minX = texture.width;
        int minY = texture.height;
        int maxX = -1;
        int maxY = -1;
        for (int y = 0; y < texture.height; y++)
        {
            int row = y * texture.width;
            for (int x = 0; x < texture.width; x++)
            {
                if (pixels[row + x].a <= 12) continue;
                if (x < minX) minX = x;
                if (y < minY) minY = y;
                if (x > maxX) maxX = x;
                if (y > maxY) maxY = y;
            }
        }

        var rect = new Rect(0f, 0f, texture.width, texture.height);
        var pivot = new Vector2((minX + maxX + 1) / (2f * texture.width), minY / (float)texture.height);
        float normalizedPixelsPerUnit = (maxY - minY + 1) / authoredCharacterHeight;
        Sprite sprite = Sprite.Create(texture, rect, pivot, normalizedPixelsPerUnit);
        sprite.name = name + "_" + texture.name;
        return sprite;
    }

    // 根据玩家在画面中的左右位置显示对应待机图
    private void ApplyDirectionalIdle()
    {
        if (!Application.isPlaying)
        {
            spriteRenderer.sprite = standSprite;
            return;
        }

        if (player == null) player = FindObjectOfType<PlayerCharacter>(true)?.transform;
        if (player == null)
        {
            spriteRenderer.sprite = standSprite;
            return;
        }

        Camera camera = HD2DSceneGM.GameplayCamera;
        Vector3 screenRight = camera != null ? camera.transform.right : Vector3.right;
        float playerSide = Vector3.Dot(player.position - transform.position, screenRight);
        spriteRenderer.sprite = Mathf.Abs(playerSide) <= 0.25f ? standSprite :
            playerSide < 0f ? rightIdleSprite : leftIdleSprite;
    }

    // 把当前帧画到渲染器上
    private void ApplyFrame()
    {
        if (standSprite != null)
        {
            ApplyDirectionalIdle();
            return;
        }

        if (spriteRenderer == null || frames == null || frames.Length == 0) return;
        currentFrame = Mathf.Clamp(currentFrame, 0, frames.Length - 1);
        spriteRenderer.sprite = frames[currentFrame];
    }

#if UNITY_EDITOR
    // 延迟到编辑器下一帧刷新，避免在 OnValidate 里直接建资源
    private void ScheduleEditorFrameApply()
    {
        EditorApplication.delayCall -= ApplyEditorFrame;
        EditorApplication.delayCall += ApplyEditorFrame;
    }

    // 编辑器下重建帧并标脏
    private void ApplyEditorFrame()
    {
        if (this == null || Application.isPlaying) return;
        EnsureRenderer();
        RebuildFrames();
        ApplyFrame();
        EditorUtility.SetDirty(this);
    }
#endif

    // 让 NPC 朝向摄像机
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
