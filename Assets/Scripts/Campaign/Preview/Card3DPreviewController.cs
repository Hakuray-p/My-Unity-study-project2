using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

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
    private Material cardBodyMaterial;
    private CardHoloVisual holoVisual;
    private CardDisplay previewDisplay; // 当前预览卡牌的显示组件

    private void Awake()
    {
        baseRotation = transform.localRotation;
        ResolvePrefabReferences();
        ConfigureCardBodyMaterial();
        ResetView();
    }

    private void OnDestroy()
    {
        if (cardBodyMaterial != null) Destroy(cardBodyMaterial);
    }

    private void Update()
    {
        HandleInput();
        ApplyRotation();
        UpdateHoloRotation();
        UpdateVisibleSide();
        UpdateCameraDistance();
    }

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

        var cardController = frontCardObject.GetComponent<CardController>();
        var cardDisplay = cardController != null ? cardController.cardDisplay : frontCardObject.GetComponentInChildren<CardDisplay>(true);
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
        var holoTier = CardHoloTierMapper.FromRarity(cardData.rarity);
        var colorSeed = GetHoloColorSeed(cardData, holoTier);
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
        var tint = Color.Lerp(Color.white, holoVisual.TextTint, holoProfile.GetSettings(holoVisual.Tier).textIntensity);
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

    private void ResolvePrefabReferences()
    {
        if (frontCardVisual == null) frontCardVisual = transform.Find("FrontCardVisual")?.gameObject;
        if (backCardVisual == null) backCardVisual = transform.Find("BackCardVisual")?.gameObject;
        if (frontCardObject == null && frontCardVisual != null)
        {
            var cardController = frontCardVisual.GetComponentInChildren<CardController>(true);
            if (cardController != null) frontCardObject = cardController.gameObject;
        }

        if (backCardRenderer == null && backCardVisual != null)
            backCardRenderer = backCardVisual.GetComponentInChildren<SpriteRenderer>(true);

        if (borderRenderer == null && frontCardVisual != null)
        {
            foreach (var renderer in frontCardVisual.GetComponentsInChildren<SpriteRenderer>(true))
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
            var cardBody = transform.Find("CardBody");
            if (cardBody != null) cardBodyRenderer = cardBody.GetComponent<MeshRenderer>();
        }
    }

    private void ConfigureCardBodyMaterial()
    {
        if (cardBodyRenderer == null) return;
        var shader = Shader.Find("Universal Render Pipeline/Lit");
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

    private void ApplyPreviewFont()
    {
        if (previewFont == null || frontCardObject == null) return;
        foreach (var text in frontCardObject.GetComponentsInChildren<TMP_Text>(true))
        {
            text.font = previewFont;
            text.raycastTarget = false;
        }
    }

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

    private static void DisableGameplayComponents(GameObject cardObject)
    {
        foreach (var collider in cardObject.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
        foreach (var collider in cardObject.GetComponentsInChildren<Collider2D>(true)) collider.enabled = false;
        foreach (var display in cardObject.GetComponentsInChildren<CardDisplay>(true)) display.enabled = false;
        var controller = cardObject.GetComponent<CardController>();
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

    private static int GetHoloColorSeed(CardData cardData, CardHoloTier tier)
    {
        if (tier != CardHoloTier.UR) return 0;
        if (CampaignSession.Instance.TryGetCardHoloVariantSeed(cardData.index, out var savedSeed)) return savedSeed;
        return cardData.index;
    }

    private void UpdateHoloRotation()
    {
        if (holoVisual == null)
        {
            return;
        }

        var frontYaw = Mathf.DeltaAngle(0f, rotationAngles.y);
        var normalizedRotation = new Vector2(
            Mathf.Clamp(rotationAngles.x / 55f, -1f, 1f),
            Mathf.Clamp(frontYaw / 70f, -1f, 1f));
        holoVisual.SetRotation(normalizedRotation, isDragging);
    }

    private void HandleInput()
    {
        var pointerOverUi = IsPointerOverUi();
        if (Input.GetMouseButtonDown(0) && !pointerOverUi)
        {
            isDragging = true;
            lastMousePosition = Input.mousePosition;
        }

        if (!Input.GetMouseButton(0)) isDragging = false;
        if (isDragging && !pointerOverUi)
        {
            var mouseDelta = Input.mousePosition - lastMousePosition;
            var horizontalDirection = horizontalRotationReversed ? -1f : 1f;
            var verticalDirection = verticalRotationReversed ? -1f : 1f;
            rotationAngles.y += mouseDelta.x * rotationSpeed * horizontalDirection;
            rotationAngles.x += mouseDelta.y * rotationSpeed * verticalDirection;
            rotationAngles.x = Mathf.Clamp(rotationAngles.x, -verticalRotationLimit, verticalRotationLimit);
            lastMousePosition = Input.mousePosition;
        }

        if (!pointerOverUi && Mathf.Abs(Input.mouseScrollDelta.y) > 0.001f)
            observationDistance = Mathf.Clamp(observationDistance - Input.mouseScrollDelta.y * zoomSpeed, minObservationDistance, maxObservationDistance);
    }

    private bool IsPointerOverUi()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }

    private void ApplyRotation()
    {
        transform.localRotation = baseRotation * Quaternion.Euler(rotationAngles.x, rotationAngles.y, 0f);
    }

    private void UpdateVisibleSide()
    {
        if (previewCamera == null || frontCardObject == null || backCardVisual == null) return;
        var cameraDirection = (previewCamera.transform.position - transform.position).normalized;
        var frontNormal = -transform.forward;
        var showFront = Vector3.Dot(frontNormal, cameraDirection) >= 0f;
        frontCardVisual.SetActive(showFront);
        backCardVisual.SetActive(!showFront);
    }

    private void UpdateCameraDistance()
    {
        if (previewCamera == null) return;
        previewCamera.transform.position = transform.position - previewCamera.transform.forward * observationDistance;
    }
}
