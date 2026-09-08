using UnityEngine;

/// <summary>
/// 用来调整整套对话 Canvas 的屏幕位置。只移动 Canvas 根节点，不改 DialoguePanel、头像、文字和按钮。
/// 可把此组件手动添加到每个 xxx的DialogueCanvas，然后在 Inspector 调整 Screen Offset。
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
public sealed class DialogueCanvasTuner : MonoBehaviour
{
    [Tooltip("相对于屏幕中心的偏移；Y 为负数会向屏幕底部移动。")]
    public Vector2 screenOffset = new Vector2(0f, -360f);

    [Tooltip("整套 Canvas 的等比缩放。建议保持 1,1,1。")]
    public Vector3 contentScale = Vector3.one;

    [Tooltip("运行时使用屏幕摄像机，保证 UI 正面显示。")]
    public bool forceScreenSpaceCamera = true;

    public void Apply(Camera targetCamera) { }
}
