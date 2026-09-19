using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

// 卡牌数据库的自定义绘制，卡片在列表里直接显示编号和卡名
[CustomEditor(typeof(CardListSO))]
public class CardListSOInspector : Editor
{
    private const float fieldIndent = 12f; // 展开后字段相对表头的缩进

    private ReorderableList cardList; // 卡片列表

    // 建好卡片列表并接上绘制回调
    private void OnEnable()
    {
        SerializedProperty cards = serializedObject.FindProperty("cards");
        cardList = new ReorderableList(serializedObject, cards, true, false, true, true);
        cardList.drawElementCallback = DrawCard;
        cardList.elementHeightCallback = GetCardHeight;
    }

    // 逐张画出卡片，表头显示编号和卡名
    private void DrawCard(Rect rect, int index, bool isActive, bool isFocused)
    {
        SerializedProperty card = cardList.serializedProperty.GetArrayElementAtIndex(index);
        float lineHeight = EditorGUIUtility.singleLineHeight;
        float spacing = EditorGUIUtility.standardVerticalSpacing;

        Rect headerRect = new Rect(rect.x, rect.y + spacing, rect.width, lineHeight);
        card.isExpanded = EditorGUI.Foldout(headerRect, card.isExpanded, GetCardTitle(card));
        if (!card.isExpanded) return;

        Rect fieldRect = new Rect(rect.x + fieldIndent, headerRect.yMax + spacing, rect.width - fieldIndent, lineHeight);
        SerializedProperty field = card.Copy();
        if (!field.NextVisible(true)) return;
        int fieldDepth = field.depth;
        while (field.depth == fieldDepth)
        {
            fieldRect.height = EditorGUI.GetPropertyHeight(field, true);
            EditorGUI.PropertyField(fieldRect, field, true);
            fieldRect.y += fieldRect.height + spacing;
            if (!field.NextVisible(false)) break;
        }
    }

    // 展开的卡片按字段数量占高，收起的只占表头一行
    private float GetCardHeight(int index)
    {
        SerializedProperty card = cardList.serializedProperty.GetArrayElementAtIndex(index);
        float lineHeight = EditorGUIUtility.singleLineHeight;
        float spacing = EditorGUIUtility.standardVerticalSpacing;
        float height = lineHeight + spacing * 2;
        if (!card.isExpanded) return height;

        SerializedProperty field = card.Copy();
        if (!field.NextVisible(true)) return height;
        int fieldDepth = field.depth;
        while (field.depth == fieldDepth)
        {
            height += EditorGUI.GetPropertyHeight(field, true) + spacing;
            if (!field.NextVisible(false)) break;
        }

        return height;
    }

    // 卡片表头显示编号加卡名
    private static string GetCardTitle(SerializedProperty card)
    {
        int index = card.FindPropertyRelative("index").intValue;
        string name = card.FindPropertyRelative("name").stringValue;
        return $"{index} {name}";
    }

    // 画出整个卡片列表
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        cardList.DoLayoutList();
        serializedObject.ApplyModifiedProperties();
    }
}
