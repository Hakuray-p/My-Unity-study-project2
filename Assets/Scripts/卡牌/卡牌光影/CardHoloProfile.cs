using System;
using UnityEngine;

// 卡牌光影配置：一个资源文件里存放 R / SR / UR 三档参数，由 CardHoloVisual 和调参面板共用。
[CreateAssetMenu(fileName = "CardHoloProfile", menuName = "ArkCard/Card Holo Profile")]
public sealed class CardHoloProfile : ScriptableObject
{
    // 单档的光影参数
    [Serializable]
    public struct TierSettings
    {
        [Range(0f, 1.5f)] public float frameIntensity; // 卡框反光强度
        [Range(0f, 1.5f)] public float surfaceIntensity; // 卡面纹理强度
        [Range(0f, 0.75f)] public float artIntensity; // 卡图反光强度
        [Range(0f, 1f)] public float textureStrength; // 纹理强度
        [Range(4f, 80f)] public float textureScale; // 纹理密度
        [Range(0.05f, 0.65f)] public float bandWidth; // 光带宽度
        [Range(0.25f, 3f)] public float spectrumFrequency; // 彩谱频率
        [Range(0.5f, 6f)] public float reflectionSharpness; // 反光锐度
        [Range(0f, 0.75f)] public float idleStrength; // 静止时的光影强度
        public Color pearlTint; // 珍珠色
        public Color rainbowTint; // 虹彩色
        [Range(0f, 1f)] public float textIntensity; // 名称与效果文字变色强度
        public Color textTint; // 名称与效果文字颜色

        // 按顺序接收一档的全部参数
        public TierSettings(
            float frame,
            float surface,
            float artwork,
            float texture,
            float textureTiling,
            float width,
            float frequency,
            float sharpness,
            float idle,
            Color pearl,
            Color rainbow,
            float text,
            Color textColor)
        {
            frameIntensity = frame;
            surfaceIntensity = surface;
            artIntensity = artwork;
            textureStrength = texture;
            textureScale = textureTiling;
            bandWidth = width;
            spectrumFrequency = frequency;
            reflectionSharpness = sharpness;
            idleStrength = idle;
            pearlTint = pearl;
            rainbowTint = rainbow;
            textIntensity = text;
            textTint = textColor;
        }
    }

    [Header("R 光影参数")]
    [SerializeField] private TierSettings r = CreateRDefault(); // R 光影配置

    [Header("SR 光影参数")]
    [SerializeField] private TierSettings sr = CreateSrDefault(); // SR 光影配置

    [Header("UR 光影参数")]
    [SerializeField] private TierSettings ur = CreateUrDefault(); // UR 光影配置

    // 按档位取参数，未知档位时返回默认值
    public TierSettings GetSettings(CardHoloTier tier)
    {
        return tier switch
        {
            CardHoloTier.R => r,
            CardHoloTier.SR => sr,
            CardHoloTier.UR => ur,
            _ => default
        };
    }

    /// <summary>
    /// 写回指定档位的光影参数。
    /// </summary>
    // 只改内存里的资源，落盘由调参面板负责
    public void SetSettings(CardHoloTier tier, TierSettings settings)
    {
        switch (tier)
        {
            case CardHoloTier.R:
                r = settings;
                break;
            case CardHoloTier.SR:
                sr = settings;
                break;
            case CardHoloTier.UR:
                ur = settings;
                break;
        }
    }

    // R 档与 SR 同档，直接复用 SR 默认值
    public static TierSettings CreateRDefault()
    {
        return CreateSrDefault();
    }

    // SR 默认值，同时作为 R 档的默认值
    public static TierSettings CreateSrDefault()
    {
        return new TierSettings(
            0.78f,
            0.48f,
            0.28f,
            0.62f,
            30f,
            0.3f,
            1.15f,
            2f,
            0.3f,
            new Color(0.97f, 0.99f, 1f, 0.96f),
            new Color(0.68f, 0.72f, 1f, 0.62f),
            0f,
            Color.white);
    }

    // UR 默认值：彩谱更亮、光带更宽，文字用金色
    public static TierSettings CreateUrDefault()
    {
        return new TierSettings(
            1.08f,
            0.92f,
            0.58f,
            0.82f,
            22f,
            0.4f,
            1f,
            1.35f,
            0.48f,
            new Color(0.98f, 0.99f, 1f, 0.98f),
            new Color(0.86f, 0.97f, 1f, 0.9f),
            1f,
            new Color(1f, 0.84f, 0.35f, 1f));
    }
}