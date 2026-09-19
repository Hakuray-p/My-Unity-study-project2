using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

// 选牌界面，把卡牌平铺在相机正前方并把相机移过去
public class SelectContainer : MonoBehaviour
{
    // 最大行数
    private int maxRow = 5;
    private int maxCol = 6; // 最大列数
    private float gap = 3f; // 卡牌间距
    private float listDistance = 13f; // 选牌网格到相机的距离
    private float revealDistance = 6f; // 展示单张卡时到相机的距离
    private float revealTime = 1f; // 展示单张卡的停留时长
    private float promptGap = 0.5f; // 说明文字和目标之间的空隙
    private float promptMargin = 0.3f; // 说明文字离画面边缘的最小留白
    private TextMeshPro promptText; // 网格上方的说明文字

    public Vector3 cameraPos; // 相机停靠位置
    public TMP_FontAsset promptFont; // 说明文字用的字体
    public float listScale = 1.35f; // 列表里卡牌的缩放

    // 把待选的卡平铺成网格并把相机移过去
    public void ShowSelect(List<CardController> cards, string prompt = "")
    {
        Camera.main.transform.DOMove(cameraPos, 0.3f);
        FaceCamera(cameraPos, listDistance);

        int rows = Mathf.Min(maxRow, Mathf.CeilToInt(cards.Count / (float)maxCol));
        int cols = Mathf.Min(maxCol, cards.Count);
        // 说明写在网格顶边上方，卡牌高 2，中心到顶边是 1
        ShowPrompt(prompt, (rows - 1) * 0.5f * gap + 1f, listDistance);
        for (int i = 0; i < cards.Count; i++)
        {
            int row = i / maxCol;
            int col = i % maxCol;
            if (row >= maxRow) break; // 超过最大行数则停止摆放
            var targetPos = new Vector3((col - (cols - 1) * 0.5f) * gap, ((rows - 1) * 0.5f - row) * gap, 0f);
            cards[i].transform.SetParent(this.transform);
            cards[i].transform.DOLocalMove(targetPos, 0.6f);
            cards[i].cardDisplay.SetBaseScale(listScale);
            cards[i].transform.DOLocalRotate(Vector3.zero, 0.6f);
            cards[i].cardDisplay.ShowBack(false);
        }
    }

    // 把一张检索到的卡正面亮在相机前，展示完再交给调用方
    public void ShowReveal(CardController card, string prompt, UnityAction onFinish)
    {
        Camera.main.transform.DOMove(cameraPos, 0.3f);
        FaceCamera(cameraPos, revealDistance);
        // 说明写在卡牌顶边上方
        ShowPrompt(prompt, 1f, revealDistance);
        card.transform.SetParent(this.transform);
        card.cardDisplay.ShowBack(false);
        card.transform.DOLocalMove(Vector3.zero, 0.5f);
        card.transform.DOLocalRotate(Vector3.zero, 0.5f);
        DOVirtual.DelayedCall(0.5f + revealTime, () => onFinish());
    }

    // 在目标上方写一句说明，顶出画面就按文字高度压回来，没传文案就收起来
    private void ShowPrompt(string prompt, float anchorY, float distance)
    {
        if (promptText == null) promptText = CreatePrompt();
        bool show = !string.IsNullOrEmpty(prompt);
        promptText.gameObject.SetActive(show);
        if (!show) return;

        promptText.text = prompt;
        // 行数多的时候说明会和网格一起顶出画面，按文字自己的高度整句压回可见范围
        float halfHeight = promptText.preferredHeight * 0.5f;
        float visibleLimit = distance * Mathf.Tan(Camera.main.fieldOfView * 0.5f * Mathf.Deg2Rad) - halfHeight - promptMargin;
        float promptY = Mathf.Min(anchorY + promptGap + halfHeight, visibleLimit);
        promptText.transform.localPosition = new Vector3(0f, promptY, 0f);
    }

    // 建一个正对相机的说明文字
    private TextMeshPro CreatePrompt()
    {
        var promptObject = new GameObject("PromptText", typeof(TextMeshPro));
        promptObject.transform.SetParent(this.transform, false);
        TextMeshPro text = promptObject.GetComponent<TextMeshPro>();
        if (promptFont != null) text.font = promptFont;
        text.fontSize = 2.4f;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;
        text.enableWordWrapping = false;
        text.rectTransform.sizeDelta = new Vector2(30f, 3f);
        text.GetComponent<MeshRenderer>().sortingOrder = 200;
        return text;
    }

    // 把网格摆到相机正前方并朝向相机，让卡牌平对镜头
    private void FaceCamera(Vector3 position, float distance)
    {
        transform.SetPositionAndRotation(position + Camera.main.transform.forward * distance, Camera.main.transform.rotation);
    }
}
