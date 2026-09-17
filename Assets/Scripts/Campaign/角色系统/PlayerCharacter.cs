using UnityEngine;

// 城市探索场景中的可操控角色，负责移动、贴地和四向动画
public sealed class PlayerCharacter : MonoBehaviour
{
    [Min(0.1f)] public float moveSpeed = 4f; // 移动速度
    [Min(1f)] public float animationFPS = 8f; // 动画帧率
    public Texture2D downSheet; // 向下行走贴图
    public Texture2D upSheet; // 向上行走贴图
    public Texture2D leftSheet; // 向左行走贴图
    public Texture2D rightSheet; // 向右行走贴图
    public Texture2D downIdleTexture; // 向下待机贴图
    public Texture2D upIdleTexture; // 向上待机贴图
    [Min(1)] public int columns = 4; // 贴图列数
    [Min(1)] public int rows = 4; // 贴图行数
    [Min(1f)] public float sheetPixelsPerUnit = 300f; // 贴图默认每单位像素数
    [Min(0.1f)] public float authoredCharacterHeight = 1.95f; // 角色可见部分的统一世界高度
    public bool normalizeAuthoredFrameSize = true; // 是否统一各帧显示大小和脚底位置
    public Camera gameplayCamera; // 场景主相机
    public LayerMask groundMask = ~0; // 地面检测层
    [Min(0.1f)] public float groundProbeDistance = 3f; // 地面探测距离
    [Min(0f)] public float groundOffset = 0.04f; // 贴地后的高度偏移
    public float minimumSafeY = -5f; // 低于这个高度就判定为掉出场景

    private SpriteRenderer spriteRenderer; // 角色渲染器
    private CharacterController characterController; // 角色控制器
    private Sprite[][] frames; // 四个方向的动画帧
    private int facing; // 当前朝向，0 下 1 上 2 左 3 右
    private int currentFrame; // 当前帧序号
    private float frameTimer; // 帧计时
    private Vector3 lastSafePosition; // 最近一次的安全位置
    private bool hasSafePosition; // 是否记录过安全位置
    private readonly RaycastHit[] groundHits = new RaycastHit[16]; // 地面检测结果缓存
    private Sprite downIdleSprite; // 向下待机图片
    private Sprite upIdleSprite; // 向上待机图片

