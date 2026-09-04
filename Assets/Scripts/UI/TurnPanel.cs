using DG.Tweening;
using TMPro;
using UnityEngine;


    public class TurnPanel: MonoBehaviour
    {
        public TMP_Text turnText;

        public void ShowTurnChange(string text)
        {
            turnText.text = text;
            gameObject.SetActive(true);
            turnText.rectTransform
                .DOLocalMove(Vector3.right*100f, 1f)
                .From()
                .OnComplete(()=>gameObject.SetActive(false));
        }
    }
