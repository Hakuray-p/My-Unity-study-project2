using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;

/// <summary>
/// One-time project setup for the single-scene HD adventure flow.
/// Run from the menu or with -executeMethod HDAdventureSetup.RunBatchSetup.
/// </summary>
public static class HDAdventureSetup
{
    private const string AdventureScenePath = "Assets/Scenes/HD_2D_Day.unity";
    private const string ConfigPath = "Assets/Resources/HDAdventureConfig.asset";
    private const string PlayerFramePath = "Assets/Resources/Ark Image/Amiya/amiya_preview.png";
    private const string LegacyPlayerFramePath = "Assets/Resources/Ark Image/Amiya/amiya_down_0.png";
    private const string DownRightSheetPath = "Assets/素材/Ark Image/阿米娅向下行走（如果是朝右情况下）.png";
    private const string DownLeftSheetPath = "Assets/素材/Ark Image/阿米娅向下行走（如果是朝左行走）.png";
    private const string LeftSheetPath = "Assets/素材/Ark Image/阿米娅朝左走.png";
    private const string RightSheetPath = "Assets/素材/Ark Image/阿米娅朝右走.png";
    private const string BackSheetSourcePath = "Assets/素材/Ark Image/阿米娅向后行走.jpg";
    private const string BackSheetPath = "Assets/Resources/Ark Image/Amiya/amiya_back_sheet.png";
    // The source sheet carries a small generator mark in the lower-right
    // corner. Coordinates are authored in the usual top-left image space;
    // GetPixels32 uses a bottom-left origin, so the helper converts them.
    private const float BackSheetWatermarkMinXNormalized = 1650f / 2048f;
    private const float BackSheetWatermarkMinTopYNormalized = 1840f / 2048f;
    private static readonly Vector3 OriginalCameraPosition = new Vector3(38.13f, 23.94f, 45.73f);
    private static readonly Vector3 OriginalCameraEulerAngles = new Vector3(17f, 0f, 0f);

    [MenuItem("ArkCard/Setup HD Adventure")]
    public static void RunBatchSetup()
    {
        if (!EnsureEditorMode("Setup HD Adventure")) return;
        EnsureConfigAsset();
        EnsureAmiyaSheetAssets();
        ConfigureBuildSettings();
        if (!ConfigureAdventureScene()) return;
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("ArkCard HD adventure setup complete.");
    }

    [MenuItem("ArkCard/Clean Amiya Back Sheet Watermark")]
    public static void CleanAmiyaBackSheetWatermark()
    {
        if (!EnsureEditorMode("Clean Amiya Back Sheet Watermark")) return;
        GenerateTransparentBackSheet();
        ConfigureAmiyaTexture(BackSheetPath, true, true);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Amiya back sheet watermark cleaned. The source JPG was kept unchanged.");
    }

    [MenuItem("ArkCard/Replace Scene Amiya With New Art")]
    public static void ReplaceSceneAmiyaWithNewArt()
    {
        if (!EnsureEditorMode("Replace Scene Amiya With New Art")) return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        EnsureConfigAsset();
        EnsureAmiyaSheetAssets();
        Scene scene = EditorSceneManager.OpenScene(AdventureScenePath, OpenSceneMode.Single);
        Camera camera = null;
        Camera[] cameras = Object.FindObjectsOfType<Camera>(true);
        foreach (Camera candidate in cameras)
        {
            if (candidate != null && candidate.gameObject.name == "Main Camera")
            {
                camera = candidate;
                break;
            }
        }

        EnsureAmiyaInScene(scene, camera);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Scene Amiya replaced with the new authored sprite sheets and saved to HD_2D_Day.");
    }

    [MenuItem("ArkCard/Setup HD Building Colliders")]
    public static void SetupBuildingColliders()
    {
        if (!EnsureEditorMode("Setup HD Building Colliders")) return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        Scene scene = EditorSceneManager.OpenScene(AdventureScenePath, OpenSceneMode.Single);
        int addedCount = AddMissingBuildingColliders(scene, out List<string> addedNames);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        StringBuilder report = new StringBuilder();
        report.AppendLine($"HD building collider setup complete. Added {addedCount} BoxCollider objects.");
        foreach (string name in addedNames) report.AppendLine("- " + name);
        Debug.Log(report.ToString());
    }

