using UnityEngine;

public sealed class CardHoloVisual : MonoBehaviour
{
    private static readonly int MainTexId = Shader.PropertyToID("_MainTex"); // 卡图纹理属性
    private static readonly int PearlTintId = Shader.PropertyToID("_PearlTint"); // 珍珠色属性
    private static readonly int RainbowTintId = Shader.PropertyToID("_RainbowTint"); // 虹彩色属性
    private static readonly int FrameIntensityId = Shader.PropertyToID("_FrameIntensity"); // 卡框强度属性
    private static readonly int SurfaceIntensityId = Shader.PropertyToID("_SurfaceIntensity"); // 卡面强度属性
    private static readonly int ArtIntensityId = Shader.PropertyToID("_ArtIntensity"); // 卡图强度属性
    private static readonly int TextureStrengthId = Shader.PropertyToID("_TextureStrength"); // 纹理强度属性
    private static readonly int TextureScaleId = Shader.PropertyToID("_TextureScale"); // 纹理密度属性
    private static readonly int BandWidthId = Shader.PropertyToID("_BandWidth"); // 光带宽度属性
    private static readonly int SpectrumFrequencyId = Shader.PropertyToID("_SpectrumFrequency"); // 彩谱频率属性
    private static readonly int ReflectionSharpnessId = Shader.PropertyToID("_ReflectionSharpness"); // 反光锐度属性
    private static readonly int IdleStrengthId = Shader.PropertyToID("_IdleStrength"); // 静止强度属性
    private static readonly int EffectModeId = Shader.PropertyToID("_EffectMode"); // 光影模式属性
    private static readonly int RotationId = Shader.PropertyToID("_Rotation"); // 旋转参数属性
    private static readonly int LayerModeId = Shader.PropertyToID("_LayerMode"); // 光影图层属性
    private static readonly int HueOffsetId = Shader.PropertyToID("_HueOffset"); // 虹彩色相偏移属性

    private SpriteRenderer cardSourceRenderer; // 原始卡图渲染器
    private SpriteRenderer borderSourceRenderer; // 原始边框渲染器
    private SpriteRenderer cardOverlayRenderer; // 卡图光影叠加渲染器
    private SpriteRenderer borderOverlayRenderer; // 边框光影叠加渲染器
    private GameObject cardOverlayObject; // 卡图光影叠加对象
    private GameObject borderOverlayObject; // 边框光影叠加对象
    private Material cardRuntimeMaterial; // 卡图运行时光影材质
    private Material borderRuntimeMaterial; // 边框运行时光影材质
    private CardHoloProfile holoProfile; // 光影分级配置
    private CardHoloTier holoTier; // 当前光影等级
    private int colorVariantSeed; // 当前卡牌光影颜色种子

    /// <summary>
    /// 初始化卡牌光影叠加层。
    /// </summary>
    public void Configure(SpriteRenderer cardRenderer, SpriteRenderer borderRenderer, CardHoloProfile profile, CardHoloTier tier, int variantSeed = 0)
    {
        cardSourceRenderer = cardRenderer;
        borderSourceRenderer = borderRenderer;
        holoProfile = profile;
        holoTier = tier;
        colorVariantSeed = variantSeed;

        EnsureOverlay(cardSourceRenderer, ref cardOverlayObject, ref cardOverlayRenderer, ref cardRuntimeMaterial, "Card Holo Art Overlay", 0f);
        EnsureOverlay(borderSourceRenderer, ref borderOverlayObject, ref borderOverlayRenderer, ref borderRuntimeMaterial, "Card Holo Border Overlay", 1f);
        ApplyProfile();
        SetRotation(Vector2.zero, false);
    }

    /// <summary>
    /// 设置当前卡牌的光影颜色变体。
    /// </summary>
    public void SetColorVariant(int variantSeed)
    {
        colorVariantSeed = variantSeed;
        ApplyProfile();
    }

    /// <summary>
    /// 切换当前卡牌的光影等级。
    /// </summary>
    public void SetTier(CardHoloTier tier)
    {
        holoTier = tier;
        ApplyProfile();
    }

    /// <summary>
    /// 设置光影跟随卡牌旋转的方向。
    /// </summary>
    public void SetRotation(Vector2 normalizedRotation, bool active)
    {
        SetRotation(cardRuntimeMaterial, normalizedRotation, active);
        SetRotation(borderRuntimeMaterial, normalizedRotation, active);
    }

    private void EnsureOverlay(SpriteRenderer sourceRenderer, ref GameObject overlayObject, ref SpriteRenderer overlayRenderer, ref Material runtimeMaterial, string objectName, float layerMode)
    {
        if (sourceRenderer == null || overlayRenderer != null) return;

        overlayObject = new GameObject(objectName);
        overlayObject.transform.SetParent(sourceRenderer.transform, false);
        overlayRenderer = overlayObject.AddComponent<SpriteRenderer>();

        var holoShader = Shader.Find("New Gamer Card/Card Pearlescent Surface");
        runtimeMaterial = new Material(holoShader)
        {
            name = $"{objectName} Runtime ({gameObject.name})",
            renderQueue = 3000
        };
        runtimeMaterial.SetFloat(LayerModeId, layerMode);
        overlayRenderer.material = runtimeMaterial;
    }

