using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// 保存事件完成位置并播放场景中预先制作的裂痕特效。
/// </summary>
public sealed class WorldEventPoint : MonoBehaviour
{
    [SerializeField] private string eventId = "first_light_event_01"; // 事件 ID
    [SerializeField] private Transform markerVisual; // 场景中的裂痕标记节点
    [SerializeField] private LineRenderer groundRing; // 地面光圈
    [SerializeField] private LineRenderer crackLine; // 裂痕线条
    [SerializeField] private ParticleSystem ambientParticles; // 提示位置的光点
    [SerializeField] private ParticleSystem gatherParticles; // 调查时聚拢的光点
    [SerializeField] private ParticleSystem completionParticles; // 完成时散去的光点
    private bool markerVisible; // 标记当前是否显示
    private Color markerColor; // 光圈的初始颜色

    public string EventId => eventId; // 当前事件标识
    public bool IsResolving { get; private set; } // 当前是否正在播放调查反馈

    // 接手已经制作好的裂痕标记并先藏起来。
    private void Awake()
    {
        markerColor = groundRing.startColor;
        SetVisible(false);
    }

    // 让标记做呼吸缩放。
    private void Update()
    {
        if (!markerVisible || IsResolving) return;
        float pulse = 1f + Mathf.Sin(Time.time * 3f) * 0.06f;
        markerVisual.localScale = Vector3.one * pulse;
    }

    /// <summary>
    /// 控制裂痕标记显示状态，不打断正在播放的完成反馈。
    /// </summary>
    public void SetVisible(bool visible)
    {
        markerVisible = visible;
        if (IsResolving) return;
        markerVisual.gameObject.SetActive(visible);
        markerVisual.localScale = Vector3.one;
        SetLineColor(markerColor);
        if (visible) ambientParticles.Play();
        else ambientParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    // 调查时先聚光，再结算事件并播放消散。
    public void PlayResolve(Action resolve)
    {
        if (IsResolving) return;
        IsResolving = true;
        StartCoroutine(ResolveEffect(resolve));
    }

    // 按调查与完成阶段播放轻量视觉反馈。
    private IEnumerator ResolveEffect(Action resolve)
    {
        ambientParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        gatherParticles.Play();
        float elapsed = 0f;
        while (elapsed < 0.65f)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / 0.65f);
            markerVisual.localScale = Vector3.one * Mathf.Lerp(1f, 0.75f, progress);
            SetLineColor(Color.Lerp(markerColor, new Color(1.6f, 1.4f, 1f, 1f), progress));
            yield return null;
        }
        gatherParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        completionParticles.Play();
        resolve();
        elapsed = 0f;
        while (elapsed < 1f)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed);
            markerVisual.localScale = Vector3.one * Mathf.Lerp(0.75f, 0.05f, progress);
            SetLineColor(new Color(1.6f, 1.4f, 1f, 1f - progress));
            yield return null;
        }
        IsResolving = false;
        SetVisible(false);
    }

    // 设置线条渲染器的颜色。
    private void SetLineColor(Color color)
    {
        groundRing.startColor = color;
        groundRing.endColor = color;
        crackLine.startColor = color;
        crackLine.endColor = color;
    }

    // 停用事件点时停止未完成的视觉反馈，不触发奖励。
    private void OnDisable()
    {
        StopAllCoroutines();
        IsResolving = false;
    }
}
