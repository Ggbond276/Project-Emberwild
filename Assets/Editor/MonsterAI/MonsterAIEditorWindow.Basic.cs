using UnityEditor;
using UnityEngine;

namespace Emberwild.Editor.MonsterAI
{
    public partial class MonsterAIEditorWindow
    {
        #region 基础与躯体标签页 (Basic & Body Tab)
        /// <summary>绘制身份、资源和 AI 基础参数。</summary>
        private void DrawBasicTab()
        {
            var profile = _selectedProfile;
            EditorGUI.BeginChangeCheck();

            DrawSection("身份与资源");
            EditorGUILayout.LabelField("TID", profile.TID.ToString());
            EditorGUILayout.HelpBox(
                "TID 创建后不可修改，避免七张表之间的引用断开。",
                MessageType.None);

            string name = EditorGUILayout.TextField(
                new GUIContent("怪物名称", "同时写入 CharacterDefine 和 MonsterAI。"),
                profile.Character.Name);
            string subType = EditorGUILayout.TextField(
                new GUIContent(
                    "角色子类型 SubType",
                    "必须与 AttackDefine 的 ComboGroup 一致。"),
                profile.Character.SubType);
            string resource = EditorGUILayout.TextField(
                new GUIContent(
                    "Prefab 资源路径",
                    "相对于 Resources 目录，不要填写 Assets/Resources 前缀。"),
                profile.Character.Resource);
            EditorGUILayout.LabelField(
                "MainType",
                string.IsNullOrWhiteSpace(profile.Character.MainType)
                    ? "Monster"
                    : profile.Character.MainType);

            GameObject prefab = string.IsNullOrWhiteSpace(resource)
                ? null
                : Resources.Load<GameObject>(resource);
            EditorGUILayout.ObjectField(
                new GUIContent("资源预览", "这里只显示，不会自动修改路径。"),
                prefab,
                typeof(GameObject),
                false);

            if (prefab == null && !string.IsNullOrWhiteSpace(resource))
            {
                EditorGUILayout.HelpBox(
                    "当前 Resources 路径找不到 Prefab。",
                    MessageType.Error);
            }
            EditorGUILayout.HelpBox(
                "SubType 会作为攻击配置的 ComboGroup。想让怪物使用哪组攻击，就在这里填写相同名称。",
                MessageType.Info);
            EndSection();

            DrawSection("战斗基础数值");
            float hp = EditorGUILayout.FloatField(
                new GUIContent("生命上限 HP", "怪物出生时恢复到的满血值。"),
                profile.Character.HP);
            float ad = EditorGUILayout.FloatField(
                new GUIContent("攻击力 AD"),
                profile.Character.AD);
            float def = EditorGUILayout.FloatField(
                new GUIContent("防御力 DEF"),
                profile.Character.DEF);
            EndSection();

            DrawSection("AI 感知与移动");
            float viewRange = EditorGUILayout.FloatField(
                new GUIContent("首次发现距离", "怪物第一次锁定目标时使用的扫描半径。"),
                profile.AI.viewRange);
            float loseTargetRange = EditorGUILayout.FloatField(
                new GUIContent("丢失目标距离", "通常略大于首次发现距离，形成追击缓冲。"),
                profile.AI.loseTargetRange);
            float scanInterval = EditorGUILayout.FloatField(
                new GUIContent("扫描间隔（秒）", "越小反应越快，但物理查询次数越多。"),
                profile.AI.scanInterval);
            float runSpeed = EditorGUILayout.FloatField(
                new GUIContent("移动速度"),
                profile.AI.runSpeed);
            float turnSpeed = EditorGUILayout.FloatField(
                new GUIContent("转向速度"),
                profile.AI.turnSpeed);
            float stoppingDistance = EditorGUILayout.FloatField(
                new GUIContent("停止距离"),
                profile.AI.stoppingDistance);
            float hitStun = EditorGUILayout.FloatField(
                new GUIContent("受击硬直（秒）"),
                profile.AI.hitStunDuration);
            EndSection();

            DrawSection("Animator 根参数");
            string speedParameter = EditorGUILayout.TextField(
                new GUIContent("移动参数名", "Animator 中的 Float 参数，例如 MoveSpeed。"),
                profile.AI.speedParameter);
            string deadParameter = EditorGUILayout.TextField(
                new GUIContent("死亡参数名", "Animator 中的 Bool 参数，例如 IsDead。"),
                profile.AI.deadParameter);
            EditorGUILayout.HelpBox(
                "参数名来自该怪物 Animator Controller，不需要填写动画状态机的资源名。",
                MessageType.Info);
            EndSection();

            if (EditorGUI.EndChangeCheck())
            {
                profile.Character.Name = name;
                profile.Character.SubType = subType;
                profile.Character.Resource = resource;
                profile.Character.MainType = string.IsNullOrWhiteSpace(
                    profile.Character.MainType)
                    ? "Monster"
                    : profile.Character.MainType;
                profile.Character.HP = Mathf.Max(0f, hp);
                profile.Character.AD = Mathf.Max(0f, ad);
                profile.Character.DEF = Mathf.Max(0f, def);

                profile.AI.Name = name;
                profile.AI.viewRange = Mathf.Max(0.01f, viewRange);
                profile.AI.loseTargetRange = Mathf.Max(
                    profile.AI.viewRange,
                    loseTargetRange);
                profile.AI.scanInterval = Mathf.Max(0.01f, scanInterval);
                profile.AI.runSpeed = Mathf.Max(0f, runSpeed);
                profile.AI.turnSpeed = Mathf.Max(0f, turnSpeed);
                profile.AI.stoppingDistance = Mathf.Max(0f, stoppingDistance);
                profile.AI.hitStunDuration = Mathf.Max(0f, hitStun);
                profile.AI.speedParameter = speedParameter;
                profile.AI.deadParameter = deadParameter;
                _store.MarkDirty();
            }

            if (_store.IsDirty)
            {
                EditorGUILayout.HelpBox(
                    "当前有未保存修改。切换怪物不会丢失，但关闭窗口前请保存。",
                    MessageType.Warning);
            }
        }
        #endregion
    }
}
