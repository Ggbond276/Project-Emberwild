using Emberwild.AI.Enemy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Emberwild.AI.BT
{
    /// <summary>
    /// 条件工厂
    /// </summary>
    public class ConditionFactory
    {
        // TODO: 条件工厂可读性太差
        public static Func<EnemyBrain, bool> Build(BTNodeDef def)
        {
            switch (def.condition)
            {
                case ConditionType.Always: 
                    return b => true;

                case ConditionType.IsDead: 
                    return b => b.Entity != null && b.Entity.Attribute != null && b.Entity.Attribute.IsDead;

                case ConditionType.RecentlyHit: 
                    return b => Time.time - b.BB.LastHitTime < b.HitStunDuration; // 与 HitState 共用 config.hitstunDuration,单一来源

                case ConditionType.HpBelow: 
                    return b => b.BB.HpRatio < def.p1;

                case ConditionType.InRange: 
                    return b => b.BB.DistanceToTarget >= def.p1 && b.BB.DistanceToTarget <= def.p2;

                case ConditionType.CooldownAttackReady: 
                    return b => Time.time - b.BB.LastAttackTime >= def.p1;

                case ConditionType.CooldownRollReady: 
                    return b => Time.time - b.BB.LastRollTime >= def.p1;

                case ConditionType.HasTarget: 
                    return b => b.BB.CanSeeTarget;

                case ConditionType.NotHidden: 
                    return b => !b.BB.HasHidden;

                case ConditionType.CooldownHeavyReady: 
                    return b => Time.time - b.BB.LastHeavyTime >= def.p1;

                case ConditionType.CooldownPounceReady:
                    return b => Time.time - b.BB.LastPounceTime >= def.p1;

                case ConditionType.CooldownLeapReady:
                    return b => Time.time - b.BB.LastLeapTime >= def.p1;

                case ConditionType.CooldownRangedReady:
                    return b => Time.time - b.BB.LastRangedTime >= def.p1;

                case ConditionType.CooldownSpellReady:
                    return b => Time.time - b.BB.LastSpellTime >= def.p1;

                case ConditionType.NotAlerted:
                    return b => !b.BB.HasAlerted;
                default: return b => true;
            }
        }
    }
}
