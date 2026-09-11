using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

// 3D 卡牌预览的控制器，负责拖拽旋转、滚轮缩放、正反面切换和光影接入
public sealed class Card3DPreviewController : MonoBehaviour
{
    [Header("资源")]
    [SerializeField] private Camera previewCamera; // 预览摄像机
    [SerializeField] private TMP_FontAsset previewFont; // 预览卡牌字体

    [Header("卡牌光影")]
    [SerializeField] private CardHoloProfile holoProfile; // 卡牌光影分级配置

    [Header("Prefab引用")]
    [SerializeField] private GameObject frontCardVisual; // 正面视觉节点
    [SerializeField] private GameObject frontCardObject; // 正面Card对象
    [SerializeField] private GameObject backCardVisual; // 背面视觉节点
    [SerializeField] private SpriteRenderer backCardRenderer; // 背面卡图
    [SerializeField] private SpriteRenderer borderRenderer; // 正面卡框
    [SerializeField] private MeshRenderer cardBodyRenderer; // 卡牌主体渲染器

    [Header("操作")]
    [SerializeField] private float rotationSpeed = 0.35f; // 鼠标旋转速度
    [SerializeField] private float zoomSpeed = 0.8f; // 滚轮缩放速度
    [SerializeField] private float defaultObservationDistance = 6f; // 默认观察距离
    [SerializeField] private float minObservationDistance = 3.5f; // 最小观察距离
    [SerializeField] private float maxObservationDistance = 10f; // 最大观察距离
    [SerializeField] private bool horizontalRotationReversed; // 是否反向水平旋转
    [SerializeField] private bool verticalRotationReversed; // 是否反向垂直旋转
    [SerializeField] private float verticalRotationLimit = 80f; // 垂直旋转限制

    [Header("卡牌质感")]
    [SerializeField] private Color cardBodyColor = new Color(0.08f, 0.11f, 0.17f, 1f); // 卡牌主体颜色
    [SerializeField, Range(0f, 1f)] private float cardBodyMetallic = 0.35f; // 卡牌主体金属度
    [SerializeField, Range(0f, 1f)] private float cardBodySmoothness = 0.8f; // 卡牌主体光滑度

    private Vector3 lastMousePosition;
    private Vector3 rotationAngles;
    private Quaternion baseRotation;
    private float observationDistance;
    private bool isDragging;
    private bool showingFront = true; // 当前朝向镜头的是不是正面
    private Material cardBodyMaterial; // 运行时创建的卡牌主体材质
    private CardHoloVisual holoVisual; // 卡牌光影叠加控制器
    private CardDisplay previewDisplay; // 当前预览卡牌的显示组件

    // 缓存初始旋转、解析预制体引用并复位视角
    private void Awake()
    {
        baseRotation = transform.localRotation;
        ResolvePrefabReferences();
        ConfigureCardBodyMaterial();
        ResetView();
    }

    // 销毁运行时生成的卡身材质
    private void OnDestroy()
    {
        if (cardBodyMaterial != null) Destroy(cardBodyMaterial);
    }

    // 每帧处理输入、旋转和正反面显示
    private void Update()
    {
        HandleInput();
        ApplyRotation();
        UpdateHoloRotation();
        UpdateVisibleSide();
        UpdateCameraDistance();
    }

    // 编辑器里改参数时重新解析引用
    private void OnValidate()
    {
        if (Application.isPlaying) return;
        ResolvePrefabReferences();
    }

    /// <summary>
    /// 设置预览摄像机。
    /// </summary>
    public void SetPreviewCamera(Camera targetCamera)
    {
        previewCamera = targetCamera;
        UpdateCameraDistance();
    }

    /// <summary>
    /// 设置当前需要预览的卡牌。
    /// </summary>
    public void SetCard(CardData cardData)
    {
        ResolvePrefabReferences();
        if (cardData == null)
        {
            if (frontCardVisual != null) frontCardVisual.SetActive(false);
            if (backCardVisual != null) backCardVisual.SetActive(false);
            return;
        }

        frontCardVisual.SetActive(true);
        backCardVisual.SetActive(true);

        CardController cardController = frontCardObject.GetComponent<CardController>();
        CardDisplay cardDisplay = cardController != null ? cardController.cardDisplay : frontCardObject.GetComponentInChildren<CardDisplay>(true);
        if (cardController != null)
        {
            cardController.Init(cardData, null);
            cardController.enabled = false;
        }

        if (cardDisplay != null)
        {
            cardDisplay.UpdateDisplay();
            cardDisplay.ShowBack(false);
            ApplyCardDisplayData(cardDisplay, cardData);
        }

        ApplyPreviewFont();
        DisableGameplayComponents(frontCardObject);
        CardHoloTier holoTier = CardHoloTierMapper.FromRarity(cardData.rarity);
        int colorSeed = GetHoloColorSeed(cardData, holoTier);
        ConfigureHoloVisual(cardDisplay != null ? cardDisplay.cardImage : null, holoTier, colorSeed);
        previewDisplay = cardDisplay;
        RefreshTextTint();
        if (backCardRenderer != null) backCardRenderer.color = Color.white;
        UpdateVisibleSide();
    }

