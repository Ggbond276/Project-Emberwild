using Newtonsoft.Json;

namespace Emberwild.Editor.MonsterAI
{
    public class MonsterAttackRow : MonsterAIEditableRow
    {
        #region 攻击判定数据 (Attack Data)
        public string ComboGroup;
        public int ComboIndex;
        public float DmgRate;
        public int HitboxType;
        public float OffsetZ;
        public float Param1;
        public float Param2;
        public float Knockback;

        [JsonIgnore]
        public bool IsExpanded = true;
        #endregion
    }
}
