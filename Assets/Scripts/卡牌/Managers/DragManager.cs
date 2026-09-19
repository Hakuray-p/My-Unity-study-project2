using UnityEngine;

// 鼠标拖拽：手牌拖到场上是召唤 / 施法，场上卡牌拖到目标上是攻击
public class DragManager : MonoBehaviour
{
    private CardController draggingCard; // 正在拖拽的卡牌
    private Vector3 offset; // 卡牌和鼠标之间的偏移
    private bool isDragging = false; // 是否正在拖拽
    private Vector3 pressMousePos; // 按下时的鼠标位置
    private float clickDistance = 30f; // 判定点选还是拖拽的鼠标位移
    private const string viewHint = "点一下屏幕或按 Esc 收起"; // 查看界面的操作提示

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

    // 按下时判断点到的是墓地、手牌还是场上的卡牌
    void MouseDown()
    {
        // 正在选目标或查看列表时，这次按下交给 TargetManager 处理（从墓地选牌的流程也在这里）
        if (GM.Ins.BM.TM.IsSelecting) return;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit nearestHit = default; // 离相机最近的那个碰撞体
        bool hasHit = false;
        foreach (RaycastHit hit in Physics.RaycastAll(ray, 100f))
        {
            if (hit.collider == null) continue;
            if (!hasHit || hit.distance < nearestHit.distance)
            {
                nearestHit = hit;
                hasHit = true;
            }

            // 点到墓地里的牌就摊开这一方的墓地
            CardController graveCard = hit.collider.GetComponent<CardController>();
            if (graveCard != null && graveCard.cardState == CardState.Graveyard)
            {
                ViewGrave(graveCard.player);
                return;
            }

            // 点到墓地空位也一样，隔着别的碰撞体也能点到
            PlayerController graveOwner = hit.collider.GetComponentInParent<PlayerController>();
            if (graveOwner != null && hit.collider.transform == graveOwner.gravePos)
            {
                ViewGrave(graveOwner);
                return;
            }
        }

        if (!hasHit) return;
        CardController card = nearestHit.collider.GetComponent<CardController>();
        if (card == null || !card.player.isMainPlayer) return;

        Vector3 mousePos = Input.mousePosition;
        mousePos.z = 10f; // 距离摄像机的距离
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(mousePos);
        if (card.cardState == CardState.Hand)
        {
            // 手牌拖拽
            draggingCard = card;
            offset = card.transform.position - worldPos;
            isDragging = true;
        }
        else if (card.cardState == CardState.Field && card.player.isInTurn)
        {
            // 场上卡牌：接着可能是拖去攻击，也可能是点选发动效果
            draggingCard = card;
            isDragging = true;
            pressMousePos = Input.mousePosition;
            if (card.ableAttack) attackAimIcon.SetActive(true);
        }
    }

    // 打开某一方的墓地查看，说明文字写清楚这是谁的墓地
    private void ViewGrave(PlayerController owner)
    {
        string title = owner.isMainPlayer ? "我方墓地" : "对手的墓地";
        GM.Ins.BM.TM.StartViewList(owner.graveCards, title + "\n" + viewHint);
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
                            Debug.Log("不符合发动条件");
                        }
                    }
                }
            }
            GM.Ins.BM.GetMainPlayer.hands.RefreshCards();
        }
        else if (releasedCard.cardState == CardState.Field)
        {
            if (TryAttack(releasedCard)) return;
            // 松开时鼠标没挪动，算点选这张卡，发动它的一回合一次效果
            if (Vector3.Distance(pressMousePos, Input.mousePosition) < clickDistance)
            {
                GM.Ins.BM.EM.TriggerCardEffect(TriggerType.Cast, releasedCard);
            }
        }
    }

    // 场上卡牌拖到目标上就开打，打出去了返回 true
    bool TryAttack(CardController attacker)
    {
        if (!attacker.ableAttack) return false;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit[] hits = Physics.RaycastAll(ray, 100f);
        foreach (var hit in hits)
        {
            if (hit.collider == null || hit.collider.gameObject == gameObject) continue;
            CardController target = hit.collider.GetComponent<CardController>();
            if (target != null)
            {
                // 攻击判定
                if (target != attacker &&
                    attacker.player != target.player &&
                    GM.Ins.BM.IsAttackableTarget(target))
                {
                    GM.Ins.BM.AttackCard(attacker, target);
                    return true;
                }
            }
            else
            {
                PlayerController player = hit.collider.GetComponentInParent<PlayerController>();
                if (player != null && player != attacker.player &&
                    GM.Ins.BM.IsAttackablePlayer(player))
                {
                    GM.Ins.BM.AttackPlayer(attacker, player);
                    return true;
                }
            }
        }
        return false;
    }
}
