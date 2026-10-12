using Emberwild.Define;
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
        public int TID; // 关联配置表的唯一ID

        [Header("血量")]
        public float MaxHp;
        public float CurrentHp;

        [Header("战斗属性")]
        public float AttackDamage;
        public float Defense;

        [Header("运行时状态")]
        public bool IsDead = false;
        public bool IsInvincible = false;
        public bool IsInvulnerable = false;

        /// <summary>
        /// 核心：从配置表注入静态数据，并初始化动态状态
        /// </summary>
        public void InitFromDefine(CharacterDefine define)
        {
            this.TID = define.TID;

            // 数据桥接：将 Define 表里的数值赋给运行时数据
            this.MaxHp = define.HP;
            this.AttackDamage = define.AD;
            this.Defense = define.DEF;

            // 状态初始化
            this.CurrentHp = this.MaxHp; // 满血出生
            this.IsDead = false;
        }

        public void ResetToFull()
        {
            CurrentHp = MaxHp;
            IsDead = false;
            IsInvincible = false;
            IsInvulnerable = false;
        }
    }
}