    [MenuItem("ArkCard/Validate HD Adventure Scene")]
    public static void ValidateAdventureScene()
    {
        if (!EnsureEditorMode("Validate HD Adventure Scene")) return;
        Scene scene = EditorSceneManager.OpenScene(AdventureScenePath, OpenSceneMode.Single);
        HDAdventureConfig config = AssetDatabase.LoadAssetAtPath<HDAdventureConfig>(ConfigPath);
        Camera[] cameras = Object.FindObjectsOfType<Camera>(true);
        int enabledCameras = 0;
        int enabledListeners = 0;
        bool cameraAnimationEnabled = false;
        foreach (Camera camera in cameras)
        {
            if (camera == null) continue;
            if (camera.enabled) enabledCameras++;
            AudioListener listener = camera.GetComponent<AudioListener>();
            if (listener != null && listener.enabled) enabledListeners++;
            Animator animator = camera.GetComponent<Animator>();
            PlayableDirector director = camera.GetComponent<PlayableDirector>();
            if ((animator != null && animator.enabled) || (director != null && director.enabled))
                cameraAnimationEnabled = true;
        }

        int legacyCount = Object.FindObjectsOfType<WorldMapController>(true).Length
            + Object.FindObjectsOfType<CityController>(true).Length
            + Object.FindObjectsOfType<GameMenuController>(true).Length;
        int amiyaCount = Object.FindObjectsOfType<AmiyaCharacter>(true).Length;

        bool configValid = config != null && config.mapTexture != null && config.gridTexture != null && config.playerTexture != null;
        AmiyaCharacter sceneCharacter = Object.FindObjectOfType<AmiyaCharacter>(true);
        bool authoredArtValid = sceneCharacter != null &&
                                sceneCharacter.downRightSheet != null &&
                                sceneCharacter.downLeftSheet != null &&
                                sceneCharacter.leftSheet != null &&
                                sceneCharacter.rightSheet != null &&
                                sceneCharacter.backSheet != null &&
                                AssetDatabase.LoadAssetAtPath<Sprite>(PlayerFramePath) != null;
        if (enabledCameras != 1 || enabledListeners > 1 || cameraAnimationEnabled || legacyCount > 0 ||
            amiyaCount != 1 || !configValid || !authoredArtValid)
        {
            Debug.LogError($"HD adventure scene needs setup: cameras {enabledCameras}, listeners {enabledListeners}, camera animation enabled {cameraAnimationEnabled}, legacy components {legacyCount}, Amiya count {amiyaCount}, config valid {configValid}, authored Amiya art valid {authoredArtValid}. Run ArkCard/Replace Scene Amiya With New Art.");
            return;
        }

        Debug.Log("HD adventure scene validation passed: one enabled camera, at most one enabled AudioListener, one scene Amiya, no legacy adventure components.");
    }

    private static bool EnsureEditorMode(string operation)
    {
        if (!Application.isPlaying) return true;

        Debug.LogWarning($"ArkCard/{operation} is an editor-only operation. Stop Play Mode before running it.");
        return false;
    }

    [MenuItem("ArkCard/Setup HD Adventure", true)]
    [MenuItem("ArkCard/Clean Amiya Back Sheet Watermark", true)]
    [MenuItem("ArkCard/Replace Scene Amiya With New Art", true)]
    [MenuItem("ArkCard/Setup HD Building Colliders", true)]
    [MenuItem("ArkCard/Validate HD Adventure Scene", true)]
    private static bool ValidateEditorOnlyMenus()
    {
        return !Application.isPlaying;
    }

    private static void EnsureConfigAsset()
    {
        HDAdventureConfig config = AssetDatabase.LoadAssetAtPath<HDAdventureConfig>(ConfigPath);
        if (config == null)
        {
            config = ScriptableObject.CreateInstance<HDAdventureConfig>();
            AssetDatabase.CreateAsset(config, ConfigPath);
        }

        config.mapTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/素材/Ark Image/地图.jpg");
        config.gridTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/素材/Ark Image/格子.png");
        config.playerTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(RightSheetPath);
        config.entryOffset = new Vector3(2.3f, -0.75f, 1.2f);
        config.cellSpacing = 86f;
        config.mapPanelSize = new Vector2(920f, 560f);
        config.freeMoveSpeed = 4f;
        EditorUtility.SetDirty(config);
    }

