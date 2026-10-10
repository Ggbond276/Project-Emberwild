using Emberwild.Core.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emberwild.Core.Events
{
    public readonly struct JumpStartedEvent : IEvent
    {
        public readonly UnityEngine.Vector3 StartPoint;

        public JumpStartedEvent(UnityEngine.Vector3 startPoint)
        {
            StartPoint = startPoint;
        }
    }
}
