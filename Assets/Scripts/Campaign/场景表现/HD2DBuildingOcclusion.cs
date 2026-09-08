using System.Collections.Generic;
using UnityEngine;

public sealed class HD2DBuildingOcclusion : MonoBehaviour
{
    [SerializeField] private float focusHeight = 0.9f;
    [SerializeField] private float rayPadding = 0.15f;

    private Camera gameplayCamera;
    private Transform target;
    private readonly HashSet<Renderer> hiddenRenderers = new HashSet<Renderer>();
    private readonly HashSet<Renderer> currentOccluders = new HashSet<Renderer>();
    private readonly RaycastHit[] raycastHits = new RaycastHit[64];

    public void Bind(Camera camera, Transform followTarget)
    {
        gameplayCamera = camera;
        target = followTarget;
    }

    private void LateUpdate()
    {
        if (gameplayCamera == null) gameplayCamera = HD2DSceneGM.GameplayCamera;
        if (target == null) target = transform;
        if (gameplayCamera != null && target != null) FindOccluders();
    }

    private void OnDisable()
    {
        RestoreOccluders();
    }

    private void OnDestroy()
    {
        RestoreOccluders();
    }

    private void FindOccluders()
    {
        currentOccluders.Clear();
        Vector3 origin = gameplayCamera.transform.position;
        Vector3 targetPoint = target.position + Vector3.up * focusHeight;
        Vector3 toTarget = targetPoint - origin;
        float distance = toTarget.magnitude;
        if (distance <= 0.01f)
        {
            RestoreOccluders();
            return;
        }

        Ray ray = new Ray(origin, toTarget / distance);
        float maxDistance = Mathf.Max(0f, distance - rayPadding);
        int hitCount = Physics.RaycastNonAlloc(ray, raycastHits, maxDistance, ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit = raycastHits[i];
            if (hit.collider == null || hit.distance <= 0f || hit.distance >= maxDistance) continue;
            AddOccluderFromCollider(hit.collider);
        }

        foreach (Renderer renderer in currentOccluders)
        {
            if (renderer == null || !renderer.enabled) continue;
            renderer.enabled = false;
            hiddenRenderers.Add(renderer);
        }
        RestoreOccludersNotInCurrentSet();
    }

    private void AddOccluderFromCollider(Collider collider)
    {
        Renderer[] renderers = collider.GetComponentsInParent<Renderer>(true);
        foreach (Renderer renderer in renderers)
            if (IsBuildingRenderer(renderer)) currentOccluders.Add(renderer);
    }

    private void RestoreOccludersNotInCurrentSet()
    {
        List<Renderer> restored = new List<Renderer>();
        foreach (Renderer renderer in hiddenRenderers)
        {
            if (renderer == null || currentOccluders.Contains(renderer)) continue;
            renderer.enabled = true;
            restored.Add(renderer);
        }
        foreach (Renderer renderer in restored) hiddenRenderers.Remove(renderer);
    }

    private void RestoreOccluders()
    {
        foreach (Renderer renderer in hiddenRenderers)
            if (renderer != null) renderer.enabled = true;
        hiddenRenderers.Clear();
    }

    private bool IsBuildingRenderer(Renderer renderer)
    {
        if (renderer == null) return false;
        if (renderer is SpriteRenderer || renderer is ParticleSystemRenderer) return false;
        if (renderer.transform == target || renderer.transform.IsChildOf(target)) return false;
        if (renderer.GetComponentInParent<HD2DSceneGM>() != null) return false;
        if (renderer.GetComponentInParent<WorldInteractionActor>() != null) return false;

        string objectName = renderer.transform.name;
        Transform parent = renderer.transform.parent;
        while (parent != null)
        {
            objectName += " " + parent.name;
            parent = parent.parent;
        }

        return objectName.Contains("EnvBdg") || objectName.Contains("EnvMdrMD_") ||
               objectName.Contains("SM_Wall") || objectName.Contains("SM_wall") ||
               objectName.Contains("Building") || objectName.Contains("House") ||
               objectName.Contains("Mansion") || objectName.Contains("DepartmentStore") ||
               objectName.Contains("Theater") || objectName.Contains("Pub");
    }
}
