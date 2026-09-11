using System.Collections.Generic;
using UnityEngine;

// 建筑遮挡处理，相机和玩家之间挡住视线的建筑会被临时隐藏
public sealed class HD2DBuildingOcclusion : MonoBehaviour
{
    [SerializeField] private float focusHeight = 0.9f; // 判定遮挡时看玩家身上的高度
    [SerializeField] private float rayPadding = 0.15f; // 射线终点离玩家保留的距离

    private Camera gameplayCamera; // 场景主相机
    private Transform target; // 跟随目标
    private readonly HashSet<Renderer> hiddenRenderers = new HashSet<Renderer>(); // 当前被隐藏的渲染器
    private readonly HashSet<Renderer> currentOccluders = new HashSet<Renderer>(); // 本次检测到的遮挡物
    private readonly RaycastHit[] raycastHits = new RaycastHit[64]; // 射线检测结果缓存

    // 记下摄像机和跟随目标
    public void Bind(Camera camera, Transform followTarget)
    {
        gameplayCamera = camera;
        target = followTarget;
    }

    // 每帧重新判断哪些建筑挡住了玩家
    private void LateUpdate()
    {
        if (gameplayCamera == null) gameplayCamera = HD2DSceneGM.GameplayCamera;
        if (target == null) target = transform;
        if (gameplayCamera != null && target != null) FindOccluders();
    }

    // 组件关掉时把藏起来的建筑还原
    private void OnDisable()
    {
        RestoreOccluders();
    }

    // 销毁时把藏起来的建筑还原
    private void OnDestroy()
    {
        RestoreOccluders();
    }

    // 从相机向玩家打一条射线，把挡在中间的建筑藏起来
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

        var ray = new Ray(origin, toTarget / distance);
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

    // 把碰撞体所属的建筑渲染器记为遮挡物
    private void AddOccluderFromCollider(Collider collider)
    {
        Renderer[] renderers = collider.GetComponentsInParent<Renderer>(true);
        foreach (Renderer renderer in renderers)
            if (IsBuildingRenderer(renderer)) currentOccluders.Add(renderer);
    }

    // 恢复这次已经不再挡视线的渲染器
    private void RestoreOccludersNotInCurrentSet()
    {
        var restored = new List<Renderer>();
        foreach (Renderer renderer in hiddenRenderers)
        {
            if (renderer == null || currentOccluders.Contains(renderer)) continue;
            renderer.enabled = true;
            restored.Add(renderer);
        }
        foreach (Renderer renderer in restored) hiddenRenderers.Remove(renderer);
    }

    // 把之前隐藏的渲染器全部恢复
    private void RestoreOccluders()
    {
        foreach (Renderer renderer in hiddenRenderers)
            if (renderer != null) renderer.enabled = true;
        hiddenRenderers.Clear();
    }

    // 按名字关键词判断是不是建筑，排除角色和特效
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
