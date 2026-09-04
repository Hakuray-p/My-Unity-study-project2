using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controllable pixel-art avatar used by the HD city exploration scene.
/// The runtime supplies the camera and can pause movement while the route map is open.
/// </summary>
public sealed class AmiyaCharacter : MonoBehaviour
{
    [Min(0.1f)] public float moveSpeed = 4f;
    [Min(1f)] public float animationFPS = 8f;
    public string resourceBase = "Ark Image/Amiya/amiya";
    public string fallbackResource = "Ark Image/阿米娅";
    [Min(1)] public int frameCount = 8;
    [Min(1f)] public float pixelsPerUnit = 100f;
    [Min(0.01f)] public float sizeScale = 1f;
    [Min(1f)] public float forwardTailFrameHold = 2.5f;
    [Header("Authored sprite sheets")]
    public Texture2D downRightSheet;
    public Texture2D downLeftSheet;
    public Texture2D leftSheet;
    public Texture2D rightSheet;
    public Texture2D backSheet;
    [Min(1f)] public float sheetPixelsPerUnit = 160f;
    [Min(1f)] public float backSheetPixelsPerUnit = 395f;
    [Header("Authored frame sizing")]
    [Tooltip("World-space height shared by the visible pixels of every authored frame.")]
    [Min(0.1f)] public float authoredCharacterHeight = 1.95f;
    public bool normalizeAuthoredFrameSize = true;
    public Camera gameplayCamera;
    [Header("Grounding")]
    public LayerMask groundMask = ~0;
    [Min(0.1f)] public float groundProbeDistance = 3f;
    [Min(0f)] public float groundOffset = 0.04f;
    public float minimumSafeY = -5f;

    private SpriteRenderer spriteRenderer;
    private CharacterController characterController;
    private Sprite[][] frames;
    private Sprite[] downRightFrames;
    private Sprite[] downLeftFrames;
    private bool usingAuthoredSheets;
    private bool useLeftDownSheet;
    private int facing;
    private int currentFrame;
    private float frameTimer;
    private bool hasShownForwardTurn;
    private bool forwardTurnPlaying;
    private bool wasMovingForward;
    private int forwardTurnFrame;
    private Vector3 lastSafePosition;
    private bool hasSafePosition;
    private readonly RaycastHit[] groundHits = new RaycastHit[16];

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null) spriteRenderer = gameObject.AddComponent<SpriteRenderer>();

        characterController = GetComponent<CharacterController>();
        if (characterController == null) characterController = gameObject.AddComponent<CharacterController>();

        spriteRenderer.sortingOrder = 50;
        transform.localScale = Vector3.one * sizeScale;
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

    private void Update()
    {
        if (spriteRenderer == null) return;

        if (IsInvalidPosition())
        {
            RecoverToSafePosition();
            return;
        }

        if (gameplayCamera == null) gameplayCamera = HDAdventureRuntime.GameplayCamera;
        FaceCamera();

        if (HDAdventureRuntime.IsMapOpen)
        {
            currentFrame = 0;
            frameTimer = 0f;
            forwardTurnPlaying = false;
            wasMovingForward = false;
            ApplyFrame();
            SnapToGround();
            return;
        }

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

    private void FaceCamera()
    {
        if (gameplayCamera == null) return;
        Vector3 towardCamera = Vector3.ProjectOnPlane(-gameplayCamera.transform.forward, Vector3.up);
        if (towardCamera.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(towardCamera.normalized, Vector3.up);
    }

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

    private void SetGroundHeight(float groundY)
    {
        Vector3 position = transform.position;
        position.y = groundY + groundOffset;
        transform.position = position;
        lastSafePosition = position;
        hasSafePosition = true;
    }

    private void MaintainGround()
    {
        if (TryGetGround(out RaycastHit hit))
        {
            SetGroundHeight(hit.point.y);
            return;
        }

        if (transform.position.y < minimumSafeY) RecoverToSafePosition();
    }

    private bool IsInvalidPosition()
    {
        return float.IsNaN(transform.position.x) || float.IsNaN(transform.position.y) ||
               float.IsNaN(transform.position.z) || float.IsInfinity(transform.position.x) ||
               float.IsInfinity(transform.position.y) || float.IsInfinity(transform.position.z) ||
               transform.position.y < minimumSafeY;
    }

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

    private Sprite[] CreateSheetFrames(Texture2D sheet, int columns, int rows, float sheetPpu, int frameLimit = -1)
    {
        if (sheet == null || columns < 1 || rows < 1) return null;

        int cellWidth = sheet.width / columns;
        int cellHeight = sheet.height / rows;
        if (cellWidth < 1 || cellHeight < 1) return null;

        int capacity = frameLimit > 0 ? Mathf.Min(frameLimit, columns * rows) : columns * rows;
        List<Sprite> result = new List<Sprite>(capacity);
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

                Rect rect = new Rect(column * cellWidth, textureRow * cellHeight, cellWidth, cellHeight);
                float framePpu = sheetPpu;
                Vector2 pivot = new Vector2(0.5f, 0.08f);
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

    private int GetForwardWalkFirstFrame()
    {
        return Mathf.Min(4, Mathf.Max(0, frameCount - 1));
    }

    private void ApplyFrame()
    {
        if (spriteRenderer == null || frames == null || frames.Length <= facing) return;
        Sprite[] directionFrames = GetCurrentDirectionFrames();
        if (directionFrames == null || directionFrames.Length == 0) return;
        int index = Mathf.Clamp(currentFrame, 0, directionFrames.Length - 1);
        if (directionFrames[index] != null) spriteRenderer.sprite = directionFrames[index];
    }

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

    private int GetCurrentFrameCount()
    {
        Sprite[] directionFrames = GetCurrentDirectionFrames();
        return directionFrames == null ? 0 : directionFrames.Length;
    }
}
