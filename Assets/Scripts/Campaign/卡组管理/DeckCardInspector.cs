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

    // 找齐场景里的 3D 预览和两段文字
    public DeckCardInspector()
    {
        previewController = Object.FindObjectOfType<Card3DPreviewController>();
        nameText = SceneTool.Find<TMP_Text>("DeckCardNameText");
        effectText = SceneTool.Find<TMP_Text>("DeckCardEffectText");

        Camera previewCamera = SceneTool.Find<Camera>("DeckPreviewCamera");
        SceneTool.Find<RawImage>("DeckPreviewRawImage").texture = previewCamera.targetTexture;
        previewController.SetPreviewCamera(previewCamera);
    }

    // 在左边显示这张卡，两段文字跟着换
    public void Show(CardData card)
    {
        previewController.SetCard(card);
        nameText.text = card.name;
        effectText.text = Regex.Unescape(card.effectDescription);
    }
}
