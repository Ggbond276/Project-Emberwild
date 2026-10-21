using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Emberwild.Editor.MonsterAI
{
    public abstract class MonsterAIEditableRow
    {
        #region 序列化扩展 (Serialization Extension)
        // 保留未来新增但编辑器暂不认识的字段，避免保存时丢数据
        [JsonExtensionData]
        public IDictionary<string, JToken> ExtraData { get; set; } =
            new Dictionary<string, JToken>();

        [JsonIgnore]
        public int RowKey { get; set; }
        #endregion
    }
}
