using UnityEngine;

public sealed class HD2DCameraFollow : MonoBehaviour
{
    private Transform target;
    private Vector3 offset;
    private Quaternion cameraRotation;

    public void Bind(Transform followTarget, Vector3 followOffset, Quaternion followRotation)
    {
        target = followTarget;
        offset = followOffset;
        cameraRotation = followRotation;
    }

    private void LateUpdate()
    {
        Apply();
    }

    private void OnPreCull()
    {
        Apply();
    }

    private void Apply()
    {
        if (target == null) return;
        transform.SetPositionAndRotation(target.position + offset, cameraRotation);
    }
}