    /// <summary>
    /// 按当前档位的字体变色强度刷新名称与效果文字颜色。
    /// </summary>
    public void RefreshTextTint()
    {
        if (previewDisplay == null || holoVisual == null) return;
        Color tint = Color.Lerp(Color.white, holoVisual.TextTint, holoProfile.GetSettings(holoVisual.Tier).textIntensity);
        tint.a = 1f;
        if (previewDisplay.nameText != null) previewDisplay.nameText.color = tint;
        if (previewDisplay.effectText != null) previewDisplay.effectText.color = tint;
    }

    /// <summary>
    /// 当前卡牌光影分级配置。
    /// </summary>
    public CardHoloProfile HoloProfile => holoProfile;

    /// <summary>
    /// 当前卡牌光影叠加控制器。
    /// </summary>
    public CardHoloVisual HoloVisual => holoVisual;

    /// <summary>
    /// 重置卡牌的旋转和观察距离。
    /// </summary>
    public void ResetView()
    {
        rotationAngles = Vector3.zero;
        observationDistance = Mathf.Clamp(defaultObservationDistance, minObservationDistance, maxObservationDistance);
        ApplyRotation();
        UpdateHoloRotation();
        UpdateCameraDistance();
        UpdateVisibleSide();
    }

    // 从子节点里找回正反面和卡框的渲染器引用
    private void ResolvePrefabReferences()
    {
        if (frontCardVisual == null) frontCardVisual = transform.Find("FrontCardVisual")?.gameObject;
        if (backCardVisual == null) backCardVisual = transform.Find("BackCardVisual")?.gameObject;
        if (frontCardObject == null && frontCardVisual != null)
        {
            CardController cardController = frontCardVisual.GetComponentInChildren<CardController>(true);
            if (cardController != null) frontCardObject = cardController.gameObject;
        }

        if (backCardRenderer == null && backCardVisual != null)
            backCardRenderer = backCardVisual.GetComponentInChildren<SpriteRenderer>(true);

        if (borderRenderer == null && frontCardVisual != null)
        {
            foreach (SpriteRenderer renderer in frontCardVisual.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (renderer.gameObject.name == "Outline")
                {
                    borderRenderer = renderer;
                    break;
                }
            }
        }

        if (cardBodyRenderer == null)
        {
            Transform cardBody = transform.Find("CardBody");
            if (cardBody != null) cardBodyRenderer = cardBody.GetComponent<MeshRenderer>();
        }
    }

    // 给卡身换一个可调色和光滑度的运行时材质
    private void ConfigureCardBodyMaterial()
    {
        if (cardBodyRenderer == null) return;
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        if (shader == null) return;

        cardBodyMaterial = new Material(shader);
        cardBodyMaterial.name = "Card3DPreviewBodyRuntimeMaterial";
        if (cardBodyMaterial.HasProperty("_BaseColor")) cardBodyMaterial.SetColor("_BaseColor", cardBodyColor);
        if (cardBodyMaterial.HasProperty("_Color")) cardBodyMaterial.SetColor("_Color", cardBodyColor);
        if (cardBodyMaterial.HasProperty("_Metallic")) cardBodyMaterial.SetFloat("_Metallic", cardBodyMetallic);
        if (cardBodyMaterial.HasProperty("_Smoothness")) cardBodyMaterial.SetFloat("_Smoothness", cardBodySmoothness);
        cardBodyRenderer.material = cardBodyMaterial;
    }

    // 把预览字体套到卡面所有文字上
    private void ApplyPreviewFont()
    {
        if (previewFont == null || frontCardObject == null) return;
        foreach (TMP_Text text in frontCardObject.GetComponentsInChildren<TMP_Text>(true))
        {
            text.font = previewFont;
            text.raycastTarget = false;
        }
    }

    // 把卡牌数据填进卡面显示
    private static void ApplyCardDisplayData(CardDisplay cardDisplay, CardData cardData)
    {
        if (cardDisplay.nameText != null) cardDisplay.nameText.text = cardData.name;
        if (cardDisplay.costText != null) cardDisplay.costText.text = cardData.cost.ToString();
        if (cardDisplay.cardImage != null)
        {
            cardDisplay.cardImage.sprite = cardData.image;
            cardDisplay.cardImage.maskInteraction = SpriteMaskInteraction.None;
        }
        if (cardDisplay.effectText != null)
        {
            cardDisplay.effectText.alignment = TextAlignmentOptions.Center;
            cardDisplay.effectText.enableWordWrapping = true;
            cardDisplay.effectText.text = Regex.Unescape(cardData.effectDescription);
        }
        if (cardDisplay.atkText != null) cardDisplay.atkText.text = cardData.attack.ToString();
        if (cardDisplay.hpText != null) cardDisplay.hpText.text = cardData.health.ToString();
        if (cardDisplay.mumberCardLayout != null) cardDisplay.mumberCardLayout.SetActive(cardData.cardType == CardType.MUMBER);
        if (cardDisplay.atkText != null) cardDisplay.atkText.gameObject.SetActive(cardData.cardType == CardType.MUMBER);
        if (cardDisplay.hpText != null) cardDisplay.hpText.gameObject.SetActive(cardData.cardType == CardType.MUMBER);
        if (cardDisplay.front != null) cardDisplay.front.SetActive(true);
        if (cardDisplay.back != null) cardDisplay.back.SetActive(false);
    }

