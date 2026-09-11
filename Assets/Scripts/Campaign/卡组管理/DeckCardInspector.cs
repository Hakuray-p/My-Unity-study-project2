using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 卡组管理界面的卡牌详情，负责左边的 3D 预览和左下角的卡名、效果文字。
public sealed class DeckCardInspector
{
    private readonly Card3DPreviewController previewController; // 3D 预览控制器
    private readonly TMP_Text nameText; // 左下角的卡名
    private readonly TMP_Text effectText; // 左下角的效果描述

    // 找齐场景里的 3D 预览和两段文字，把预览相机的渲染贴图接到预览界面上
    public DeckCardInspector()
    {
        previewController = Object.FindObjectOfType<Card3DPreviewController>();
        nameText = FindText("DeckCardNameText");
        effectText = FindText("DeckCardEffectText");

        Camera previewCamera = FindCamera("DeckPreviewCamera");
        FindRawImage("DeckPreviewRawImage").texture = previewCamera.targetTexture;
        previewController.SetPreviewCamera(previewCamera);
    }

    // 在左边显示这张卡，两段文字跟着换
    public void Show(CardData card)
    {
        previewController.SetCard(card);
        nameText.text = card.name;
        effectText.text = Regex.Unescape(card.effectDescription);
    }

    // 按名字片段找场景里的文字
    private static TMP_Text FindText(string namePart)
    {
        foreach (TMP_Text text in Object.FindObjectsOfType<TMP_Text>(true))
            if (text.gameObject.name.Contains(namePart)) return text;
        return null;
    }

    // 按名字片段找场景里的相机
    private static Camera FindCamera(string namePart)
    {
        foreach (Camera camera in Object.FindObjectsOfType<Camera>(true))
            if (camera.gameObject.name.Contains(namePart)) return camera;
        return null;
    }

    // 按名字片段找场景里的 RawImage
    private static RawImage FindRawImage(string namePart)
    {
        foreach (RawImage image in Object.FindObjectsOfType<RawImage>(true))
            if (image.gameObject.name.Contains(namePart)) return image;
        return null;
    }
}
