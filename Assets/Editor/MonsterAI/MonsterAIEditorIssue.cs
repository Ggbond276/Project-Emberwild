namespace Emberwild.Editor.MonsterAI
{
    public class MonsterAIEditorIssue
    {
        #region 问题数据 (Issue Data)
        public readonly MonsterAIEditorIssueLevel Level;
        public readonly int TID;
        public readonly string Message;
        #endregion

        #region 构造方法 (Constructor)
        /// <summary>创建一条配置校验结果。</summary>
        public MonsterAIEditorIssue(
            MonsterAIEditorIssueLevel level,
            int tid,
            string message)
        {
            Level = level;
            TID = tid;
            Message = message;
        }
        #endregion
    }
}
