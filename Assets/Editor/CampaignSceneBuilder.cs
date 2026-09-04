using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class CampaignSceneBuilder
{
    private const string SceneFolder = "Assets/Scenes";
    private const string PrefabFolder = "Assets/素材/Environment/Prefabs";

    [MenuItem("ArkCard/Build Campaign Scenes")]
    public static void BuildAllCampaignScenes()
    {
        EnsureFolder("Assets/Editor");
        BuildWorldMap();
        BuildCity("first_light", "City_FirstLight", new Color(0.18f, 0.34f, 0.32f), false);
        BuildCity("glass_harbor", "City_GlassHarbor", new Color(0.07f, 0.16f, 0.24f), true);
        AttachMenuController();
        ConfigureBuildSettings();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Campaign scenes built.");
    }

    private static void BuildWorldMap()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "WorldMap";
        CreateCamera("WorldMap Camera", new Vector3(0f, 14f, -10f), new Vector3(55f, 0f, 0f), 11f);
        CreateLight("WorldMap Sun", new Vector3(50f, -30f, 25f), new Color(1f, 0.82f, 0.64f), 1.2f);

        GameObject root = new GameObject("WorldMap Runtime");
        root.AddComponent<WorldMapController>();
        CreatePrimitive("Map Ground", PrimitiveType.Plane, new Vector3(0f, -0.1f, 0f), new Vector3(2.4f, 1f, 1.4f), new Color(0.10f, 0.23f, 0.22f), root.transform);
        CreatePrimitive("League Route", PrimitiveType.Cube, new Vector3(0f, 0.1f, 0f), new Vector3(10f, 0.18f, 0.5f), new Color(0.76f, 0.53f, 0.24f), root.transform);
        AddPrefab("BackdropMountains", new Vector3(0f, -1.5f, 8f), Vector3.one, root.transform);
        AddPrefab("ObjMD_Bridge_C", new Vector3(0f, 0f, 2.5f), Vector3.one, root.transform);
        AddPrefab("EnvObjMD_Fountain_A_B", new Vector3(0f, 0f, -2.5f), Vector3.one, root.transform);

        CreateMapNode("First Light", "first_light", new Vector3(-5f, 0.6f, 0f), root.transform, new Color(0.31f, 0.84f, 0.70f));
        CreateMapNode("Glass Harbor", "glass_harbor", new Vector3(5f, 0.6f, 0f), root.transform, new Color(0.26f, 0.56f, 0.88f));
        EditorSceneManager.SaveScene(scene, SceneFolder + "/WorldMap.unity");
    }

    private static void BuildCity(string cityId, string sceneName, Color groundColor, bool harbor)
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = sceneName;
        RenderSettings.ambientLight = groundColor * 1.55f;
        RenderSettings.fog = true;
        RenderSettings.fogColor = groundColor;
        RenderSettings.fogDensity = 0.006f;

        GameObject environment = new GameObject("City Environment");
        CreatePrimitive("City Ground", PrimitiveType.Plane, new Vector3(0f, -0.1f, 2f), new Vector3(4.8f, 1f, 4.8f), groundColor, environment.transform);
        CreatePrimitive("Main Avenue", PrimitiveType.Cube, new Vector3(0f, 0.06f, 2f), new Vector3(5f, 0.12f, 26f), new Color(0.25f, 0.22f, 0.19f), environment.transform);
        CreatePrimitive("Cross Street", PrimitiveType.Cube, new Vector3(0f, 0.08f, 2f), new Vector3(24f, 0.14f, 4.4f), new Color(0.29f, 0.25f, 0.20f), environment.transform);
        CreatePrimitive("Plaza", PrimitiveType.Cylinder, new Vector3(0f, 0.14f, 12f), new Vector3(5.5f, 0.18f, 5.5f), new Color(0.44f, 0.37f, 0.28f), environment.transform);

        AddPrefab("EnvBdgMD_City_B_Outdoor_L_B", new Vector3(-10f, 0f, 11f), Vector3.one, environment.transform);
        AddPrefab("EnvBdgMD_City_B_Outdoor_L_F_Redwall_A", new Vector3(10f, 0f, 11f), Vector3.one, environment.transform);
        AddPrefab("EnvBdgMD_City_B_Outdoor_L_G", new Vector3(-11f, 0f, -4f), Vector3.one, environment.transform);
        AddPrefab("EnvBdgMD_City_B_Outdoor_Pub_A", new Vector3(11f, 0f, -4f), Vector3.one, environment.transform);
        AddPrefab("EnvBdgMD_City_B_Outdoor_Theater_A", new Vector3(0f, 0f, 21f), Vector3.one, environment.transform);
        AddPrefab("EnvMdrMD_Garden_A_Bush_A", new Vector3(-8f, 0f, 18f), Vector3.one, environment.transform);
        AddPrefab("EnvMdrMD_Garden_A_Bush_B", new Vector3(8f, 0f, 18f), Vector3.one, environment.transform);
        AddPrefab("EnvObjMD_Fountain_A_B", new Vector3(-4.5f, 0f, 12f), Vector3.one, environment.transform);
        AddPrefab("ObjMD_Bridge_C", new Vector3(5f, 0f, 12f), Vector3.one, environment.transform);
        AddPrefab("SM_light01", new Vector3(-3f, 0f, 4f), Vector3.one, environment.transform);
        AddPrefab("SM_light02", new Vector3(3f, 0f, 4f), Vector3.one, environment.transform);
        AddPrefab("SM_light03", new Vector3(-3f, 0f, 17f), Vector3.one, environment.transform);
        AddPrefab("SM_light04", new Vector3(3f, 0f, 17f), Vector3.one, environment.transform);
        if (harbor)
        {
            AddPrefab("ObjMD_Boat_A", new Vector3(17f, 0f, 16f), Vector3.one, environment.transform);
            AddPrefab("EnvObjMD_Canoedock_A_Base", new Vector3(16f, 0f, 9f), Vector3.one, environment.transform);
            AddPrefab("EnvMdrMD_Sea_A_Fence_A_Loop_A", new Vector3(14f, 0f, 2f), Vector3.one, environment.transform);
        }

        CreateBoundary(new Vector3(0f, 1f, -18f), new Vector3(44f, 2f, 1f), environment.transform);
        CreateBoundary(new Vector3(0f, 1f, 28f), new Vector3(44f, 2f, 1f), environment.transform);
        CreateBoundary(new Vector3(-22f, 1f, 5f), new Vector3(1f, 2f, 46f), environment.transform);
        CreateBoundary(new Vector3(22f, 1f, 5f), new Vector3(1f, 2f, 46f), environment.transform);

        GameObject runtime = new GameObject("City Runtime");
        CityController cityController = runtime.AddComponent<CityController>();
        cityController.cityId = cityId;
        cityController.playerStart = new Vector3(0f, 1f, -11f);
        cityController.moveSpeed = 5f;
        if (!harbor)
        {
            CreateMatchPoint("Practice Table", "first_light_practice", new Vector3(-8f, 0.7f, -1f), environment.transform, new Color(0.31f, 0.82f, 0.70f));
            CreateMatchPoint("Riverside Open", "first_light_public_01", new Vector3(8f, 0.7f, 1f), environment.transform, new Color(0.36f, 0.64f, 0.90f));
            CreateMatchPoint("Lantern Open", "first_light_public_02", new Vector3(-7f, 0.7f, 9f), environment.transform, new Color(0.94f, 0.65f, 0.27f));
            CreateMatchPoint("City Champion", "first_light_champion", new Vector3(0f, 0.7f, 18f), environment.transform, new Color(0.92f, 0.35f, 0.48f));
        }
        else
        {
            CreateMatchPoint("Dockside Open", "glass_harbor_public_01", new Vector3(8f, 0.7f, 1f), environment.transform, new Color(0.36f, 0.64f, 0.90f));
            CreateMatchPoint("Moon Market Open", "glass_harbor_public_02", new Vector3(-7f, 0.7f, 9f), environment.transform, new Color(0.94f, 0.65f, 0.27f));
            CreateMatchPoint("Harbor Champion", "glass_harbor_champion", new Vector3(0f, 0.7f, 18f), environment.transform, new Color(0.92f, 0.35f, 0.48f));
        }

        CreateCamera("City Camera", new Vector3(9f, 13f, -20f), new Vector3(48f, 27f, 0f), 9f);
        CreateLight("City Sun", new Vector3(50f, -30f, 35f), harbor ? new Color(0.42f, 0.58f, 1f) : new Color(1f, 0.80f, 0.56f), harbor ? 0.85f : 1.35f);
        EditorSceneManager.SaveScene(scene, SceneFolder + "/" + sceneName + ".unity");
    }

    private static void AttachMenuController()
    {
        string path = SceneFolder + "/GameMenu.unity";
        if (!System.IO.File.Exists(path)) return;
        Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        if (Object.FindObjectOfType<GameMenuController>() == null)
        {
            GameObject root = new GameObject("Campaign Menu Runtime");
            root.AddComponent<GameMenuController>();
        }
        EditorSceneManager.SaveScene(scene, path);
    }

    private static void ConfigureBuildSettings()
    {
        string[] paths =
        {
            SceneFolder + "/GameMenu.unity",
            SceneFolder + "/WorldMap.unity",
            SceneFolder + "/City_FirstLight.unity",
            SceneFolder + "/City_GlassHarbor.unity",
            SceneFolder + "/BattleScene.unity"
        };
        List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>();
        foreach (string path in paths)
        {
            if (System.IO.File.Exists(path)) scenes.Add(new EditorBuildSettingsScene(path, true));
        }
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    private static GameObject CreateMatchPoint(string label, string matchId, Vector3 position, Transform parent, Color color)
    {
        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        marker.name = label;
        marker.transform.SetParent(parent);
        marker.transform.position = position;
        marker.transform.localScale = new Vector3(1.2f, 0.7f, 1.2f);
        marker.GetComponent<Renderer>().material.color = color;
        Collider markerCollider = marker.GetComponent<Collider>();
        if (markerCollider != null) markerCollider.isTrigger = true;
        SphereCollider trigger = marker.AddComponent<SphereCollider>();
        trigger.isTrigger = true;
        trigger.radius = 1.8f;
        CityMatchPoint point = marker.AddComponent<CityMatchPoint>();
        point.matchId = matchId;
        point.displayName = label;
        return marker;
    }

    private static GameObject CreateMapNode(string label, string cityId, Vector3 position, Transform parent, Color color)
    {
        GameObject node = CreatePrimitive(label, PrimitiveType.Cylinder, position, new Vector3(1.2f, 0.6f, 1.2f), color, parent);
        CityMapNode mapNode = node.AddComponent<CityMapNode>();
        mapNode.cityId = cityId;
        return node;
    }

    private static void CreateBoundary(Vector3 position, Vector3 scale, Transform parent)
    {
        GameObject wall = CreatePrimitive("City Boundary", PrimitiveType.Cube, position, scale, new Color(0f, 0f, 0f, 0f), parent);
        wall.GetComponent<Renderer>().enabled = false;
    }

    private static Camera CreateCamera(string name, Vector3 position, Vector3 euler, float size)
    {
        GameObject cameraObject = new GameObject(name);
        cameraObject.tag = "MainCamera";
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = size;
        camera.transform.position = position;
        camera.transform.rotation = Quaternion.Euler(euler);
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.06f, 0.09f, 0.13f);
        cameraObject.AddComponent<AudioListener>();
        return camera;
    }

    private static void CreateLight(string name, Vector3 euler, Color color, float intensity)
    {
        GameObject lightObject = new GameObject(name);
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.color = color;
        light.intensity = intensity;
        light.transform.rotation = Quaternion.Euler(euler);
    }

    private static GameObject CreatePrimitive(string name, PrimitiveType type, Vector3 position, Vector3 scale, Color color, Transform parent)
    {
        GameObject objectInstance = GameObject.CreatePrimitive(type);
        objectInstance.name = name;
        objectInstance.transform.SetParent(parent);
        objectInstance.transform.position = position;
        objectInstance.transform.localScale = scale;
        objectInstance.GetComponent<Renderer>().material.color = color;
        return objectInstance;
    }

    private static GameObject AddPrefab(string prefabName, Vector3 position, Vector3 scale, Transform parent)
    {
        string[] guids = AssetDatabase.FindAssets(prefabName + " t:Prefab", new[] { PrefabFolder });
        if (guids.Length == 0) return null;
        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null) return null;
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.name = prefabName;
        instance.transform.SetParent(parent);
        instance.transform.position = position;
        instance.transform.localScale = scale;
        return instance;
    }

    private static void EnsureFolder(string path)
    {
        if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder("Assets", "Editor");
    }
}
