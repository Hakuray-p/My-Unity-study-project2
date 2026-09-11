using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// HD 城市探索场景中的可操作像素角色。
/// </summary>
// HD 城市探索场景中的可移动像素角色，负责移动、贴地和四向动画
public sealed class AmiyaCharacter : MonoBehaviour
{
    [Min(0.1f)] public float moveSpeed = 4f; // 移动速度
    [Min(1f)] public float animationFPS = 8f; // 动画帧率
    public string resourceBase = "Ark Image/Amiya/amiya"; // 默认贴图的 Resources 前缀
    public string fallbackResource = "Ark Image/阿米娅"; // 找不到贴图时的兜底资源
    [Min(1)] public int frameCount = 8; // 每个方向的帧数
    [Min(1f)] public float pixelsPerUnit = 100f; // 每单位像素数
    [Min(1f)] public float forwardTailFrameHold = 2.5f; // 朝前动画末帧的停留倍数
    [Header("Authored sprite sheets")]
    public Texture2D downRightSheet; // 朝右下的贴图
    public Texture2D downLeftSheet; // 朝左下的贴图
    public Texture2D leftSheet; // 朝左的贴图
    public Texture2D rightSheet; // 朝右的贴图
    public Texture2D backSheet; // 背面的贴图
    [Min(1f)] public float sheetPixelsPerUnit = 160f; // 拼图的每单位像素数
    [Min(1f)] public float backSheetPixelsPerUnit = 395f; // 背面拼图的每单位像素数
    [Header("Authored frame sizing")]
    [Tooltip("World-space height shared by the visible pixels of every authored frame.")]
    [Min(0.1f)] public float authoredCharacterHeight = 1.95f; // 各帧统一到的世界高度
    public bool normalizeAuthoredFrameSize = true; // 是否按可见像素统一帧尺寸
    public Camera gameplayCamera; // 场景主相机
    [Header("Grounding")]
    public LayerMask groundMask = ~0; // 地面检测层
    [Min(0.1f)] public float groundProbeDistance = 3f; // 地面探测距离
    [Min(0f)] public float groundOffset = 0.04f; // 贴地后的高度偏移
    public float minimumSafeY = -5f; // 低于这个高度就判定为掉出场景

    private SpriteRenderer spriteRenderer; // 角色渲染器
    private CharacterController characterController; // 角色控制器
    private Sprite[][] frames; // 四个方向的动画帧
    private Sprite[] downRightFrames; // 朝右下的动画帧
    private Sprite[] downLeftFrames; // 朝左下的动画帧
    private bool usingAuthoredSheets; // 是否在用 Inspector 指定的大图
    private bool useLeftDownSheet; // 朝下时是否改用左下的那张图
    private int facing; // 当前朝向，0 下 1 上 2 左 3 右
    private int currentFrame; // 当前帧序号
    private float frameTimer; // 帧计时
    private bool hasShownForwardTurn; // 是否已经播过第一次转身
    private bool forwardTurnPlaying; // 是否正在播转身动画
    private bool wasMovingForward; // 上一帧是否在朝前移动
    private int forwardTurnFrame; // 转身动画的当前帧
    private Vector3 lastSafePosition; // 最近一次的安全位置
    private bool hasSafePosition; // 是否记录过安全位置
    private readonly RaycastHit[] groundHits = new RaycastHit[16]; // 地面检测结果缓存

