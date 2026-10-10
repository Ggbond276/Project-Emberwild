using Emberwild.Core.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emberwild.Core.Events
{
    /// <summary>
    /// 玩家执行二段跳(空中再次触发)时发布,仅一次。
    /// 订阅方:音效系统(二段跳 SFX)、特效系统(空中冲量特效)、UI 系统(跳跃计数 HUD)。
    /// </summary>
    public readonly struct DoubleJumpStartedEvent : IEvent
    {
        public readonly UnityEngine.Vector3 StartPoint;
        public DoubleJumpStartedEvent(UnityEngine.Vector3 startPoint)
        {
            StartPoint = startPoint;
        }

    }
}
