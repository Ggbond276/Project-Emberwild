namespace Emberwild.Editor.MonsterAI
{
    public class MonsterAIRow : MonsterAIEditableRow
    {
        #region 怪物 AI 数据 (Monster AI Data)
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
        #endregion
    }
}
