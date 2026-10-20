using Emberwild.AI.BT;
using UnityEditor;
using UnityEngine;

namespace Emberwild.AI.BT.Editor
{
    /// <summary>
    /// BTNodeDef 的自定义 Inspector 绘制器。
    /// 按 type 只显示相关字段；按 condition 只显示该条件真正用到的参数（带语义标签）。
    /// 参数个数与标签全部来自 ConditionMeta，不在此处另写一份。
    /// </summary>
    [CustomPropertyDrawer(typeof(BTNodeDef))]
    public class BTNodeDefDrawer : PropertyDrawer
    {
        private const float HeaderHeight = 20f;
        private const float Spacing = 2f;

        private static float LineHeight => EditorGUIUtility.singleLineHeight;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            BTNodeType type = (BTNodeType)property.FindPropertyRelative("type").enumValueIndex;

            float height = HeaderHeight + Spacing;

            switch (type)
            {
                case BTNodeType.Condition:
                    height += LineHeight + Spacing; // 条件下拉
                    ConditionType cond = (ConditionType)property.FindPropertyRelative("condition").enumValueIndex;
                    height += ConditionMeta.ParamCount(cond) * (LineHeight + Spacing);
                    break;

                case BTNodeType.Action:
                    height += LineHeight + Spacing; // 动作下拉
                    break;

                default: // Selector / Sequence
                    height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("children"), true);
                    break;
            }

            return height;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            SerializedProperty typeProp = property.FindPropertyRelative("type");
            BTNodeType type = (BTNodeType)typeProp.enumValueIndex;

            DrawHeader(new Rect(position.x, position.y, position.width, HeaderHeight), typeProp, type);

            float y = position.y + HeaderHeight + Spacing;
            EditorGUI.indentLevel++;

            switch (type)
            {
                case BTNodeType.Condition:
                    DrawCondition(position, property, ref y);
                    break;

                case BTNodeType.Action:
                    DrawLine(position, ref y, property.FindPropertyRelative("action"), "动作");
                    break;

                default:
                    DrawChildren(position, property, ref y);
                    break;
            }

            EditorGUI.indentLevel--;
            EditorGUI.EndProperty();
        }

        private void DrawCondition(Rect position, SerializedProperty property, ref float y)
        {
            SerializedProperty condProp = property.FindPropertyRelative("condition");
            DrawLine(position, ref y, condProp, "条件");

            ConditionType cond = (ConditionType)condProp.enumValueIndex;
            int count = ConditionMeta.ParamCount(cond);

            if (count >= 1)
                DrawLine(position, ref y, property.FindPropertyRelative("p1"), ConditionMeta.ParamLabel(cond, 0));

            if (count >= 2)
                DrawLine(position, ref y, property.FindPropertyRelative("p2"), ConditionMeta.ParamLabel(cond, 1));
        }

        private void DrawChildren(Rect position, SerializedProperty property, ref float y)
        {
            SerializedProperty children = property.FindPropertyRelative("children");
            float height = EditorGUI.GetPropertyHeight(children, true);

            EditorGUI.PropertyField(new Rect(position.x, y, position.width, height),
                children, new GUIContent("子节点"), true);

            y += height;
        }

        private static void DrawLine(Rect position, ref float y, SerializedProperty prop, string label)
        {
            EditorGUI.PropertyField(new Rect(position.x, y, position.width, LineHeight),
                prop, new GUIContent(label));

            y += LineHeight + Spacing;
        }

        private void DrawHeader(Rect header, SerializedProperty typeProp, BTNodeType type)
        {
            Color solid = SolidColor(type);
            EditorGUI.DrawRect(header, new Color(solid.r, solid.g, solid.b, 0.16f));

            float swatchSize = header.height - 8f;
            EditorGUI.DrawRect(new Rect(header.x + 4f, header.y + 4f, swatchSize, swatchSize), solid);

            EditorGUI.PropertyField(
                new Rect(header.x + header.height, header.y, header.width - header.height, header.height),
                typeProp, GUIContent.none);
        }

        private static Color SolidColor(BTNodeType type)
        {
            switch (type)
            {
                case BTNodeType.Selector: return new Color(0.30f, 0.68f, 0.32f);
                case BTNodeType.Sequence: return new Color(0.30f, 0.55f, 0.85f);
                case BTNodeType.Condition: return new Color(0.90f, 0.72f, 0.22f);
                case BTNodeType.Action: return new Color(0.68f, 0.42f, 0.85f);
                default: return Color.gray;
            }
        }
    }
}
