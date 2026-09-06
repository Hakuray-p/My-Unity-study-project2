using UnityEngine;

/// <summary>
/// Builds the authored city block from environment prefabs and lightweight primitives.
/// The scene stores only data and prefab references; the layout is intentionally independent
/// from the large sample day/night scenes.
/// </summary>
public class CityArtDirector : MonoBehaviour
{
    [SerializeField] private bool harbor;
    [SerializeField] private Color groundColor = new Color(0.18f, 0.34f, 0.32f);
    [SerializeField] private GameObject[] landmarkPrefabs = new GameObject[0];
    [SerializeField] private Vector3[] landmarkPositions = new Vector3[0];
    [SerializeField] private Vector3[] landmarkRotations = new Vector3[0];
    [SerializeField] private Vector3[] landmarkScales = new Vector3[0];

    private bool built;

    private void Awake()
    {
        Build();
    }

    public void Build()
    {
        if (built) return;
        built = true;

        Transform artRoot = new GameObject("Authored City Art").transform;
        CreatePrimitive("City Ground", PrimitiveType.Plane, new Vector3(0f, -0.1f, 4f), new Vector3(4.8f, 1f, 4.8f), groundColor, artRoot);
        CreatePrimitive("Main Avenue", PrimitiveType.Cube, new Vector3(0f, 0.06f, 4f), new Vector3(5f, 0.12f, 26f), new Color(0.25f, 0.22f, 0.19f), artRoot);
        CreatePrimitive("Cross Street", PrimitiveType.Cube, new Vector3(0f, 0.08f, 4f), new Vector3(24f, 0.14f, 4.4f), new Color(0.29f, 0.25f, 0.20f), artRoot);
        CreatePrimitive("Market Plaza", PrimitiveType.Cylinder, new Vector3(0f, 0.14f, 17f), new Vector3(5.5f, 0.18f, 5.5f), new Color(0.44f, 0.37f, 0.28f), artRoot);
        CreatePrimitive("West Walk", PrimitiveType.Cube, new Vector3(-8.5f, 0.12f, 4f), new Vector3(5.5f, 0.18f, 25f), new Color(0.36f, 0.32f, 0.27f), artRoot);
        CreatePrimitive("East Walk", PrimitiveType.Cube, new Vector3(8.5f, 0.12f, 4f), new Vector3(5.5f, 0.18f, 25f), new Color(0.36f, 0.32f, 0.27f), artRoot);

        if (harbor)
        {
            CreatePrimitive("Harbor Water", PrimitiveType.Plane, new Vector3(16f, -0.03f, 15f), new Vector3(1.25f, 1f, 1.15f), new Color(0.08f, 0.28f, 0.38f), artRoot);
            CreatePrimitive("Harbor Pier", PrimitiveType.Cube, new Vector3(14f, 0.2f, 14f), new Vector3(6f, 0.35f, 1.3f), new Color(0.39f, 0.25f, 0.14f), artRoot);
            CreateMatchMarker("Dockside Open", "glass_harbor_public_01", new Vector3(10f, 0.7f, 5f), new Color(0.36f, 0.64f, 0.90f), artRoot);
            CreateMatchMarker("Moon Market Open", "glass_harbor_public_02", new Vector3(-7f, 0.7f, 10f), new Color(0.94f, 0.65f, 0.27f), artRoot);
            CreateMatchMarker("Harbor Champion", "glass_harbor_champion", new Vector3(0f, 0.7f, 20f), new Color(0.92f, 0.35f, 0.48f), artRoot);
        }
        else
        {
            CreateMatchMarker("Practice Table", "first_light_practice", new Vector3(-7f, 0.7f, -1f), new Color(0.31f, 0.82f, 0.70f), artRoot);
            CreateMatchMarker("Riverside Open", "first_light_public_01", new Vector3(7f, 0.7f, 2f), new Color(0.36f, 0.64f, 0.90f), artRoot);
            CreateMatchMarker("Lantern / Sunset Open", "first_light_public_02", new Vector3(-7f, 0.7f, 10f), new Color(0.94f, 0.65f, 0.27f), artRoot);
            CreateMatchMarker("City Champion", "first_light_champion", new Vector3(0f, 0.7f, 20f), new Color(0.92f, 0.35f, 0.48f), artRoot);
        }

        for (int i = 0; i < landmarkPrefabs.Length; i++)
        {
            GameObject prefab = landmarkPrefabs[i];
            if (prefab == null) continue;

            Vector3 position = i < landmarkPositions.Length ? landmarkPositions[i] : Vector3.zero;
            Vector3 rotation = i < landmarkRotations.Length ? landmarkRotations[i] : Vector3.zero;
            Vector3 scale = i < landmarkScales.Length ? landmarkScales[i] : Vector3.one;
            GameObject instance = Instantiate(prefab, position, Quaternion.Euler(rotation), artRoot);
            instance.name = prefab.name + " (authored layout)";
            instance.transform.localScale = scale;
        }

        CreateBoundary(new Vector3(0f, 1f, -18f), new Vector3(44f, 2f, 1f), artRoot);
        CreateBoundary(new Vector3(0f, 1f, 28f), new Vector3(44f, 2f, 1f), artRoot);
        CreateBoundary(new Vector3(-22f, 1f, 5f), new Vector3(1f, 2f, 46f), artRoot);
        CreateBoundary(new Vector3(22f, 1f, 5f), new Vector3(1f, 2f, 46f), artRoot);
    }

    private static GameObject CreatePrimitive(string name, PrimitiveType type, Vector3 position, Vector3 scale, Color color, Transform parent)
    {
        GameObject instance = GameObject.CreatePrimitive(type);
        instance.name = name;
        instance.transform.SetParent(parent);
        instance.transform.position = position;
        instance.transform.localScale = scale;
        Renderer renderer = instance.GetComponent<Renderer>();
        if (renderer != null) renderer.material.color = color;
        return instance;
    }

    private static void CreateBoundary(Vector3 position, Vector3 scale, Transform parent)
    {
        GameObject wall = CreatePrimitive("City Boundary", PrimitiveType.Cube, position, scale, Color.clear, parent);
        Renderer renderer = wall.GetComponent<Renderer>();
        if (renderer != null) renderer.enabled = false;
    }

    private static void CreateMatchMarker(string label, string matchId, Vector3 position, Color color, Transform parent)
    {
        GameObject marker = CreatePrimitive(label, PrimitiveType.Cylinder, position, new Vector3(1.2f, 0.7f, 1.2f), color, parent);
        Collider baseCollider = marker.GetComponent<Collider>();
        if (baseCollider != null) baseCollider.isTrigger = true;
        SphereCollider trigger = marker.AddComponent<SphereCollider>();
        trigger.isTrigger = true;
        trigger.radius = 1.8f;
        CityMatchPoint point = marker.AddComponent<CityMatchPoint>();
        point.matchId = matchId;
        point.displayName = label;
    }
}

