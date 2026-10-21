namespace Emberwild.Editor.MonsterAI
{
    public class MonsterAIEditorOption
    {
        #region 选项数据 (Option Data)
        public readonly string Value;
        public readonly string Label;
        #endregion

        #region 构造方法 (Constructor)
        /// <summary>创建一个下拉选项。</summary>
        public MonsterAIEditorOption(string value, string label)
        {
            Value = value;
            Label = label;
        }
        #endregion
    }
}
