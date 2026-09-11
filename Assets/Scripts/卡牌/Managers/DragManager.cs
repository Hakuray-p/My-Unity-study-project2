using UnityEngine;

// 鼠标拖拽：手牌拖到场上是召唤 / 施法，场上卡牌拖到目标上是攻击
public class DragManager : MonoBehaviour
{
    private CardController draggingCard; // 正在拖拽的卡牌
    private Vector3 offset; // 卡牌和鼠标之间的偏移
    private bool isDragging = false; // 是否正在拖拽

    public GameObject attackAimIcon; // 攻击矄准图标

    public int mainPlayerId; // 主玩家编号


    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            MouseDown();
        }
        if (isDragging && draggingCard != null)
        {
            DraggingCard();
            if (Input.GetMouseButtonUp(0))
            {
                MouseUp();
            }
        }
    }

    // 按下时判断抓到的是手牌还是可攻击的场上卡牌
    void MouseDown()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = 10f; // 距离摄像机的距离
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(mousePos);
        if (Physics.Raycast(ray, out hit))
        {
            CardController card = hit.collider.GetComponent<CardController>();
            if (card != null && card.player.isMainPlayer)
            {
                if (card.cardState == CardState.Hand)
                {
                    // 手牌拖拽
                    draggingCard = card;
                    offset = card.transform.position - worldPos;
                    isDragging = true;
                }
                else if (card.cardState == CardState.Field && card.ableAttack)
                {
                    // 场上卡牌攻击
                    draggingCard = card;
                    isDragging = true;
                    attackAimIcon.SetActive(true);
                }
            }
        }
    }

    void DraggingCard()
    {
        if (GM.Ins.BM.EM.IsProcessingEffect) return;

        Vector3 mousePos = Input.mousePosition;
        mousePos.z = 10f; // 距离摄像机的距离
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(mousePos);

        if (draggingCard.cardState == CardState.Hand)
        {
            // 拖拽手牌
            draggingCard.transform.position = worldPos + offset;
        }
        else if (draggingCard.cardState == CardState.Field && draggingCard.ableAttack)
        {
            // 攻击瞄准图标跟随
            if (attackAimIcon != null)
            {
                attackAimIcon.transform.position = worldPos;
            }
        }
    }

    // 松开时判定落点：手牌落场做召唤 / 施法，场上卡牌落到目标上做攻击
    void MouseUp()
    {
        CardController releasedCard = draggingCard;
        draggingCard = null;
        isDragging = false;
        if (attackAimIcon != null) attackAimIcon.SetActive(false);
        if (releasedCard == null) return;
        if (!GM.Ins.BM.GetMainPlayer.isInTurn)
        {
            GM.Ins.BM.GetMainPlayer.hands.RefreshCards();
            return;
        }
        if (releasedCard.cardState == CardState.Hand) // 射线检测区域
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit[] hits = Physics.RaycastAll(ray, 100f);
            foreach (var hit in hits)
            {
                //Debug.Log(hit.collider.name);
                if (hit.collider == null || hit.collider.gameObject == gameObject) continue;
                FieldController target = hit.collider.GetComponent<FieldController>();
                if (target != null && target.player == releasedCard.player)
                {
                    //Debug.Log("检测到召唤区域");
                    if (releasedCard.cardData.cardType == CardType.MUMBER)
                    {
                        // 召唤判定
                        if (GM.Ins.BM.CheckSummonCondition(releasedCard))
                        {
                            GM.Ins.BM.SummonCard(releasedCard);
                            break;
                        }
                    }
                    else if (releasedCard.cardData.cardType == CardType.SPELL)
                    {
                        // 发动判定
                        if (GM.Ins.BM.CheckSpellCastCondition(releasedCard))
                        {
                            GM.Ins.BM.CastSpell(releasedCard);
                            break;
                        }
                        else
                        {
                            Debug.Log($"不符合发动条件{draggingCard.cardData.effectCondition}");
                        }
                    }
                }
            }
            GM.Ins.BM.GetMainPlayer.hands.RefreshCards();
        }
        else if (releasedCard.ableAttack)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit[] hits = Physics.RaycastAll(ray, 100f);
            foreach (var hit in hits)
            {
                if (hit.collider == null || hit.collider.gameObject == gameObject) continue;
                CardController target = hit.collider.GetComponent<CardController>();
                if (target != null)
                {
                    // 攻击判定
                    if (target != releasedCard &&
                        releasedCard.player != target.player &&
                        GM.Ins.BM.IsAttackableTarget(target))
                    {
                        GM.Ins.BM.AttackCard(releasedCard, target);
                        break;
                    }
                }
                else
                {

                    PlayerController player = hit.collider.GetComponentInParent<PlayerController>();
                    if (player != null && player != releasedCard.player &&
                        GM.Ins.BM.IsAttackablePlayer(player))
                    {
                        GM.Ins.BM.AttackPlayer(releasedCard, player);
                        break;
                    }
                }
            }
        }
    }
}
