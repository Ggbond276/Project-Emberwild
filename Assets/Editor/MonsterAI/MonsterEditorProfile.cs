using System.Collections.Generic;

namespace Emberwild.Editor.MonsterAI
{
    public class MonsterEditorProfile
    {
        #region 怪物聚合数据 (Monster Aggregate)
        public MonsterCharacterRow Character;
        public MonsterAIRow AI;
        public List<MonsterAbilityRow> Abilities = new List<MonsterAbilityRow>();
        public List<MonsterAnimationRow> Animations = new List<MonsterAnimationRow>();
        public List<MonsterBehaviorEditorRule> Rules =
            new List<MonsterBehaviorEditorRule>();
        #endregion

        #region 公共属性 (Public API)
        public int TID => AI != null ? AI.TID : Character != null ? Character.TID : 0;
        public string DisplayName => AI != null ? AI.Name : Character != null
            ? Character.Name
            : "Unnamed";
        #endregion
    }
}
