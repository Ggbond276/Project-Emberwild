using Emberwild.Core.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emberwild.Core.Events
{
    public readonly struct LandedEvent : IEvent
    {
        public readonly UnityEngine.Vector3 GroundPoint;

        public LandedEvent(UnityEngine.Vector3 groundPoint)
        {
            GroundPoint = groundPoint;
        }
    }
}
