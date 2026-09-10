using System;
using UnityEngine;

[CreateAssetMenu(fileName = "CardHoloProfile", menuName = "ArkCard/Card Holo Profile")]
public sealed class CardHoloProfile : ScriptableObject
{
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
            Color rainbow)
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
        }
    }

    [Header("R 光影参数")]
    [SerializeField] private TierSettings r = CreateRDefault(); // R 光影配置

    [Header("SR 光影参数")]
    [SerializeField] private TierSettings sr = CreateSrDefault(); // SR 光影配置

    [Header("UR 光影参数")]
    [SerializeField] private TierSettings ur = CreateUrDefault(); // UR 光影配置

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

    public static TierSettings CreateRDefault()
    {
        return new TierSettings(
            0.58f,
            0.2f,
            0.08f,
            0f,
            24f,
            0.22f,
            0.8f,
            2.6f,
            0.24f,
            new Color(0.97f, 0.99f, 1f, 0.95f),
            new Color(0.78f, 0.92f, 1f, 0.28f));
    }

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
            new Color(0.68f, 0.72f, 1f, 0.62f));
    }

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
            new Color(0.86f, 0.97f, 1f, 0.9f));
    }
}