    private static void EnsureAmiyaSheetAssets()
    {
        ConfigureAmiyaTexture(DownRightSheetPath, true, true);
        ConfigureAmiyaTexture(DownLeftSheetPath, true, true);
        ConfigureAmiyaTexture(LeftSheetPath, true, true);
        ConfigureAmiyaTexture(RightSheetPath, true, true);
        GenerateAmiyaPreviewSprite();
        GenerateTransparentBackSheet();
        ConfigureAmiyaTexture(BackSheetPath, true, true);
        AssetDatabase.Refresh();
    }

    private static void GenerateAmiyaPreviewSprite()
    {
        TextureImporter sourceImporter = AssetImporter.GetAtPath(RightSheetPath) as TextureImporter;
        if (sourceImporter == null) return;

        if (!sourceImporter.isReadable)
        {
            sourceImporter.isReadable = true;
            sourceImporter.SaveAndReimport();
        }

        Texture2D source = AssetDatabase.LoadAssetAtPath<Texture2D>(RightSheetPath);
        if (source == null) return;

        const int columns = 4;
        const int rows = 4;
        int cellWidth = source.width / columns;
        int cellHeight = source.height / rows;
        if (cellWidth < 1 || cellHeight < 1) return;

        Color32[] sourcePixels = source.GetPixels32();
        Color32[] previewPixels = new Color32[cellWidth * cellHeight];
        int topRowTextureY = (rows - 1) * cellHeight;
        for (int y = 0; y < cellHeight; y++)
        {
            int sourceRow = (topRowTextureY + y) * source.width;
            int previewRow = y * cellWidth;
            for (int x = 0; x < cellWidth; x++)
                previewPixels[previewRow + x] = sourcePixels[sourceRow + x];
        }

        Texture2D preview = new Texture2D(cellWidth, cellHeight, TextureFormat.RGBA32, false, false);
        preview.SetPixels32(previewPixels);
        preview.Apply(false, false);
        string absolutePath = Path.Combine(Application.dataPath, PlayerFramePath.Substring("Assets/".Length));
        File.WriteAllBytes(absolutePath, preview.EncodeToPNG());
        Object.DestroyImmediate(preview);
        AssetDatabase.ImportAsset(PlayerFramePath, ImportAssetOptions.ForceUpdate);

        TextureImporter previewImporter = AssetImporter.GetAtPath(PlayerFramePath) as TextureImporter;
        if (previewImporter != null)
        {
            previewImporter.textureType = TextureImporterType.Sprite;
            previewImporter.spriteImportMode = SpriteImportMode.Single;
            previewImporter.spritePivot = new Vector2(0.5f, 0.08f);
            previewImporter.filterMode = FilterMode.Point;
            previewImporter.mipmapEnabled = false;
            previewImporter.textureCompression = TextureImporterCompression.Uncompressed;
            previewImporter.alphaIsTransparency = true;
            previewImporter.SaveAndReimport();
        }

    }

    private static void ConfigureAmiyaTexture(string assetPath, bool hasAlpha, bool readable = false)
    {
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null) return;

