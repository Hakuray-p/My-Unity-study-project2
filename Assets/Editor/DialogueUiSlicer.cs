#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class DialogueUiSlicer
{
    private const string DialoguePath = "Assets/素材/Ark Image/UI/UI/对话框ui.png";
    private const string TrianglePath = "Assets/素材/Ark Image/UI/UI/对话框倒三角ui.png";
    private const string AnimatedTrianglePath = "Assets/素材/Ark Image/UI/UI/对话框倒三角8帧动画图.png";

    [MenuItem("ArkCard/UI/切割对话框素材")]
    public static void Slice()
    {
        SliceDialogue();
        SetSingleSprite(TrianglePath);
        SetSingleSprite(AnimatedTrianglePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("对话框素材已切割：对话框ui_0/1/2，右下角倒三角素材已设为 Sprite。");
    }

    private static void SliceDialogue()
    {
        TextureImporter importer = AssetImporter.GetAtPath(DialoguePath) as TextureImporter;
        if (importer == null) { Debug.LogWarning("找不到 " + DialoguePath); return; }
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.spritePixelsPerUnit = 100f;
        AssetDatabase.ImportAsset(DialoguePath, ImportAssetOptions.ForceUpdate);
    }

    private static void SetSingleSprite(string path)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) return;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.spritePixelsPerUnit = 100f;
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
    }
}
#endif