    // 关掉卡面上的碰撞和战斗逻辑，只留显示
    private static void DisableGameplayComponents(GameObject cardObject)
    {
        foreach (Collider collider in cardObject.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
        foreach (Collider2D collider in cardObject.GetComponentsInChildren<Collider2D>(true)) collider.enabled = false;
        foreach (CardDisplay display in cardObject.GetComponentsInChildren<CardDisplay>(true)) display.enabled = false;
        CardController controller = cardObject.GetComponent<CardController>();
        if (controller != null) controller.enabled = false;
    }

    /// <summary>
    /// 将当前卡牌图层接入移植后的光影叠加效果。
    /// </summary>
    private void ConfigureHoloVisual(SpriteRenderer cardRenderer, CardHoloTier tier, int colorSeed)
    {
        if (cardRenderer == null)
        {
            return;
        }

        if (holoVisual == null)
        {
            holoVisual = GetComponent<CardHoloVisual>();
            if (holoVisual == null)
            {
                holoVisual = gameObject.AddComponent<CardHoloVisual>();
            }
        }

        holoVisual.Configure(cardRenderer, borderRenderer, holoProfile, tier, colorSeed);
    }

    // 取 UR 卡的色相种子，没记录过就用卡牌 id 兜底
    private static int GetHoloColorSeed(CardData cardData, CardHoloTier tier)
    {
        if (tier != CardHoloTier.UR) return 0;
        if (CampaignSession.Instance.TryGetCardHoloVariantSeed(cardData.index, out int savedSeed)) return savedSeed;
        return cardData.index;
    }

    // 把旋转角度压成 -1~1 的方向值，供光影跟随视角变化
    private void UpdateHoloRotation()
    {
        if (holoVisual == null)
        {
            return;
        }

        float frontYaw = Mathf.DeltaAngle(0f, rotationAngles.y);
        var normalizedRotation = new Vector2(
            Mathf.Clamp(rotationAngles.x / 55f, -1f, 1f),
            Mathf.Clamp(frontYaw / 70f, -1f, 1f));
        holoVisual.SetRotation(normalizedRotation, isDragging);
    }

    // 处理拖拽旋转和滚轮缩放
    private void HandleInput()
    {
        bool pointerOverUi = IsPointerOverUi();
        if (Input.GetMouseButtonDown(0) && !pointerOverUi)
        {
            isDragging = true;
            lastMousePosition = Input.mousePosition;
        }

        if (!Input.GetMouseButton(0)) isDragging = false;
        if (isDragging && !pointerOverUi)
        {
            Vector3 mouseDelta = Input.mousePosition - lastMousePosition;
            float horizontalDirection = horizontalRotationReversed ? -1f : 1f;
            float verticalDirection = verticalRotationReversed ? -1f : 1f;
            if (!showingFront) verticalDirection = -verticalDirection; // 背面朝前时上下会反过来，这里补回来
            rotationAngles.y += mouseDelta.x * rotationSpeed * horizontalDirection;
            rotationAngles.x += mouseDelta.y * rotationSpeed * verticalDirection;
            rotationAngles.x = Mathf.Clamp(rotationAngles.x, -verticalRotationLimit, verticalRotationLimit);
            lastMousePosition = Input.mousePosition;
        }

        if (!pointerOverUi && Mathf.Abs(Input.mouseScrollDelta.y) > 0.001f)
            observationDistance = Mathf.Clamp(observationDistance - Input.mouseScrollDelta.y * zoomSpeed, minObservationDistance, maxObservationDistance);
    }

    // 指针是不是停在 UI 上
    private bool IsPointerOverUi()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }

    // 把当前旋转角度写到 Transform 上
    private void ApplyRotation()
    {
        transform.localRotation = baseRotation * Quaternion.Euler(rotationAngles.x, rotationAngles.y, 0f);
    }

    // 按视角朝向切换显示正面还是背面
    private void UpdateVisibleSide()
    {
        if (previewCamera == null || frontCardObject == null || backCardVisual == null) return;
        Vector3 cameraDirection = (previewCamera.transform.position - transform.position).normalized;
        Vector3 frontNormal = -transform.forward;
        bool showFront = Vector3.Dot(frontNormal, cameraDirection) >= 0f;
        showingFront = showFront;
        frontCardVisual.SetActive(showFront);
        backCardVisual.SetActive(!showFront);
    }

    // 让摄像机保持在设定的观察距离上
    private void UpdateCameraDistance()
    {
        if (previewCamera == null) return;
        previewCamera.transform.position = transform.position - previewCamera.transform.forward * observationDistance;
    }
}
