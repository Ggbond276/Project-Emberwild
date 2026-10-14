using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emberwild.Define
{
    [System.Serializable]
    public class AttackDefine
    {
        public int Key;
        public string ComboGroup; // 例如 "SwordAndShield"
        public int ComboIndex;    // 1, 2, 3, 4
        public float DmgRate;
        public int HitboxType;
        public float OffsetZ;
        public float Param1;
        public float Param2;
        public float Knockback;
    }
}
