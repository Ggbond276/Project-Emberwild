using Emberwild.Core.AppLog;
using Emberwild.Core.Singleton;
using Emberwild.Define;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace Emberwild.Manager
{
    public class DataManager : Singleton<DataManager>
    {
        public string DataPath;

        public Dictionary<int, CharacterDefine> Characters = null;
        public Dictionary<int, AttackDefine> Attacks = null;
        public Dictionary<string, Dictionary<int, AttackDefine>> ComboGroupDict = new Dictionary<string, Dictionary<int, AttackDefine>>();

        public Dictionary<int, MonsterAICoreDefine> MonsterAICores =
            new Dictionary<int, MonsterAICoreDefine>();
        public Dictionary<int, List<MonsterAbilityDefine>> MonsterAbilitiesByTid =
            new Dictionary<int, List<MonsterAbilityDefine>>();
        public Dictionary<int, List<MonsterBehaviorDefine>> MonsterBehaviorsByTid =
            new Dictionary<int, List<MonsterBehaviorDefine>>();
        public Dictionary<int, List<MonsterBehaviorConditionDefine>> MonsterBehaviorConditionsByTid =
            new Dictionary<int, List<MonsterBehaviorConditionDefine>>();
        public Dictionary<int, List<MonsterAnimationBindingDefine>> MonsterAnimationBindingsByTid =
            new Dictionary<int, List<MonsterAnimationBindingDefine>>();

         public DataManager()
        {
            this.DataPath = "Data/Data/";
            Load();
        }

        public void Init()
        {
            AppLog.Info("DataManager", "DataManager Init");
        }

        public void Load()
        {
            string json = File.ReadAllText(this.DataPath + "CharacterDefine.txt");
            this.Characters = JsonConvert.DeserializeObject<Dictionary<int, CharacterDefine>>(json);

            json = File.ReadAllText(this.DataPath + "AttackDefine.txt");
            this.Attacks = JsonConvert.DeserializeObject<Dictionary<int, AttackDefine>>(json);

            ComboGroupDict.Clear();
            foreach(var kvp in Attacks)
            {
                AttackDefine atk = kvp.Value;
                if(!ComboGroupDict.ContainsKey(atk.ComboGroup))
                {
                    ComboGroupDict[atk.ComboGroup] = new Dictionary<int, AttackDefine>();
                }

                // 把这把武器的第 N 刀，塞进对应的坑位里
                ComboGroupDict[atk.ComboGroup][atk.ComboIndex] = atk;
            }

            LoadMonsterTables();
        }

        public IEnumerator LoadData()
        {
            Load();
            yield return null;
        }

        /// <summary>
        /// 加载怪物核心、能力、行为、条件和动画绑定外联表。
        /// </summary>
        private void LoadMonsterTables()
        {
            MonsterAICores = LoadRequired<Dictionary<int, MonsterAICoreDefine>>("MonsterAI.txt");
            var abilities = LoadRequired<Dictionary<int, MonsterAbilityDefine>>("MonsterAbility.txt");
            var behaviors = LoadRequired<Dictionary<int, MonsterBehaviorDefine>>("MonsterBehavior.txt");
            var conditions = LoadRequired<Dictionary<int, MonsterBehaviorConditionDefine>>("MonsterBehaviorCondition.txt");
            var bindings = LoadRequired<Dictionary<int, MonsterAnimationBindingDefine>>("MonsterAnimation.txt");

            MonsterAbilitiesByTid = abilities.Values
                .Where(item => item != null)
                .GroupBy(item => item.TID)
                .ToDictionary(group => group.Key, group => group.ToList());
            MonsterBehaviorsByTid = behaviors.Values
                .Where(item => item != null)
                .GroupBy(item => item.TID)
                .ToDictionary(group => group.Key, group => group.OrderBy(item => item.RuleIndex).ToList());
            MonsterBehaviorConditionsByTid = conditions.Values
                .Where(item => item != null)
                .GroupBy(item => item.TID)
                .ToDictionary(group => group.Key,
                    group => group.OrderBy(item => item.RuleIndex).ThenBy(item => item.ConditionIndex).ToList());
            MonsterAnimationBindingsByTid = bindings.Values
                .Where(item => item != null)
                .GroupBy(item => item.TID)
                .ToDictionary(group => group.Key, group => group.OrderBy(item => item.BindingIndex).ToList());

            AppLog.Info("DataManager",
                $"Monster tables loaded: cores={MonsterAICores.Count} abilities={abilities.Count} "
                + $"rules={behaviors.Count} conditions={conditions.Count} bindings={bindings.Count}");
        }

        private T LoadRequired<T>(string fileName) where T : class
        {
            string path = DataPath + fileName;
            if (!File.Exists(path))
                throw new FileNotFoundException($"Required data table is missing: {path}", path);
            return JsonConvert.DeserializeObject<T>(File.ReadAllText(path));
        }
    }
}