    // 补齐角色组件并加载四向动画
    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null) spriteRenderer = gameObject.AddComponent<SpriteRenderer>();

        characterController = GetComponent<CharacterController>();
        if (characterController == null) characterController = gameObject.AddComponent<CharacterController>();

        spriteRenderer.sortingOrder = 50;
        lastSafePosition = transform.position;
        hasSafePosition = true;
        LoadFrames();
        SetFacing(0);
        ApplyIdleFrame();
    }

    // 把角色贴到当前地面
    public bool SnapToGround()
    {
        if (!TryGetGround(out RaycastHit hit)) return false;
        SetGroundHeight(hit.point.y);
        return true;
    }

    // 更新角色掉出场景时使用的安全位置
    public void SetSafePosition()
    {
        if (IsInvalidPosition()) return;
        lastSafePosition = transform.position;
        hasSafePosition = true;
    }

    // 每帧处理移动、动画和贴地
    private void Update()
    {
        if (IsInvalidPosition())
        {
            RecoverToSafePosition();
            return;
        }

        if (gameplayCamera == null) gameplayCamera = HD2DSceneGM.GameplayCamera;
        FaceCamera();

        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        if (Mathf.Abs(horizontal) <= 0.01f && Mathf.Abs(vertical) <= 0.01f)
        {
            currentFrame = 0;
            frameTimer = 0f;
            ApplyIdleFrame();
            MaintainGround();
            return;
        }

        Vector3 forward = gameplayCamera != null
            ? Vector3.ProjectOnPlane(gameplayCamera.transform.forward, Vector3.up).normalized
            : Vector3.forward;
        Vector3 right = gameplayCamera != null
            ? Vector3.ProjectOnPlane(gameplayCamera.transform.right, Vector3.up).normalized
            : Vector3.right;
        Vector3 move = forward * vertical + right * horizontal;
        if (move.sqrMagnitude > 1f) move.Normalize();

        if (characterController.enabled)
        {
            bool hasGround = TryGetGround(out RaycastHit groundBeforeMove);
            if (hasGround) SetGroundHeight(groundBeforeMove.point.y);

            Vector3 velocity = move * moveSpeed;
            velocity.y = hasGround || characterController.isGrounded ? -0.5f : 0f;
            characterController.Move(velocity * Time.deltaTime);

            if (TryGetGround(out RaycastHit groundAfterMove)) SetGroundHeight(groundAfterMove.point.y);
        }
        else
        {
            transform.position += move * moveSpeed * Time.deltaTime;
            SnapToGround();
        }

        UpdateFacing(move, forward, right);
        AdvanceAnimation();
        ApplyFrame();
        if (IsInvalidPosition()) RecoverToSafePosition();
    }

    // 让角色始终正对场景相机
    private void FaceCamera()
    {
        if (gameplayCamera == null) return;
        Vector3 towardCamera = Vector3.ProjectOnPlane(gameplayCamera.transform.forward, Vector3.up);
        if (towardCamera.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(towardCamera.normalized, Vector3.up);
    }

    // 向下检测最近的地面
    private bool TryGetGround(out RaycastHit closestHit)
    {
        closestHit = default;
        Vector3 origin = transform.position + Vector3.up * 0.75f;
        float distance = Mathf.Max(groundProbeDistance, 0.5f) + 0.75f;
        int hitCount = Physics.RaycastNonAlloc(origin, Vector3.down, groundHits, distance, groundMask,
            QueryTriggerInteraction.Ignore);
        float nearestDistance = float.MaxValue;
        bool found = false;
        for (int index = 0; index < hitCount; index++)
        {
            RaycastHit hit = groundHits[index];
            if (hit.collider == null || hit.collider.transform == transform || hit.collider.transform.IsChildOf(transform))
                continue;
            if (hit.distance >= nearestDistance) continue;
            nearestDistance = hit.distance;
            closestHit = hit;
            found = true;
        }
        return found;
    }

    // 把角色移动到地面高度并记录安全位置
    private void SetGroundHeight(float groundY)
    {
        Vector3 position = transform.position;
        position.y = groundY + groundOffset;
        transform.position = position;
        lastSafePosition = position;
        hasSafePosition = true;
    }

    // 角色静止时维持贴地状态
    private void MaintainGround()
    {
        if (TryGetGround(out RaycastHit hit))
        {
            SetGroundHeight(hit.point.y);
            return;
        }

        if (transform.position.y < minimumSafeY) RecoverToSafePosition();
    }

    // 判断角色位置是否已经失效
    private bool IsInvalidPosition()
    {
        return float.IsNaN(transform.position.x) || float.IsNaN(transform.position.y) ||
               float.IsNaN(transform.position.z) || float.IsInfinity(transform.position.x) ||
               float.IsInfinity(transform.position.y) || float.IsInfinity(transform.position.z) ||
               transform.position.y < minimumSafeY;
    }

    // 把角色送回最近一次安全位置
    private void RecoverToSafePosition()
    {
        if (!hasSafePosition) return;
        if (characterController.enabled)
        {
            characterController.enabled = false;
            transform.position = lastSafePosition;
            characterController.enabled = true;
        }
        else
        {
            transform.position = lastSafePosition;
        }
        frameTimer = 0f;
    }

    // 加载四个方向的行走帧
    private void LoadFrames()
    {
        frames = new Sprite[4][];
        frames[0] = CreateSheetFrames(downSheet);
        frames[1] = CreateSheetFrames(upSheet);
        frames[2] = CreateSheetFrames(leftSheet);
        frames[3] = CreateSheetFrames(rightSheet);
        downIdleSprite = CreateIdleSprite(downIdleTexture);
        upIdleSprite = CreateIdleSprite(upIdleTexture);
    }

    // 把单张待机贴图转换成与行走动画相同大小的图片
    private Sprite CreateIdleSprite(Texture2D texture)
    {
        var rect = new Rect(0f, 0f, texture.width, texture.height);
        float pixelsPerUnit = sheetPixelsPerUnit;
        var pivot = new Vector2(0.5f, 0.08f);
        if (normalizeAuthoredFrameSize && TryGetVisibleFrameBounds(texture.GetPixels32(), texture.width, rect,
            out _, out int minY, out _, out int maxY))
        {
            pixelsPerUnit = (maxY - minY + 1) / authoredCharacterHeight;
            pivot = new Vector2(0.5f, minY / (float)texture.height);
        }

        return Sprite.Create(texture, rect, pivot, pixelsPerUnit);
    }

    // 把一张行走贴图切成动画帧
    private Sprite[] CreateSheetFrames(Texture2D sheet)
    {
        int cellWidth = sheet.width / columns;
        int cellHeight = sheet.height / rows;
        var result = new Sprite[columns * rows];
        Color32[] pixels = normalizeAuthoredFrameSize ? sheet.GetPixels32() : null;
        int frame = 0;

        for (int rowFromTop = 0; rowFromTop < rows; rowFromTop++)
        {
            int textureRow = rows - 1 - rowFromTop;
            for (int column = 0; column < columns; column++)
            {
                var rect = new Rect(column * cellWidth, textureRow * cellHeight, cellWidth, cellHeight);
                float framePixelsPerUnit = sheetPixelsPerUnit;
                var pivot = new Vector2(0.5f, 0.08f);
                if (pixels != null && TryGetVisibleFrameBounds(pixels, sheet.width, rect,
                    out int minX, out int minY, out int maxX, out int maxY))
                {
                    int visibleHeight = maxY - minY + 1;
                    framePixelsPerUnit = visibleHeight / authoredCharacterHeight;
                    pivot = new Vector2(0.5f, minY / (float)cellHeight);
                }

                result[frame] = Sprite.Create(sheet, rect, pivot, framePixelsPerUnit);
                frame++;
            }
        }

        return result;
    }

    // 在单帧中查找角色可见像素范围
    private bool TryGetVisibleFrameBounds(Color32[] pixels, int textureWidth, Rect frameRect,
        out int minX, out int minY, out int maxX, out int maxY)
    {
        minX = Mathf.RoundToInt(frameRect.width);
        minY = Mathf.RoundToInt(frameRect.height);
        maxX = -1;
        maxY = -1;

        int startX = Mathf.RoundToInt(frameRect.x);
        int startY = Mathf.RoundToInt(frameRect.y);
        int frameWidth = Mathf.RoundToInt(frameRect.width);
        int frameHeight = Mathf.RoundToInt(frameRect.height);
        for (int y = 0; y < frameHeight; y++)
        {
            int textureRow = (startY + y) * textureWidth;
            for (int x = 0; x < frameWidth; x++)
            {
                Color32 pixel = pixels[textureRow + startX + x];
                if (pixel.a <= 12) continue;
                if (x < minX) minX = x;
                if (y < minY) minY = y;
                if (x > maxX) maxX = x;
                if (y > maxY) maxY = y;
            }
        }

        return maxX >= minX && maxY >= minY;
    }

    // 按移动方向切换角色朝向
    private void UpdateFacing(Vector3 move, Vector3 forward, Vector3 right)
    {
        float forwardAmount = Vector3.Dot(move, forward);
        float rightAmount = Vector3.Dot(move, right);
        if (Mathf.Abs(forwardAmount) >= Mathf.Abs(rightAmount))
            SetFacing(forwardAmount >= 0f ? 1 : 0);
        else
            SetFacing(rightAmount >= 0f ? 3 : 2);
    }

    // 切换朝向并从第一帧开始播放
    private void SetFacing(int direction)
    {
        if (facing != direction)
        {
            facing = direction;
            currentFrame = 0;
            frameTimer = 0f;
        }
        ApplyFrame();
    }

    // 按帧率推进当前方向的行走动画
    private void AdvanceAnimation()
    {
        frameTimer += Time.deltaTime * animationFPS;
        while (frameTimer >= 1f)
        {
            frameTimer -= 1f;
            currentFrame = (currentFrame + 1) % frames[facing].Length;
        }
    }

    // 显示当前上下方向的专用待机图片
    private void ApplyIdleFrame()
    {
        if (facing == 0)
        {
            spriteRenderer.sprite = downIdleSprite;
            return;
        }

        if (facing == 1)
        {
            spriteRenderer.sprite = upIdleSprite;
            return;
        }

        ApplyFrame();
    }

    // 把当前动画帧显示到角色渲染器
    private void ApplyFrame()
    {
        if (frames == null || frames[facing] == null || frames[facing].Length == 0) return;
        spriteRenderer.sprite = frames[facing][currentFrame];
    }
}
