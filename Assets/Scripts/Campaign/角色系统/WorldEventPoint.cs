using UnityEngine;

/// <summary>
/// 保存事件完成位置并显示运行时裂痕标记。
/// </summary>
public sealed class WorldEventPoint : MonoBehaviour
{
    [SerializeField] private string eventId = "first_light_event_01"; // 事件 ID
    [SerializeField] private float markerRadius = 0.65f; // 裂痕标记半径
    [SerializeField] private Color markerColor = new Color(0.75f, 0.1f, 1f, 1f); // 裂痕标记颜色

    private Transform markerVisual;
    private Material markerMaterial;
    private bool markerVisible;

    public string EventId => eventId;

    private void Awake()
    {
        CreateMarker();
        SetVisible(false);
    }

    private void Update()
    {
        if (!markerVisible || markerVisual == null) return;
        float pulse = 1f + Mathf.Sin(Time.time * 4f) * 0.08f;
        markerVisual.localScale = Vector3.one * pulse;
    }

    /// <summary>
    /// 控制裂痕标记显示状态。
    /// </summary>
    public void SetVisible(bool visible)
    {
        markerVisible = visible;
        if (markerVisual != null) markerVisual.gameObject.SetActive(visible);
    }

    private void CreateMarker()
    {
        markerVisual = new GameObject("RiftVisual").transform;
        markerVisual.SetParent(transform, false);
        markerMaterial = new Material(Shader.Find("Sprites/Default"));

        var groundRingObject = new GameObject("GroundRing");
        groundRingObject.transform.SetParent(markerVisual, false);
        var groundRing = groundRingObject.AddComponent<LineRenderer>();
        ConfigureLine(groundRing, 0.06f, true);
        groundRing.positionCount = 32;
        for (int index = 0; index < groundRing.positionCount; index++)
        {
            float angle = index / (float)groundRing.positionCount * Mathf.PI * 2f;
            groundRing.SetPosition(index, new Vector3(Mathf.Cos(angle) * markerRadius, 0.05f,
                Mathf.Sin(angle) * markerRadius * 0.45f));
        }

        var crackLineObject = new GameObject("CrackLine");
        crackLineObject.transform.SetParent(markerVisual, false);
        var crackLine = crackLineObject.AddComponent<LineRenderer>();
        ConfigureLine(crackLine, 0.1f, false);
        crackLine.positionCount = 7;
        crackLine.SetPosition(0, new Vector3(-0.32f, 0.08f, 0.12f));
        crackLine.SetPosition(1, new Vector3(-0.12f, 0.08f, -0.08f));
        crackLine.SetPosition(2, new Vector3(-0.04f, 0.08f, 0.12f));
        crackLine.SetPosition(3, new Vector3(0.1f, 0.08f, -0.16f));
        crackLine.SetPosition(4, new Vector3(0.18f, 0.08f, 0.08f));
        crackLine.SetPosition(5, new Vector3(0.32f, 0.08f, -0.04f));
        crackLine.SetPosition(6, new Vector3(0.4f, 0.08f, 0.12f));
    }

    private void ConfigureLine(LineRenderer lineRenderer, float width, bool loop)
    {
        lineRenderer.useWorldSpace = false;
        lineRenderer.loop = loop;
        lineRenderer.widthMultiplier = width;
        lineRenderer.numCapVertices = 4;
        lineRenderer.material = markerMaterial;
        lineRenderer.startColor = markerColor;
        lineRenderer.endColor = markerColor;
    }

    private void OnDestroy()
    {
        if (markerMaterial != null) Destroy(markerMaterial);
    }
}
