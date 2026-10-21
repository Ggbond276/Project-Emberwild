using System;

namespace Emberwild.Define
{
    /// <summary>怪物核心表和 Animator 根参数。</summary>
    [Serializable]
    public class MonsterAICoreDefine
    {
        public int TID;
        public string Name;
        public float viewRange;
        public float loseTargetRange;
        public float scanInterval;
        public float runSpeed;
        public float turnSpeed;
        public float stoppingDistance;
        public float hitStunDuration;
        public string speedParameter;
        public string deadParameter;
    }
}