    // 补齐渲染器和角色控制器，记下初始安全位置
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
    }

    /// <summary>
    /// Performs an initial grounding pass after the runtime has configured the
    /// CharacterController. This is also used by the scene bootstrap as a safety net.
    /// </summary>
    public bool SnapToGround()
    {
        if (!TryGetGround(out RaycastHit hit)) return false;
        SetGroundHeight(hit.point.y);
        return true;
    }

    /// <summary>
    /// Updates the fallback position used by the grounding/invalid-position
    /// guard after an external scene or save-system reposition.
    /// </summary>
    public void SetSafePosition()
    {
        if (IsInvalidPosition()) return;
        lastSafePosition = transform.position;
        hasSafePosition = true;
    }

    // 每帧处理移动、动画和落地
    private void Update()
    {
        if (spriteRenderer == null) return;

        if (IsInvalidPosition())
        {
            RecoverToSafePosition();
            return;
        }

        if (gameplayCamera == null) gameplayCamera = HD2DSceneGM.GameplayCamera;
        FaceCamera();

        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        bool moving = Mathf.Abs(horizontal) > 0.01f || Mathf.Abs(vertical) > 0.01f;
        if (!moving)
        {
            currentFrame = 0;
            frameTimer = 0f;
            forwardTurnPlaying = false;
            wasMovingForward = false;
            ApplyFrame();
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

        if (characterController != null && characterController.enabled)
        {
            bool hasGround = TryGetGround(out RaycastHit groundBeforeMove);
            if (hasGround) SetGroundHeight(groundBeforeMove.point.y);

            Vector3 velocity = move * moveSpeed;
            velocity.y = hasGround || characterController.isGrounded ? -0.5f : 0f;
            characterController.Move(velocity * Time.deltaTime);

            if (TryGetGround(out RaycastHit groundAfterMove))
                SetGroundHeight(groundAfterMove.point.y);
        }
        else
        {
            transform.position += move * moveSpeed * Time.deltaTime;
            SnapToGround();
        }

        UpdateFacing(move, forward, right);
        float forwardAmount = Vector3.Dot(move, forward);
        float rightAmount = Vector3.Dot(move, right);
        bool movingForward = forwardAmount > 0.5f && Mathf.Abs(forwardAmount) >= Mathf.Abs(rightAmount);
        if (movingForward)
        {
            AdvanceForwardAnimation();
            wasMovingForward = true;
        }
        else
        {
            forwardTurnPlaying = false;
            wasMovingForward = false;
            AdvanceStandardAnimation();
        }
        ApplyFrame();

        if (IsInvalidPosition()) RecoverToSafePosition();
    }

    // 让角色朝向摄像机
    private void FaceCamera()
    {
        if (gameplayCamera == null) return;
        Vector3 towardCamera = Vector3.ProjectOnPlane(-gameplayCamera.transform.forward, Vector3.up);
        if (towardCamera.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(towardCamera.normalized, Vector3.up);
    }

    // 向下打射线取最近的地面高度
    private bool TryGetGround(out RaycastHit closestHit)
    {
        closestHit = default;
        Vector3 origin = transform.position + Vector3.up * 0.75f;
        float distance = Mathf.Max(groundProbeDistance, 0.5f) + 0.75f;
        int hitCount = Physics.RaycastNonAlloc(origin, Vector3.down, groundHits, distance, groundMask,
            QueryTriggerInteraction.Ignore);
        float nearestDistance = float.MaxValue;
        bool found = false;
        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit = groundHits[i];
            if (hit.collider == null || hit.collider.transform == transform || hit.collider.transform.IsChildOf(transform))
                continue;
            if (hit.distance < nearestDistance)
            {
                nearestDistance = hit.distance;
                closestHit = hit;
                found = true;
            }
        }
        return found;
    }

    // 把角色贴到地面并记成安全位置
    private void SetGroundHeight(float groundY)
    {
        Vector3 position = transform.position;
        position.y = groundY + groundOffset;
        transform.position = position;
        lastSafePosition = position;
        hasSafePosition = true;
    }

    // 站着不动时也把角色贴回地面，掉太深就送回安全位置
    private void MaintainGround()
    {
        if (TryGetGround(out RaycastHit hit))
        {
            SetGroundHeight(hit.point.y);
            return;
        }

        if (transform.position.y < minimumSafeY) RecoverToSafePosition();
    }

    // 位置是否变成 NaN / 无穷大或掉出场景
    private bool IsInvalidPosition()
    {
        return float.IsNaN(transform.position.x) || float.IsNaN(transform.position.y) ||
               float.IsNaN(transform.position.z) || float.IsInfinity(transform.position.x) ||
               float.IsInfinity(transform.position.y) || float.IsInfinity(transform.position.z) ||
               transform.position.y < minimumSafeY;
    }

    // 把角色送回最近一次的安全位置
    private void RecoverToSafePosition()
    {
        if (!hasSafePosition) return;
        if (characterController != null && characterController.enabled)
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

    // 加载四个朝向的行走帧
    private void LoadFrames()
    {
        if (LoadAuthoredSheets()) return;

        frames = new Sprite[4][];
        frames[0] = LoadDirection("down");
        frames[1] = LoadDirection("up");
        frames[2] = LoadDirection("left");
        frames[3] = LoadDirection("right");

        Sprite fallback = Resources.Load<Sprite>(fallbackResource);
        if (fallback == null)
        {
            Texture2D texture = Resources.Load<Texture2D>(fallbackResource);
            if (texture != null)
            {
                fallback = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height),
                    new Vector2(0.5f, 0.08f), pixelsPerUnit);
            }
        }

        if (fallback == null) return;
        for (int direction = 0; direction < frames.Length; direction++)
        {
            for (int frame = 0; frame < frames[direction].Length; frame++)
            {
                if (frames[direction][frame] == null) frames[direction][frame] = fallback;
            }
        }
    }

    // 按美术给的整张图切帧，切得出来就用它
    private bool LoadAuthoredSheets()
    {
        if (downRightSheet == null && downLeftSheet == null && leftSheet == null &&
            rightSheet == null && backSheet == null)
            return false;

        usingAuthoredSheets = true;
        frames = new Sprite[4][];
        // Both downward sheets are laid out in a 3x3 grid, but the final
        // cell is intentionally empty. Do not expose it as a blank frame.
        downRightFrames = CreateSheetFrames(downRightSheet, 3, 3, sheetPixelsPerUnit, 8);
        downLeftFrames = CreateSheetFrames(downLeftSheet, 3, 3, sheetPixelsPerUnit, 8);
        frames[0] = downRightFrames != null && downRightFrames.Length > 0
            ? downRightFrames
            : downLeftFrames;
        frames[1] = CreateSheetFrames(backSheet, 4, 2, backSheetPixelsPerUnit);
        frames[2] = CreateSheetFrames(leftSheet, 4, 4, sheetPixelsPerUnit);
        frames[3] = CreateSheetFrames(rightSheet, 4, 4, sheetPixelsPerUnit);

        bool hasAnyFrames = false;
        for (int i = 0; i < frames.Length; i++)
            hasAnyFrames |= frames[i] != null && frames[i].Length > 0;
        return hasAnyFrames;
    }

    // 把一张拼图切成帧，可选按可见像素统一尺寸和轴心
    private Sprite[] CreateSheetFrames(Texture2D sheet, int columns, int rows, float sheetPpu, int frameLimit = -1)
    {
        if (sheet == null || columns < 1 || rows < 1) return null;

        int cellWidth = sheet.width / columns;
        int cellHeight = sheet.height / rows;
        if (cellWidth < 1 || cellHeight < 1) return null;

        int capacity = frameLimit > 0 ? Mathf.Min(frameLimit, columns * rows) : columns * rows;
        var result = new List<Sprite>(capacity);
        Color32[] pixels = null;
        if (normalizeAuthoredFrameSize && sheet.isReadable)
            pixels = sheet.GetPixels32();

        for (int rowFromTop = 0; rowFromTop < rows; rowFromTop++)
        {
            int textureRow = rows - 1 - rowFromTop;
            for (int column = 0; column < columns; column++)
            {
                if (frameLimit > 0 && result.Count >= frameLimit)
                    return result.ToArray();

                var rect = new Rect(column * cellWidth, textureRow * cellHeight, cellWidth, cellHeight);
                float framePpu = sheetPpu;
                var pivot = new Vector2(0.5f, 0.08f);
                if (pixels != null && TryGetVisibleFrameBounds(pixels, sheet.width, rect,
                    out int minX, out int minY, out int maxX, out int maxY))
                {
                    int visibleHeight = maxY - minY + 1;
                    if (normalizeAuthoredFrameSize && authoredCharacterHeight > 0.01f)
                        framePpu = visibleHeight / authoredCharacterHeight;

                    // Keep the lowest visible pixel on the GameObject origin,
                    // preventing foot jitter when frames have different padding.
                    pivot = new Vector2(0.5f, minY / (float)cellHeight);
                }

                Sprite sprite = Sprite.Create(sheet, rect, pivot, framePpu);
                result.Add(sprite);
            }
        }

        return result.ToArray();
    }

    // 在单帧里找出非透明像素的包围盒
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

    // 加载某个朝向的序列帧
    private Sprite[] LoadDirection(string direction)
    {
        Sprite[] result = new Sprite[frameCount];
        for (int i = 0; i < frameCount; i++)
        {
            string path = resourceBase + "_" + direction + "_" + i;
            Sprite sprite = Resources.Load<Sprite>(path);
            if (sprite == null)
            {
                Texture2D texture = Resources.Load<Texture2D>(path);
                if (texture != null)
                {
                    sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height),
                        new Vector2(0.5f, 0.08f), pixelsPerUnit);
                }
            }
            result[i] = sprite;
        }
        return result;
    }

    // 按移动方向决定朝哪一面
    private void UpdateFacing(Vector3 move, Vector3 forward, Vector3 right)
    {
        float forwardAmount = Vector3.Dot(move, forward);
        float rightAmount = Vector3.Dot(move, right);
        if (Mathf.Abs(forwardAmount) >= Mathf.Abs(rightAmount))
        {
            SetFacing(forwardAmount >= 0f ? 1 : 0);
            if (facing == 0) useLeftDownSheet = rightAmount < 0f;
        }
        else
            SetFacing(rightAmount >= 0f ? 2 : 3);
    }

    // 切换朝向，换向时重置帧
    private void SetFacing(int direction)
    {
        int nextFacing = Mathf.Clamp(direction, 0, 3);
        if (facing != nextFacing)
        {
            facing = nextFacing;
            currentFrame = 0;
            frameTimer = 0f;
        }
        ApplyFrame();
    }

    // 按帧率推进当前朝向的行走动画
    private void AdvanceStandardAnimation()
    {
        if (usingAuthoredSheets && facing == 0)
        {
            Sprite[] downFrames = GetCurrentDirectionFrames();
            int downFrameCount = downFrames == null ? 0 : downFrames.Length;
            if (downFrameCount < 1) return;
        }

        int activeFrameCount = GetCurrentFrameCount();
        if (activeFrameCount < 1) return;
        frameTimer += Time.deltaTime * animationFPS;
        while (frameTimer >= 1f)
        {
            frameTimer -= 1f;
            currentFrame = (currentFrame + 1) % activeFrameCount;
        }
    }

    // 朝前移动的动画，普通贴图会先播一次转身再进入走路循环
    private void AdvanceForwardAnimation()
    {
        if (usingAuthoredSheets)
        {
            AdvanceStandardAnimation();
            return;
        }

        if (!wasMovingForward)
            BeginForwardAnimation();

        if (forwardTurnPlaying)
            AdvanceForwardTurn();
        else
            AdvanceForwardWalk();
    }

    // 朝前走时先播一次转身动画
    private void BeginForwardAnimation()
    {
        frameTimer = 0f;
        if (!hasShownForwardTurn)
        {
            hasShownForwardTurn = true;
            forwardTurnPlaying = true;
            forwardTurnFrame = 0;
            currentFrame = 0;
            return;
        }

        forwardTurnPlaying = false;
        currentFrame = GetForwardWalkFirstFrame();
    }

    // 推进转身动画，播完接着走
    private void AdvanceForwardTurn()
    {
        int lastTurnFrame = Mathf.Min(3, Mathf.Max(0, frameCount - 1));
        frameTimer += Time.deltaTime * animationFPS;
        while (forwardTurnPlaying && frameTimer >= 1f)
        {
            frameTimer -= 1f;
            if (forwardTurnFrame < lastTurnFrame)
            {
                forwardTurnFrame++;
                currentFrame = forwardTurnFrame;
            }
            else
            {
                // The turn is a one-shot. Keep all later forward movement on rear walk frames.
                forwardTurnPlaying = false;
                currentFrame = GetForwardWalkFirstFrame();
                frameTimer = 0f;
            }
        }
    }

    // 朝前走路循环，最后一帧多停一会
    private void AdvanceForwardWalk()
    {
        int firstWalkFrame = GetForwardWalkFirstFrame();
        int lastWalkFrame = Mathf.Max(firstWalkFrame, Mathf.Max(0, frameCount - 1));
        if (currentFrame < firstWalkFrame || currentFrame > lastWalkFrame)
            currentFrame = firstWalkFrame;

        float tailHold = Mathf.Max(1f, forwardTailFrameHold);
        float frameDuration = currentFrame == lastWalkFrame ? tailHold : 1f;
        frameTimer += Time.deltaTime * animationFPS;
        while (frameTimer >= frameDuration)
        {
            frameTimer -= frameDuration;
            currentFrame = currentFrame >= lastWalkFrame ? firstWalkFrame : currentFrame + 1;
            frameDuration = currentFrame == lastWalkFrame ? tailHold : 1f;
        }
    }

    // 取朝前走动画的起始帧
    private int GetForwardWalkFirstFrame()
    {
        return Mathf.Min(4, Mathf.Max(0, frameCount - 1));
    }

    // 把当前帧画到渲染器上
    private void ApplyFrame()
    {
        if (spriteRenderer == null || frames == null || frames.Length <= facing) return;
        Sprite[] directionFrames = GetCurrentDirectionFrames();
        if (directionFrames == null || directionFrames.Length == 0) return;
        int index = Mathf.Clamp(currentFrame, 0, directionFrames.Length - 1);
        if (directionFrames[index] != null) spriteRenderer.sprite = directionFrames[index];
    }

    // 取当前朝向该用的帧数组，朝下时还要分左右
    private Sprite[] GetCurrentDirectionFrames()
    {
        if (facing == 0 && usingAuthoredSheets)
        {
            if (useLeftDownSheet && downLeftFrames != null && downLeftFrames.Length > 0)
                return downLeftFrames;
            if (downRightFrames != null && downRightFrames.Length > 0)
                return downRightFrames;
            return downLeftFrames;
        }

        return frames != null && frames.Length > facing ? frames[facing] : null;
    }

    // 取当前朝向有多少帧
    private int GetCurrentFrameCount()
    {
        Sprite[] directionFrames = GetCurrentDirectionFrames();
        return directionFrames == null ? 0 : directionFrames.Length;
    }
}
