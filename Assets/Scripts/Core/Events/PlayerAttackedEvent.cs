using UnityEngine;

namespace Emberwild.Core.Events
{
    /// <summary>
    /// 玩家完成一次攻击动作时发布(动画 Attack State 退出/命中帧触发)。
    /// 订阅方:伤害系统(判定命中目标)、音效系统、特效系统。
    /// </summary>
    public readonly struct PlayerAttackedEvent : IEvent
    {
        /// <summary>攻击段位(0~3,对应 Attack01~04)。</summary>
        public readonly int ComboIndex;
        /// <summary>命中框中心(世界坐标)。</summary>
        public readonly Vector3 HitPoint;
        /// <summary>命中框朝向(单位向量)。</summary>
        public readonly Vector3 HitDirection;
        /// <summary>伤害值。</summary>
        public readonly float Damage;

        public PlayerAttackedEvent(int comboIndex, Vector3 hitPoint, Vector3 hitDirection, float damage)
        {
            ComboIndex = comboIndex;
            HitPoint = hitPoint;
            HitDirection = hitDirection;
            Damage = damage;
        }
    }
}
