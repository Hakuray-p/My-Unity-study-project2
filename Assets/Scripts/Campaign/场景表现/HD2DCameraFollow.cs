using UnityEngine;

// 跟随目标的相机，LateUpdate 和渲染前都对齐一次，避免画面抖动
public sealed class HD2DCameraFollow : MonoBehaviour
{
    private Transform target; // 跟随目标
    private Vector3 offset; // 相对目标的偏移
    private Quaternion cameraRotation; // 相机固定朝向

    // 记下跟随目标和相机的位姿
    public void Bind(Transform followTarget, Vector3 followOffset, Quaternion followRotation)
    {
        target = followTarget;
        offset = followOffset;
        cameraRotation = followRotation;
    }

    // 每帧跟随目标
    private void LateUpdate()
    {
        Apply();
    }

    // 剔除前再贴一次位置，避免画面抖动
    private void OnPreCull()
    {
        Apply();
    }

    // 把相机放到目标加偏移的位置上
    private void Apply()
    {
        if (target == null) return;
        transform.SetPositionAndRotation(target.position + offset, cameraRotation);
    }
}
