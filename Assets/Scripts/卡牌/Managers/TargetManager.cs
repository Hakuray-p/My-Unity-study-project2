using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;


// 效果选目标时的交互，支持玩家点选和 AI 随机选
public class TargetManager : MonoBehaviour
{
    private bool _isSelecting; // 正在选择目标中
    public bool IsSelecting => _isSelecting || _isViewing;
    private bool _isViewing; // 正在查看墓地这类只看的列表
    public bool IsViewing => _isViewing;
    public int ViewClosedFrame { get; private set; } = -1; // 本帧关闭查看时不再响应暂停或拖牌
    private int _viewFrame; // 打开查看的那一帧
    private List<CardController> _targetCards = new(); // 候选目标
    private UnityAction<TargetPack> _finishCallBack; // 选完后的回调
    public GameObject selectIcon; // 选择时跟随鼠标的图标
    public SelectContainer selectContainer; // 选牌界面

    private Transform _origionParent; // 选择前卡牌所在的父节点
    private bool _finishShowBack = true; // 选完后要不要把牌面翻回去

    // 选择状态
    private int selectNum; // 需要选择的数量


    private TargetPack _targetPack; // 已选中的目标
    private bool _callbackInvoked; // 回调是否已经触发过

    // 筛选条件 
    //private Func<CardController, bool> filterCondition;
    private void Update()
    {
        if (Time.timeScale == 0f) return;
        if (_isViewing)
        {
            // 打开查看的那一下点击不算收起
            if (Time.frameCount > _viewFrame &&
                (Input.GetMouseButtonDown(0) && !GM.Ins.BM.Tutorial.BlocksPointer || Input.GetKeyDown(KeyCode.Escape)))
            {
                CloseViewList();
            }
            return;
        }

        if (_isSelecting)
        {
            if (Input.GetMouseButtonDown(0) && !GM.Ins.BM.Tutorial.BlocksPointer)
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

    // 开始选目标，亮出瞄准图标
    private void SelectStart()
    {
        _isSelecting = true;
        if (selectIcon != null) selectIcon.SetActive(true);
        _targetPack = new TargetPack();
    }

    // 结束选目标，把候选卡放回原位
    private void SelectFinish(bool showBack = true)
    {
        _isSelecting = false;
        if (selectIcon != null) selectIcon.SetActive(false);
        foreach (var card in _targetCards)
        {
            card.cardDisplay.ShowSpecial(false);
            card.cardDisplay.SetBaseScale(1f);
            if (_origionParent != null)
            {
                card.transform.SetParent(_origionParent);
                card.transform.localPosition = Vector3.zero;
                card.cardDisplay.ShowBack(showBack);
            }
        }

        _origionParent = null;
    }

    // 打开墓地这类只看的列表，点一下屏幕或按 Esc 收起
    public void StartViewList(List<CardController> cards, string prompt)
    {
        _targetCards = cards != null ? cards : new List<CardController>();
        _origionParent = _targetCards.Count > 0 ? _targetCards[0].transform.parent : null;
        _viewFrame = Time.frameCount;
        _isViewing = true;
        selectContainer.ShowSelect(_targetCards, prompt);
    }

    // 收起查看列表，把卡放回原位并让相机回战斗视角
    public void CloseViewList()
    {
        _isViewing = false;
        ViewClosedFrame = Time.frameCount;
        SelectFinish(false);
        GM.Ins.BM.EM.ResetCamera();
        GM.Ins.BM.NoteAction();
        GM.Ins.BM.Tutorial.GraveClosed();
    }

    // 射线选中一张候选卡，选够就收尾
    public void CheckCard()
    {
        if (_callbackInvoked) return;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit))
        {
            CardController card = hit.collider.GetComponent<CardController>();
            if (_targetCards.Contains(card) && !_targetPack.cards.Contains(card) &&
                GM.Ins.BM.Tutorial.Allows(TutorialAction.SelectTarget, card))
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
                GM.Ins.BM.Tutorial.ActionAccepted(TutorialAction.SelectTarget);
            }
        }
    }


    /// <summary>
    /// 从卡组或墓地里选择
    /// </summary>
    public void SelectFormList(PlayerController effectPlayer
        , List<CardController> targetCards,
        int num, UnityAction<TargetPack> finishCallBack, bool showBack = true, string prompt = "")
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
            selectContainer.ShowSelect(targetCards, prompt);
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
// 一次目标选择的结果
public class TargetPack
{
    public List<CardController> cards = new(); // 选中的卡牌

    // 构造时初始化选中列表
    public TargetPack()
    {
        cards = new List<CardController>();
    }
}