    private void SyncOverlay(SpriteRenderer sourceRenderer, SpriteRenderer overlayRenderer, Material runtimeMaterial)
    {
        if (sourceRenderer == null || overlayRenderer == null) return;

        overlayRenderer.sprite = sourceRenderer.sprite;
        if (sourceRenderer.sprite != null) runtimeMaterial.SetTexture(MainTexId, sourceRenderer.sprite.texture);
        overlayRenderer.drawMode = sourceRenderer.drawMode;
        overlayRenderer.size = sourceRenderer.size;
        overlayRenderer.flipX = sourceRenderer.flipX;
        overlayRenderer.flipY = sourceRenderer.flipY;
        overlayRenderer.maskInteraction = sourceRenderer.maskInteraction;
        overlayRenderer.sortingLayerID = sourceRenderer.sortingLayerID;
        overlayRenderer.sortingOrder = sourceRenderer.sortingOrder + 1;
        overlayRenderer.color = Color.white;
        overlayRenderer.enabled = sourceRenderer.enabled && holoTier != CardHoloTier.None;
    }

    private void ApplyProfile()
    {
        var settings = holoProfile != null
            ? holoProfile.GetSettings(holoTier)
            : GetDefaultSettings(holoTier);

        ApplyProfile(cardRuntimeMaterial, settings, 0f);
        ApplyProfile(borderRuntimeMaterial, settings, 1f);
        SyncOverlay(cardSourceRenderer, cardOverlayRenderer, cardRuntimeMaterial);
        SyncOverlay(borderSourceRenderer, borderOverlayRenderer, borderRuntimeMaterial);
    }

    private void ApplyProfile(Material runtimeMaterial, CardHoloProfile.TierSettings settings, float layerMode)
    {
        if (runtimeMaterial == null) return;
        runtimeMaterial.SetFloat(FrameIntensityId, settings.frameIntensity);
        runtimeMaterial.SetFloat(SurfaceIntensityId, settings.surfaceIntensity);
        runtimeMaterial.SetFloat(ArtIntensityId, settings.artIntensity);
        runtimeMaterial.SetFloat(TextureStrengthId, settings.textureStrength);
        runtimeMaterial.SetFloat(TextureScaleId, settings.textureScale);
        runtimeMaterial.SetFloat(BandWidthId, settings.bandWidth);
        runtimeMaterial.SetFloat(SpectrumFrequencyId, settings.spectrumFrequency);
        runtimeMaterial.SetFloat(ReflectionSharpnessId, settings.reflectionSharpness);
        runtimeMaterial.SetFloat(IdleStrengthId, settings.idleStrength);
        runtimeMaterial.SetColor(PearlTintId, settings.pearlTint);
        runtimeMaterial.SetFloat(LayerModeId, layerMode);
        runtimeMaterial.SetFloat(EffectModeId, Mathf.Max(0f, (float)holoTier - 1f));
        ApplyColorVariant(runtimeMaterial, settings);
    }

    private void ApplyColorVariant(Material runtimeMaterial, CardHoloProfile.TierSettings settings)
    {
        var hueOffset = 0f;
        var rainbowTint = settings.rainbowTint;
        if (holoTier == CardHoloTier.UR && colorVariantSeed != 0)
        {
            hueOffset = GetVariantHue(colorVariantSeed);
            var variantColor = Color.HSVToRGB(hueOffset, 0.62f, 1f);
            variantColor.a = settings.rainbowTint.a;
            rainbowTint = Color.Lerp(settings.rainbowTint, variantColor, 0.82f);
        }

        runtimeMaterial.SetFloat(HueOffsetId, hueOffset);
        runtimeMaterial.SetColor(RainbowTintId, rainbowTint);
    }

    private static void SetRotation(Material runtimeMaterial, Vector2 normalizedRotation, bool active)
    {
        if (runtimeMaterial == null) return;
        runtimeMaterial.SetVector(
            RotationId,
            new Vector4(
                Mathf.Clamp(normalizedRotation.x, -1f, 1f),
                Mathf.Clamp(normalizedRotation.y, -1f, 1f),
                active ? 1f : 0f,
                0f));
    }

    private static float GetVariantHue(int seed)
    {
        var hash = seed;
        hash = (hash ^ 61) ^ (hash >> 16);
        hash *= 9;
        hash ^= hash >> 4;
        hash *= 0x27d4eb2d;
        hash ^= hash >> 15;
        return (hash & int.MaxValue) / (float)int.MaxValue;
    }

    private static CardHoloProfile.TierSettings GetDefaultSettings(CardHoloTier tier)
    {
        return tier switch
        {
            CardHoloTier.R => CardHoloProfile.CreateRDefault(),
            CardHoloTier.SR => CardHoloProfile.CreateSrDefault(),
            CardHoloTier.UR => CardHoloProfile.CreateUrDefault(),
            _ => default
        };
    }

    private void OnDestroy()
    {
        if (cardRuntimeMaterial != null) Destroy(cardRuntimeMaterial);
        if (borderRuntimeMaterial != null) Destroy(borderRuntimeMaterial);
        if (cardOverlayObject != null) Destroy(cardOverlayObject);
        if (borderOverlayObject != null) Destroy(borderOverlayObject);
    }
}
