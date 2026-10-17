using Emberwild.AI.BT;
using UnityEditor;
using UnityEngine;

namespace Emberwild.AI.BT.Editor
{
    /// <summary>
    /// BTNodeDef 的自定义 Inspector 绘制器。
    /// 按节点类型(type)只显示相关字段；按条件类型(condition)只显示其真正需要的参数(带语义标签)；
    /// 并用「类型色块 + 缩进」强化决策树的层级辨识。
    /// 仅影响 Editor 显示，不改变任何运行时逻辑。
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
                    height += LineHeight + Spacing; // Condition 下拉
                    ConditionType cond = (ConditionType)property.FindPropertyRelative("condition").enumValueIndex;
                    height += ParamCount(cond) * (LineHeight + Spacing);
                    break;
                case BTNodeType.Action:
                    height += LineHeight + Spacing; // Action 下拉
                    break;
                default: // Selector / Sequence
                    SerializedProperty children = property.FindPropertyRelative("children");
                    height += EditorGUI.GetPropertyHeight(children, true);
                    break;
            }

            return height;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            SerializedProperty typeProp = property.FindPropertyRelative("type");
            BTNodeType type = (BTNodeType)typeProp.enumValueIndex;

            // 1. 着色头部（类型色块 + type 下拉）
            Rect header = new Rect(position.x, position.y, position.width, HeaderHeight);
            DrawHeader(header, typeProp, type);

            // 2. 按类型绘制字段（缩进一级，嵌套层级逐层递增）
            float y = position.y + HeaderHeight + Spacing;
            EditorGUI.indentLevel++;

            switch (type)
            {
                case BTNodeType.Condition:
                    DrawCondition(position, property, ref y);
                    break;
                case BTNodeType.Action:
                    DrawAction(position, property, ref y);
                    break;
                default:
                    DrawChildren(position, property, ref y);
                    break;
            }

            EditorGUI.indentLevel--;
            EditorGUI.EndProperty();
        }

        private void DrawHeader(Rect header, SerializedProperty typeProp, BTNodeType type)
        {
            Color solid = SolidColor(type);
            EditorGUI.DrawRect(header, new Color(solid.r, solid.g, solid.b, 0.16f));

            // 左侧类型色块
            float swatchSize = header.height - 8f;
            Rect swatch = new Rect(header.x + 4f, header.y + 4f, swatchSize, swatchSize);
            EditorGUI.DrawRect(swatch, solid);

            // 右侧 type 下拉（显示中文名）
            Rect typeRect = new Rect(header.x + header.height, header.y,
                header.width - header.height, header.height);
            EditorGUI.PropertyField(typeRect, typeProp, GUIContent.none);
        }

        private void DrawCondition(Rect position, SerializedProperty property, ref float y)
        {
            SerializedProperty condProp = property.FindPropertyRelative("condition");
            SerializedProperty p1Prop = property.FindPropertyRelative("p1");
            SerializedProperty p2Prop = property.FindPropertyRelative("p2");

            DrawLine(position, ref y, condProp, "条件");
            ConditionType cond = (ConditionType)condProp.enumValueIndex;

            switch (cond)
            {
                case ConditionType.HpBelow:
                    DrawLine(position, ref y, p1Prop, "血量阈值 (0~1)");
                    break;
                case ConditionType.InRange:
                    DrawLine(position, ref y, p1Prop, "最小距离");
                    DrawLine(position, ref y, p2Prop, "最大距离");
                    break;
                case ConditionType.CooldownAttackReady:
                    DrawLine(position, ref y, p1Prop, "攻击冷却时长");
                    break;
                case ConditionType.CooldownRollReady:
                    DrawLine(position, ref y, p1Prop, "滚动冷却时长");
                    break;
                // Always / IsDead / RecentlyHit / HasTarget / NotHidden：无参数，不显示
            }
        }

        private void DrawAction(Rect position, SerializedProperty property, ref float y)
        {
            DrawLine(position, ref y, property.FindPropertyRelative("action"), "动作");
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
            EditorGUI.PropertyField(
                new Rect(position.x, y, position.width, LineHeight),
                prop, new GUIContent(label));
            y += LineHeight + Spacing;
        }

        /// <summary>某条件真正用到的参数个数(0/1/2)，与 ConditionFactory 的语义一一对应。</summary>
        private static int ParamCount(ConditionType cond)
        {
            switch (cond)
            {
                case ConditionType.HpBelow:
                case ConditionType.CooldownAttackReady:
                case ConditionType.CooldownRollReady:
                    return 1;
                case ConditionType.InRange:
                    return 2;
                default: // Always / IsDead / RecentlyHit / HasTarget / NotHidden：无参数
                    return 0;
            }
        }

        private static Color SolidColor(BTNodeType type)
        {
            switch (type)
            {
                case BTNodeType.Selector: return new Color(0.30f, 0.68f, 0.32f); // 绿
                case BTNodeType.Sequence: return new Color(0.30f, 0.55f, 0.85f); // 蓝
                case BTNodeType.Condition: return new Color(0.90f, 0.72f, 0.22f); // 黄
                case BTNodeType.Action: return new Color(0.68f, 0.42f, 0.85f); // 紫
                default: return Color.gray;
            }
        }
    }
}
