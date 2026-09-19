using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// 卡牌效果的自定义绘制，把效果参数显示成有名字的字段
[CustomPropertyDrawer(typeof(CardEffect))]
public class CardEffectDrawer : PropertyDrawer
{
    // 触发时点的中文名，下标对应 TriggerType
    private static readonly string[] triggerNames =
    {
        "无", "自己回合开始时", "自己回合结束时", "召唤成功时", "被破坏送去墓地时", "伤害计算后", "战斗破坏对方角色时", "起动效果"
    };

    // 效果种类的中文名，下标对应 EffectType，中括号里是这个效果的参数名，没有对应枚举值的留空
    private static readonly string[] effectNames =
    {
        "无", "对1名敌方角色造成[伤害]点伤害", "", "抽[抽牌数]张卡", "修改1名友方角色的攻击力[攻击变化]和生命值[生命变化]", "修改自身的攻击力[攻击变化]和生命值[生命变化]", "", "",
        "对双方场上所有角色各造成[伤害]点伤害", "修改自己场上全体角色的攻击力[攻击变化]和生命值[生命变化]", "提高自己[增加部署点上限]点部署费用上限", "恢复1名友方角色[治疗量]点生命值", "使1名敌方角色返回持有者手牌",
        "对随机[数量]名敌方角色各造成[伤害]点伤害", "对敌方场上所有角色各造成[伤害]点伤害", "修改1名敌方角色的攻击力[攻击变化]和生命值[生命变化]", "随机恢复[数量]名友方角色各[治疗量]点生命值", "使1名敌方角色的效果无效化",
        "使场上1张卡返回持有者手牌", "从自己墓地选1名角色加入手牌", "破坏1名敌方角色", "从牌堆检索1名部署费用[费用下限]以上的角色加入手牌", "",
        "从牌堆检索1名具有[检索的触发时点]效果的角色加入手牌", "", "从自己墓地特殊召唤1名角色", "增加自己[增加费用]点当前部署费用", "使1名友方角色返回手牌，自己增加[增加费用]点部署费用",
        "修改1名生命值2以下的友方角色的攻击力[攻击变化]和生命值[生命变化]", "使这张卡可以再次攻击", "随机使敌方把[弃牌数]张手牌送去墓地", "把自己[弃牌数]张手牌送去墓地，然后抽[抽牌数]张卡", "恢复玩家[治疗量]点生命值"
    };

    // 发动条件的中文名，下标对应 ConditionType，没有对应枚举值的留空
    private static readonly string[] conditionNames =
    {
        "无", "自己场上恰好有2名角色", "自己手牌3张以上", "", "敌方场上有角色", "自己场上有角色", "自己墓地有角色"
    };

    private static readonly string[] triggerLabels; // 触发时点下拉框的显示名
    private static readonly int[] triggerValues; // 触发时点下拉框对应的枚举值
    private static readonly string[] effectLabels; // 效果种类下拉框的显示名
    private static readonly int[] effectValues; // 效果种类下拉框对应的枚举值
    private static readonly string[] conditionLabels; // 发动条件下拉框的显示名
    private static readonly int[] conditionValues; // 发动条件下拉框对应的枚举值

    // 生成三组下拉框数据
    static CardEffectDrawer()
    {
        BuildOptions(triggerNames, out triggerLabels, out triggerValues);
        BuildOptions(effectNames, out effectLabels, out effectValues);
        BuildOptions(conditionNames, out conditionLabels, out conditionValues);
    }

    // 按中文名表生成下拉框的显示名和枚举值，空名字的枚举值跳过
    private static void BuildOptions(string[] names, out string[] labels, out int[] values)
    {
        var labelList = new List<string>();
        var valueList = new List<int>();
        for (int i = 0; i < names.Length; i++)
        {
            if (names[i].Length == 0) continue;
            labelList.Add(names[i]);
            valueList.Add(i);
        }

        labels = labelList.ToArray();
        values = valueList.ToArray();
    }

    // 每个效果类型的参数名，顺序就是效果参数的下标
    private static string[] GetParamNames(EffectType effectType)
    {
        switch (effectType)
        {
            case EffectType.DealDamageToEnemy:
            case EffectType.DamageAllEnemy:
            case EffectType.DamageAll:
                return new[] { "伤害" };
            case EffectType.HealAlly:
            case EffectType.HealPlayer:
                return new[] { "治疗量" };
            case EffectType.Draw:
                return new[] { "抽牌数" };
            case EffectType.DropEnemyHand:
                return new[] { "弃牌数" };
            case EffectType.AddCost:
            case EffectType.BackHandAddCost:
                return new[] { "增加费用" };
            case EffectType.AddCostMax:
                return new[] { "增加部署点上限" };
            case EffectType.SearchMumberCostUp:
                return new[] { "费用下限" };
            case EffectType.SearchMumberTrigger:
                return new[] { "检索的触发时点" };
            case EffectType.BuffSelf:
            case EffectType.BuffAlly:
            case EffectType.BuffEnemy:
            case EffectType.BuffAlliesAll:
            case EffectType.BuffLowHpAlly:
                return new[] { "攻击变化", "生命变化" };
            case EffectType.DropAndDraw:
                return new[] { "弃牌数", "抽牌数" };
            case EffectType.DealDamageToRandomEnemy:
                return new[] { "数量", "伤害" };
            case EffectType.HeallRandomAllies:
                return new[] { "数量", "治疗量" };
            default:
                return new string[0];
        }
    }

    // 这个效果在 Inspector 里占用的高度
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        int lineCount = 3 + GetParamNames((EffectType)property.FindPropertyRelative("effectType").intValue).Length;
        return lineCount * EditorGUIUtility.singleLineHeight + (lineCount - 1) * EditorGUIUtility.standardVerticalSpacing;
    }

    // 按效果种类画出带名字的字段
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        SerializedProperty triggerProperty = property.FindPropertyRelative("triggerType"); // 触发时点
        SerializedProperty effectProperty = property.FindPropertyRelative("effectType"); // 效果种类
        SerializedProperty valueProperty = property.FindPropertyRelative("effectValue"); // 效果参数
        SerializedProperty conditionProperty = property.FindPropertyRelative("effectCondition"); // 发动条件

        float step = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing; // 每行的下移距离
        Rect rect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        triggerProperty.intValue = EditorGUI.IntPopup(rect, "触发时点", triggerProperty.intValue, triggerLabels, triggerValues);
        rect.y += step;
        effectProperty.intValue = EditorGUI.IntPopup(rect, "效果种类", effectProperty.intValue, effectLabels, effectValues);
        rect.y += step;

        EffectType effectType = (EffectType)effectProperty.intValue;
        string[] paramNames = GetParamNames(effectType);
        // 参数个数由效果种类决定，换效果时自动对齐
        if (valueProperty.arraySize != paramNames.Length) valueProperty.arraySize = paramNames.Length;
        for (int i = 0; i < paramNames.Length; i++)
        {
            SerializedProperty element = valueProperty.GetArrayElementAtIndex(i);
            element.intValue = effectType == EffectType.SearchMumberTrigger
                ? EditorGUI.IntPopup(rect, paramNames[i], element.intValue, triggerLabels, triggerValues)
                : EditorGUI.IntField(rect, paramNames[i], element.intValue);
            rect.y += step;
        }

        conditionProperty.intValue = EditorGUI.IntPopup(rect, "发动条件", conditionProperty.intValue, conditionLabels, conditionValues);
    }
}
