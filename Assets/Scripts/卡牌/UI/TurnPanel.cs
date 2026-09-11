using DG.Tweening;
using TMPro;
using UnityEngine;


// 回合切换提示面板，飞入动画播完后自己隐藏
public class TurnPanel : MonoBehaviour
{
    public TMP_Text turnText; // 回合提示文字

    // 飞入显示回合提示，播完自动隐藏
    public void ShowTurnChange(string text)
    {
        turnText.text = text;
        gameObject.SetActive(true);
        turnText.rectTransform
            .DOLocalMove(Vector3.right * 100f, 1f)
            .From()
            .OnComplete(() => gameObject.SetActive(false));
    }
}