        importer.textureType = TextureImporterType.Default;
        importer.filterMode = FilterMode.Point;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.alphaIsTransparency = hasAlpha;
        importer.isReadable = readable;
        importer.SaveAndReimport();
    }

    private static void GenerateTransparentBackSheet()
    {
        TextureImporter sourceImporter = AssetImporter.GetAtPath(BackSheetSourcePath) as TextureImporter;
        if (sourceImporter == null) return;

        if (!sourceImporter.isReadable)
        {
            sourceImporter.isReadable = true;
            sourceImporter.SaveAndReimport();
        }

        Texture2D source = AssetDatabase.LoadAssetAtPath<Texture2D>(BackSheetSourcePath);
        if (source == null) return;

        Color32[] pixels = source.GetPixels32();
        int width = source.width;
        int height = source.height;
        bool[] background = new bool[pixels.Length];
        bool[] cleared = new bool[pixels.Length];
        Queue<int> pending = new Queue<int>();

        for (int i = 0; i < pixels.Length; i++)
            background[i] = IsBackSheetBackground(pixels[i]);

        for (int y = 0; y < height; y++)
        {
            EnqueueBackground(y * width, background, cleared, pending);
            EnqueueBackground(y * width + width - 1, background, cleared, pending);
        }
        for (int x = 0; x < width; x++)
        {
            EnqueueBackground(x, background, cleared, pending);
            EnqueueBackground((height - 1) * width + x, background, cleared, pending);
        }

        while (pending.Count > 0)
        {
            int index = pending.Dequeue();
            int x = index % width;
            int y = index / width;
            pixels[index] = new Color32(0, 0, 0, 0);

            EnqueueBackground(x > 0 ? index - 1 : -1, background, cleared, pending);
            EnqueueBackground(x < width - 1 ? index + 1 : -1, background, cleared, pending);
            EnqueueBackground(y > 0 ? index - width : -1, background, cleared, pending);
            EnqueueBackground(y < height - 1 ? index + width : -1, background, cleared, pending);
        }

        ClearBackSheetWatermark(pixels, width, height);

        Texture2D transparent = new Texture2D(width, height, TextureFormat.RGBA32, false, false);
        transparent.SetPixels32(pixels);
        transparent.Apply(false, false);
        string absolutePath = Path.Combine(Application.dataPath, BackSheetPath.Substring("Assets/".Length));
        File.WriteAllBytes(absolutePath, transparent.EncodeToPNG());
        Object.DestroyImmediate(transparent);
        AssetDatabase.ImportAsset(BackSheetPath, ImportAssetOptions.ForceUpdate);
    }

    private static void ClearBackSheetWatermark(Color32[] pixels, int width, int height)
    {
        if (pixels == null || pixels.Length != width * height) return;

        int minX = Mathf.Clamp(Mathf.CeilToInt(width * BackSheetWatermarkMinXNormalized), 0, width);
        int minTopY = Mathf.Clamp(Mathf.FloorToInt(height * BackSheetWatermarkMinTopYNormalized), 0, height);
        int maxTextureY = Mathf.Clamp(height - 1 - minTopY, -1, height - 1);
        if (minX >= width || maxTextureY < 0) return;

        for (int y = 0; y <= maxTextureY; y++)
        {
            int rowStart = y * width;
            for (int x = minX; x < width; x++)
                pixels[rowStart + x] = new Color32(0, 0, 0, 0);
        }
    }

    private static void EnqueueBackground(int index, bool[] background, bool[] cleared, Queue<int> pending)
    {
        if (index < 0 || index >= background.Length || !background[index] || cleared[index]) return;
        cleared[index] = true;
        pending.Enqueue(index);
    }

    private static bool IsBackSheetBackground(Color32 pixel)
    {
        int brightest = Mathf.Max(pixel.r, Mathf.Max(pixel.g, pixel.b));
        int darkest = Mathf.Min(pixel.r, Mathf.Min(pixel.g, pixel.b));
        return pixel.a > 0 && darkest >= 212 && brightest - darkest <= 45;
    }

    private static void ConfigureBuildSettings()
    {
        string[] required =
        {
            "Assets/Scenes/GameMenu.unity",
            AdventureScenePath,
            "Assets/Scenes/BattleScene.unity"
        };

        var scenes = new List<EditorBuildSettingsScene>(required.Length);
        foreach (string path in required)
            scenes.Add(new EditorBuildSettingsScene(path, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    private static bool ConfigureAdventureScene()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            Debug.LogWarning("HD adventure setup canceled because the current scene was not saved.");
            return false;
        }

        Scene scene = EditorSceneManager.OpenScene(AdventureScenePath, OpenSceneMode.Single);
        Camera[] cameras = Object.FindObjectsOfType<Camera>(true);
        Camera selected = null;
        foreach (Camera camera in cameras)
        {
            if (camera == null) continue;
            if (camera.gameObject.name == "Main Camera")
            {
                selected = camera;
                break;
            }
            if (selected == null && camera.enabled && camera.CompareTag("MainCamera")) selected = camera;
        }

        if (selected == null && cameras.Length > 0) selected = cameras[0];

        if (selected != null)
        {
            selected.gameObject.name = "Main Camera";
            selected.tag = "MainCamera";
            RestoreOriginalCamera(selected);
            foreach (Camera camera in cameras)
            {
                if (camera == null) continue;
                bool keep = camera == selected;
                camera.enabled = keep;
                AudioListener listener = camera.GetComponent<AudioListener>();
                if (listener != null) listener.enabled = keep;
            }
        }

        // These objects/components belong to the retired world-map/city flow.
        // Remove them while editing the scene so a fresh play session does not
        // depend on runtime cleanup to avoid duplicate input or UI.
        GameObject legacyMenu = GameObject.Find("Campaign Menu Runtime");
        if (legacyMenu != null) Object.DestroyImmediate(legacyMenu);

        DestroyLegacyComponents<WorldMapController>();
        DestroyLegacyComponents<CityController>();
        DestroyLegacyComponents<GameMenuController>();
        EnsureAmiyaInScene(scene, selected);
        AddMissingBuildingColliders(scene, out _);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        return true;
    }

    private static int AddMissingBuildingColliders(Scene scene, out List<string> addedNames)
    {
        addedNames = new List<string>();
        if (!scene.IsValid()) return 0;

        List<GameObject> candidates = new List<GameObject>();
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
            foreach (Transform item in transforms)
            {
                if (item == null || !item.gameObject.scene.IsValid()) continue;
                GameObject candidate = item.gameObject;
                if (!IsBuildingCandidate(candidate)) continue;
                if (PrefabUtility.IsPartOfPrefabAsset(candidate)) continue;
                if (!PrefabUtility.IsAnyPrefabInstanceRoot(candidate) && candidate != root &&
                    PrefabUtility.IsPartOfPrefabInstance(candidate)) continue;
                if (candidate.GetComponentsInChildren<Collider>(true).Length > 0) continue;
                if (!TryGetWorldBounds(candidate, out Bounds bounds)) continue;
                if (bounds.size.x < 0.5f || bounds.size.z < 0.5f || bounds.size.y < 0.25f) continue;
                candidates.Add(candidate);
            }
        }

        // A parent and one of its nested prefab roots can both satisfy the
        // name filter. Keep the highest-level matching object so a building
        // receives one stable collider instead of overlapping duplicates.
        candidates.Sort((left, right) => GetHierarchyDepth(left).CompareTo(GetHierarchyDepth(right)));
        HashSet<Transform> covered = new HashSet<Transform>();
        int addedCount = 0;
        foreach (GameObject candidate in candidates)
        {
            if (candidate == null || covered.Contains(candidate.transform)) continue;
            if (HasCoveredAncestor(candidate.transform, covered)) continue;
            if (candidate.GetComponentsInChildren<Collider>(true).Length > 0) continue;
            if (!TryGetWorldBounds(candidate, out Bounds bounds)) continue;

            GameObject collisionObject = new GameObject("Auto BoxCollider");
            Undo.RegisterCreatedObjectUndo(collisionObject, "Add building BoxCollider");
            collisionObject.transform.SetParent(candidate.transform, false);
            collisionObject.transform.localPosition = Vector3.zero;
            collisionObject.transform.localRotation = Quaternion.identity;
            collisionObject.transform.localScale = Vector3.one;
            collisionObject.layer = candidate.layer;

            BoxCollider box = collisionObject.AddComponent<BoxCollider>();
            Bounds localBounds = TransformWorldBoundsToLocal(candidate.transform, bounds);
            box.center = localBounds.center;
            box.size = localBounds.size;
            box.isTrigger = false;

            EditorUtility.SetDirty(collisionObject);
            EditorUtility.SetDirty(candidate);
            covered.Add(candidate.transform);
            addedNames.Add(candidate.name);
            addedCount++;
        }

        return addedCount;
    }

    private static bool IsBuildingCandidate(GameObject candidate)
    {
        string name = candidate.name;
        if (string.IsNullOrEmpty(name)) return false;

        bool largeCityBuilding = name.StartsWith("EnvBdgMD_City_B_Outdoor_L_") ||
                                 name.StartsWith("EnvBdgMD_City_B_Outdoor_Pub") ||
                                 name.StartsWith("EnvBdgMD_City_B_Outdoor_Theater") ||
                                 name.StartsWith("EnvMdrMD_Mansion_") ||
                                 name.StartsWith("EnvMdrMD_DepartmentStore_") ||
                                 name.StartsWith("ObjMD_Daiseido_A") ||
                                 name.StartsWith("SM_Wall") ||
                                 name.StartsWith("SM_wall");
        if (!largeCityBuilding) return false;

        // These are architectural decorations or thin props. A full bounds
        // collider would create invisible walls in front of the actual rooms.
        return !name.Contains("Awning") &&
               !name.Contains("Window") &&
               !name.Contains("Signbord") &&
               !name.Contains("Chimney") &&
               !name.Contains("Balcony");
    }

    private static bool TryGetWorldBounds(GameObject target, out Bounds bounds)
    {
        bounds = default;
        Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
        bool found = false;
        foreach (Renderer renderer in renderers)
        {
            if (renderer == null || renderer is ParticleSystemRenderer) continue;
            if (!renderer.enabled && renderer.gameObject.activeInHierarchy) continue;
            if (!found)
            {
                bounds = renderer.bounds;
                found = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }
        return found;
    }

    private static Bounds TransformWorldBoundsToLocal(Transform target, Bounds worldBounds)
    {
        Vector3 center = target.InverseTransformPoint(worldBounds.center);
        Vector3 min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        Vector3 max = new Vector3(float.MinValue, float.MinValue, float.MinValue);
        Vector3 extents = worldBounds.extents;
        for (int x = -1; x <= 1; x += 2)
        {
            for (int y = -1; y <= 1; y += 2)
            {
                for (int z = -1; z <= 1; z += 2)
                {
                    Vector3 point = target.InverseTransformPoint(worldBounds.center +
                        Vector3.Scale(extents, new Vector3(x, y, z)));
                    min = Vector3.Min(min, point);
                    max = Vector3.Max(max, point);
                }
            }
        }

        Bounds localBounds = new Bounds(center, Vector3.zero);
        localBounds.SetMinMax(min, max);
        return localBounds;
    }

    private static int GetHierarchyDepth(GameObject target)
    {
        int depth = 0;
        Transform current = target != null ? target.transform.parent : null;
        while (current != null)
        {
            depth++;
            current = current.parent;
        }
        return depth;
    }

    private static bool HasCoveredAncestor(Transform candidate, HashSet<Transform> covered)
    {
        Transform current = candidate.parent;
        while (current != null)
        {
            if (covered.Contains(current)) return true;
            current = current.parent;
        }
        return false;
    }

    private static void EnsureAmiyaInScene(Scene scene, Camera gameplayCamera)
    {
        AmiyaCharacter[] characters = Object.FindObjectsOfType<AmiyaCharacter>(true);
        AmiyaCharacter character = characters.Length > 0 ? characters[0] : null;
        for (int i = 1; i < characters.Length; i++)
        {
            if (characters[i] != null) Object.DestroyImmediate(characters[i].gameObject);
        }

        bool created = character == null;
        if (created)
        {
            GameObject avatar = new GameObject("Amiya");
            SceneManager.MoveGameObjectToScene(avatar, scene);
            character = avatar.AddComponent<AmiyaCharacter>();
        }

        GameObject avatarObject = character.gameObject;
        avatarObject.name = "Amiya";
        avatarObject.SetActive(true);
        SpriteRenderer renderer = avatarObject.GetComponent<SpriteRenderer>();
        if (renderer == null) renderer = avatarObject.AddComponent<SpriteRenderer>();
        renderer.sortingOrder = 50;
        Sprite playerSprite = LoadPlayerSprite();
        if (playerSprite != null) renderer.sprite = playerSprite;

        CharacterController controller = avatarObject.GetComponent<CharacterController>();
        if (controller == null) controller = avatarObject.AddComponent<CharacterController>();
        controller.height = 1.8f;
        controller.radius = 0.32f;
        controller.center = Vector3.up * 0.9f;
        controller.skinWidth = 0.03f;
        controller.stepOffset = 0.25f;

        character.gameplayCamera = gameplayCamera;
        character.moveSpeed = 4f;
        character.sizeScale = 1f;
        character.sheetPixelsPerUnit = 160f;
        character.backSheetPixelsPerUnit = 395f;
        character.authoredCharacterHeight = 1.95f;
        character.normalizeAuthoredFrameSize = true;
        character.downRightSheet = AssetDatabase.LoadAssetAtPath<Texture2D>(DownRightSheetPath);
        character.downLeftSheet = AssetDatabase.LoadAssetAtPath<Texture2D>(DownLeftSheetPath);
        character.leftSheet = AssetDatabase.LoadAssetAtPath<Texture2D>(LeftSheetPath);
        character.rightSheet = AssetDatabase.LoadAssetAtPath<Texture2D>(RightSheetPath);
        character.backSheet = AssetDatabase.LoadAssetAtPath<Texture2D>(BackSheetPath);

        HDAdventureConfig config = AssetDatabase.LoadAssetAtPath<HDAdventureConfig>(ConfigPath);
        if (created || !IsUsablePosition(avatarObject.transform.position))
            avatarObject.transform.position = ResolvePlayerStartPosition(gameplayCamera, config);
        if (config != null)
        {
            config.playerStartPosition = avatarObject.transform.position;
            EditorUtility.SetDirty(config);
        }

        EditorUtility.SetDirty(character);
        EditorUtility.SetDirty(avatarObject);
    }

    private static void RestoreOriginalCamera(Camera camera)
    {
        if (camera == null) return;
        camera.transform.position = OriginalCameraPosition;
        camera.transform.rotation = Quaternion.Euler(OriginalCameraEulerAngles);
        camera.orthographic = false;
        camera.fieldOfView = 60f;

        // The source HD scene includes a showcase Timeline which pans this
        // camera. Adventure movement owns the camera, so disable the source
        // animation in the saved scene instead of relying on runtime cleanup.
        Animator animator = camera.GetComponent<Animator>();
        if (animator != null)
        {
            animator.enabled = false;
            EditorUtility.SetDirty(animator);
        }

        PlayableDirector director = camera.GetComponent<PlayableDirector>();
        if (director != null)
        {
            director.Stop();
            director.enabled = false;
            EditorUtility.SetDirty(director);
        }
        EditorUtility.SetDirty(camera);
    }

    private static Sprite LoadPlayerSprite()
    {
        TextureImporter importer = AssetImporter.GetAtPath(PlayerFramePath) as TextureImporter;
        if (importer != null && importer.textureType != TextureImporterType.Sprite)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePivot = new Vector2(0.5f, 0.08f);
            importer.SaveAndReimport();
        }

        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(PlayerFramePath);
        if (sprite != null) return sprite;

        TextureImporter legacyImporter = AssetImporter.GetAtPath(LegacyPlayerFramePath) as TextureImporter;
        if (legacyImporter != null && legacyImporter.textureType != TextureImporterType.Sprite)
        {
            legacyImporter.textureType = TextureImporterType.Sprite;
            legacyImporter.spriteImportMode = SpriteImportMode.Single;
            legacyImporter.spritePivot = new Vector2(0.5f, 0.08f);
            legacyImporter.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(LegacyPlayerFramePath);
    }

    private static Vector3 ResolvePlayerStartPosition(Camera camera, HDAdventureConfig config)
    {
        if (config != null && IsUsablePosition(config.playerStartPosition))
            return config.playerStartPosition;

        if (camera != null)
        {
            Ray ray = camera.ViewportPointToRay(new Vector3(0.5f, 0.38f, 0f));
            if (Physics.Raycast(ray, out RaycastHit hit, 500f, ~0, QueryTriggerInteraction.Ignore))
                return hit.point + Vector3.up * 0.05f;

            Vector3 fallback = camera.transform.position + camera.transform.forward * 8f;
            fallback.y = Mathf.Max(0f, fallback.y);
            return fallback;
        }

        return new Vector3(38f, 16f, 42f);
    }

    private static bool IsUsablePosition(Vector3 position)
    {
        return !float.IsNaN(position.x) && !float.IsNaN(position.y) && !float.IsNaN(position.z) &&
               !float.IsInfinity(position.x) && !float.IsInfinity(position.y) && !float.IsInfinity(position.z) &&
               position.sqrMagnitude > 0.01f;
    }

    private static void DestroyLegacyComponents<T>() where T : Component
    {
        T[] components = Object.FindObjectsOfType<T>(true);
        foreach (T component in components)
        {
            if (component != null) Object.DestroyImmediate(component);
        }
    }
}
