using Newtonsoft.Json;

namespace Emberwild.Editor.MonsterAI
{
    public class MonsterCharacterRow : MonsterAIEditableRow
    {
        #region 角色数据 (Character Data)
        public int TID;
        public string Name;

        [JsonProperty("MainType")]
        public string MainType;

        public string SubType;
        public string Resource;
        public float HP;
        public float AD;
        public float DEF;
        #endregion
    }
}
