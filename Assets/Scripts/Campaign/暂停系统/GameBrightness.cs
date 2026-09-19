using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// 调整独立后处理中的曝光，不修改场景灯光和共享材质。
public sealed class GameBrightness : MonoBehaviour
{
    [SerializeField] private Volume volume; // 场景预置的亮度后处理
    private VolumeProfile runtimeProfile; // 当前场景独立使用的配置
    private ColorAdjustments colorAdjustments; // 控制曝光的颜色设置

    // 取得运行时配置，避免更改原始资源。
    private void Awake()
    {
        runtimeProfile = volume.profile;
        runtimeProfile.TryGet(out colorAdjustments);
    }

    // 恢复保存的亮度，并接收设置页面的调整。
    private void OnEnable()
    {
        GameSettings.BrightnessChanged += ApplyBrightness;
        ApplyBrightness();
    }

    // 让默认亮度保留原画面，调整时只改变曝光。
    private void ApplyBrightness()
    {
        colorAdjustments.postExposure.Override(Mathf.Log(GameSettings.Brightness, 2f));
    }

    // 离开场景后停止接收亮度变化。
    private void OnDisable()
    {
        GameSettings.BrightnessChanged -= ApplyBrightness;
    }

    // 释放本场景使用的后处理副本。
    private void OnDestroy()
    {
        foreach (VolumeComponent component in runtimeProfile.components) Destroy(component);
        Destroy(runtimeProfile);
    }
}
