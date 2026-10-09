using UnityEngine;

namespace Emberwild.Character
{
    /// <summary>
    /// 角色运行时数据容器 — 纯数据,无逻辑。
    /// 支持 Unity JSON 序列化,可直接用于存档/读档。
    ///
    /// 未来可追加:MaxMp, Strength, Defense, Level, Experience,
    /// Buff列表, Debuff列表, 装备列表 等。
    /// </summary>
    [System.Serializable]
    public class CharacterData
    {
        [Header("血量")]
        [Tooltip("最大生命值")]
        public float MaxHp = 100f;

        [Tooltip("当前生命值")]
        public float CurrentHp = 100f;

        [Header("运行时状态")]
        [Tooltip("是否已死亡")]
        public bool IsDead = false;

        // === 预留扩展字段(本 Stage 不启用) ===
        [Tooltip("是否霸体(受伤不打断动作)")]
        public bool IsInvincible = false;

        [Tooltip("是否处于无敌帧")]
        public bool IsInvulnerable = false;

        /// <summary>
        /// 重置为满血状态(用于关卡重置/复活)
        /// </summary>
        public void ResetToFull()
        {
            CurrentHp = MaxHp;
            IsDead = false;
            IsInvincible = false;
            IsInvulnerable = false;
        }
    }
}
