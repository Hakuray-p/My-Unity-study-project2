using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;


public class TargetManager : MonoBehaviour
{
    private bool _isSelecting; // 正在选择目标中
    public bool IsSelecting => _isSelecting;
    private List<CardController> _targetCards = new();
    private UnityAction<TargetPack> _finishCallBack;
    public GameObject selectIcon;
    public SelectContainer selectContainer;

    private Transform _origionParent;
    private bool _finishShowBack = true;

    // 选择状态
    private int selectNum;


    private TargetPack _targetPack;
    private bool _callbackInvoked;

    // 筛选条件 
    //private Func<CardController, bool> filterCondition;
    //
    private void Update()
    {
        if (_isSelecting)
        {
            if (Input.GetMouseButtonDown(0))
            {
                // 射线检测卡牌
                CheckCard();
            }

            Vector3 mousePos = Input.mousePosition;
            mousePos.z = 10f; // 距离摄像机的距离
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(mousePos);
            // 攻击瞄准图标跟随
            if (selectIcon != null)
            {
                selectIcon.transform.position = worldPos;
            }
        }
    }

    //
    private void SelectStart()
    {
        _isSelecting = true;
        if (selectIcon != null) selectIcon.SetActive(true);
        _targetPack = new TargetPack();
    }

    //
    private void SelectFinish(bool showBack = true)
    {
        _isSelecting = false;
        if (selectIcon != null) selectIcon.SetActive(false);
        foreach (var card in _targetCards)
        {
            card.cardDisplay.ShowSpecial(false);
            if (_origionParent != null)
            {
                card.transform.SetParent(_origionParent);
                card.transform.localPosition = Vector3.zero;
                card.cardDisplay.ShowBack(showBack);
            }
        }

        _origionParent = null;
    }

    //
    public void CheckCard()
    {
        if (_callbackInvoked) return;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit))
        {
            CardController card = hit.collider.GetComponent<CardController>();
            if (_targetCards.Contains(card) && !_targetPack.cards.Contains(card))
            {
                _targetPack.cards.Add(card);
                card.cardDisplay.ShowSpecial(false);
            }

            if (_targetPack.cards.Count >= selectNum)
            {
                // 选择结束
                SelectFinish(_finishShowBack);
                _callbackInvoked = true;
                _finishCallBack.Invoke(_targetPack);
            }
        }
    }

    
    /// <summary>
    /// 从卡组或墓地里选择
    /// </summary>
    public void SelectFormList(PlayerController effectPlayer
        , List<CardController> targetCards,
        int num, UnityAction<TargetPack> finishCallBack, bool showBack = true)
    {
        if (targetCards == null) targetCards = new List<CardController>();
        if (finishCallBack == null) return;
        _finishCallBack = finishCallBack;
        _callbackInvoked = false;
        _targetCards = targetCards;
        _origionParent = null;
        _finishShowBack = showBack;
        if (effectPlayer is AIController ai)
        {
            DOVirtual.DelayedCall(0.5f, () =>
            {
                if (_callbackInvoked) return;
                SelectFinish(showBack);
                _callbackInvoked = true;
                finishCallBack.Invoke(ai.RandomSelect(targetCards, num));
            });
        }
        else
        {
            _targetPack = new TargetPack();
            selectNum = Math.Min(num, targetCards.Count);
            _origionParent = targetCards.Count > 0 ? targetCards[0].transform.parent : null;
            selectContainer.ShowSelect(targetCards);
            if (selectNum > 0)
            {
                SelectStart();
            }
            else
            {
                SelectFinish(showBack);
                _callbackInvoked = true;
                _finishCallBack.Invoke(_targetPack);
            }
        }
    }

    /// <summary>
    /// 从场上选择目标
    /// </summary>
    /// <param name="effectPlayer">效果发动玩家</param>
    /// <param name="targetCards">可选的卡牌</param>
    /// <param name="num">选择的数量</param>
    /// <param name="finishCallBack">选择完成后的回调</param>
    public void StartSelectFieldCards(PlayerController effectPlayer
        , List<CardController> targetCards,
        int num, UnityAction<TargetPack> finishCallBack)
    {
        if (targetCards == null) targetCards = new List<CardController>();
        if (finishCallBack == null) return;
        _finishCallBack = finishCallBack;
        _callbackInvoked = false;
        _targetCards = targetCards;
        if (effectPlayer is AIController ai)
        {
            DOVirtual.DelayedCall(0.5f, () =>
            {
                SelectFinish();
                if (_callbackInvoked) return;
                _callbackInvoked = true;
                finishCallBack.Invoke(ai.RandomSelect(targetCards, num));
            });
        }
        else
        {
            _targetPack = new TargetPack();
            selectNum = Math.Min(num, targetCards.Count);
            if (selectNum > 0)
            {
                SelectStart();
            }
            else
            {
                Debug.Log("目标少于选择数量");
                SelectFinish();
                _callbackInvoked = true;
                _finishCallBack.Invoke(_targetPack);
            }
        }
    }
}

[System.Serializable]
public class TargetPack
{
    public List<CardController> cards = new();

    public TargetPack()
    {
        cards = new List<CardController>();
    }
}
