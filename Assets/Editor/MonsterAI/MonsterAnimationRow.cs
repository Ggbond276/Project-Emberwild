using Newtonsoft.Json;

namespace Emberwild.Editor.MonsterAI
{
    public class MonsterAnimationRow : MonsterAIEditableRow
    {
        #region 动画映射数据 (Animation Data)
        public int TID;
        public int BindingIndex;
        public string SemanticKey;
        public string BindingType;
        public string ParameterName;

        [JsonIgnore]
        public bool IsExpanded = true;
        #endregion
    }
}
