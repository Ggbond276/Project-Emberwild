using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Emberwild.AI.Enemy.Ability;
using Newtonsoft.Json;
using UnityEngine;

namespace Emberwild.Editor.MonsterAI
{
    public class MonsterAIEditorStore
    {
        #region 文件定义 (File Definition)
        private const string CharacterFile = "CharacterDefine.txt";
        private const string MonsterAIFile = "MonsterAI.txt";
        private const string AbilityFile = "MonsterAbility.txt";
        private const string AnimationFile = "MonsterAnimation.txt";
        private const string BehaviorFile = "MonsterBehavior.txt";
        private const string ConditionFile = "MonsterBehaviorCondition.txt";
        private const string AttackFile = "AttackDefine.txt";
        #endregion

        #region 运行时数据 (Runtime Data)
        private readonly Dictionary<int, MonsterCharacterRow> _characters =
            new Dictionary<int, MonsterCharacterRow>();
        private readonly List<MonsterAbilityRow> _orphanAbilities =
            new List<MonsterAbilityRow>();
        private readonly List<MonsterAnimationRow> _orphanAnimations =
            new List<MonsterAnimationRow>();
        private readonly List<MonsterBehaviorRow> _orphanBehaviors =
            new List<MonsterBehaviorRow>();
        private readonly List<MonsterBehaviorConditionRow> _orphanConditions =
            new List<MonsterBehaviorConditionRow>();

        private static readonly JsonSerializerSettings JsonSettings =
            new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                NullValueHandling = NullValueHandling.Ignore,
                MissingMemberHandling = MissingMemberHandling.Ignore,
                ObjectCreationHandling = ObjectCreationHandling.Replace
            };
        #endregion

        #region 公共属性 (Public API)
        public List<MonsterEditorProfile> Monsters { get; } =
            new List<MonsterEditorProfile>();

        public List<MonsterAttackRow> Attacks { get; } =
            new List<MonsterAttackRow>();

        public bool IsDirty { get; private set; }

        public string DataDirectory => Path.GetFullPath(
            Path.Combine(Application.dataPath, "..", "Data", "Data"));
        #endregion

        #region 数据加载 (Data Loading)
        /// <summary>从 Data/Data 读取七张配置表并聚合怪物。</summary>
        public bool Load(out string error)
        {
            error = string.Empty;
            try
            {
                ClearRuntimeData();

                foreach (var pair in LoadDictionary<MonsterCharacterRow>(CharacterFile))
                {
                    pair.Value.RowKey = pair.Key;
                    _characters[pair.Key] = pair.Value;
                }

                var aiRows = LoadDictionary<MonsterAIRow>(MonsterAIFile).Values
                    .OrderBy(row => row.TID)
                    .ToList();
                var abilities = LoadDictionary<MonsterAbilityRow>(AbilityFile).Values
                    .ToList();
                var animations = LoadDictionary<MonsterAnimationRow>(AnimationFile).Values
                    .ToList();
                var behaviors = LoadDictionary<MonsterBehaviorRow>(BehaviorFile).Values
                    .ToList();
                var conditions = LoadDictionary<MonsterBehaviorConditionRow>(ConditionFile).Values
                    .ToList();

                Attacks.AddRange(LoadDictionary<MonsterAttackRow>(AttackFile)
                    .Select(pair =>
                    {
                        pair.Value.RowKey = pair.Key;
                        return pair.Value;
                    }));

                var monsterTids = new HashSet<int>(aiRows.Select(row => row.TID));
                var abilitiesByTid = abilities.GroupBy(row => row.TID)
                    .ToDictionary(group => group.Key, group => group.ToList());
                var animationsByTid = animations.GroupBy(row => row.TID)
                    .ToDictionary(group => group.Key, group => group.ToList());
                var behaviorsByTid = behaviors.GroupBy(row => row.TID)
                    .ToDictionary(
                        group => group.Key,
                        group => group.OrderBy(row => row.RuleIndex).ToList());
                var conditionsByTid = conditions.GroupBy(row => row.TID)
                    .ToDictionary(
                        group => group.Key,
                        group => group.OrderBy(row => row.RuleIndex)
                            .ThenBy(row => row.ConditionIndex)
                            .ToList());

                foreach (var ai in aiRows)
                {
                    if (!_characters.TryGetValue(ai.TID, out var character))
                    {
                        character = CreateMissingCharacter(ai.TID, ai.Name);
                        _characters[ai.TID] = character;
                    }

                    var profile = new MonsterEditorProfile
                    {
                        Character = character,
                        AI = ai,
                        Abilities = abilitiesByTid.TryGetValue(ai.TID, out var monsterAbilities)
                            ? monsterAbilities
                            : new List<MonsterAbilityRow>(),
                        Animations = animationsByTid.TryGetValue(ai.TID, out var monsterAnimations)
                            ? monsterAnimations
                            : new List<MonsterAnimationRow>()
                    };

                    if (behaviorsByTid.TryGetValue(ai.TID, out var monsterBehaviors))
                    {
                        var conditionsByRule = conditionsByTid.TryGetValue(
                            ai.TID,
                            out var monsterConditions)
                            ? monsterConditions.GroupBy(row => row.RuleIndex)
                                .ToDictionary(
                                    group => group.Key,
                                    group => group.OrderBy(row => row.ConditionIndex).ToList())
                            : new Dictionary<int, List<MonsterBehaviorConditionRow>>();

                        foreach (var behavior in monsterBehaviors)
                        {
                            profile.Rules.Add(new MonsterBehaviorEditorRule
                            {
                                Rule = behavior,
                                Conditions = conditionsByRule.TryGetValue(
                                    behavior.RuleIndex,
                                    out var ruleConditions)
                                    ? ruleConditions
                                    : new List<MonsterBehaviorConditionRow>()
                            });
                        }
                    }

                    Monsters.Add(profile);
                }

                _orphanAbilities.AddRange(abilities.Where(row => !monsterTids.Contains(row.TID)));
                _orphanAnimations.AddRange(
                    animations.Where(row => !monsterTids.Contains(row.TID)));
                _orphanBehaviors.AddRange(
                    behaviors.Where(row => !monsterTids.Contains(row.TID)));
                _orphanConditions.AddRange(
                    conditions.Where(row => !monsterTids.Contains(row.TID)));

                SortMonsters();
                IsDirty = false;
                return true;
            }
            catch (Exception exception)
            {
                error = $"读取配置失败：{exception.Message}";
                return false;
            }
        }

        private T LoadRequired<T>(string fileName) where T : class
        {
            string path = Path.Combine(DataDirectory, fileName);
            if (!File.Exists(path))
                throw new FileNotFoundException($"找不到配置文件：{path}", path);

            string json = File.ReadAllText(path, Encoding.UTF8);
            T result = JsonConvert.DeserializeObject<T>(json, JsonSettings);
            return result ?? throw new InvalidDataException($"配置文件内容为空：{fileName}");
        }

        private Dictionary<int, T> LoadDictionary<T>(string fileName)
            where T : MonsterAIEditableRow
        {
            return LoadRequired<Dictionary<int, T>>(fileName);
        }
        #endregion

        #region 怪物操作 (Monster Operations)
        /// <summary>新增一只带最小合法默认配置的怪物。</summary>
        public MonsterEditorProfile AddMonster()
        {
            int tid = NextMonsterTid();
            var character = new MonsterCharacterRow
            {
                TID = tid,
                Name = $"新怪物{tid}",
                MainType = "Monster",
                SubType = $"Monster{tid}",
                Resource = string.Empty,
                HP = 100f,
                AD = 10f,
                DEF = 10f
            };
            var ai = new MonsterAIRow
            {
                TID = tid,
                Name = character.Name,
                viewRange = 10f,
                loseTargetRange = 13f,
                scanInterval = 0.2f,
                runSpeed = 4f,
                turnSpeed = 10f,
                stoppingDistance = 1.2f,
                hitStunDuration = 0.4f,
                speedParameter = "MoveSpeed",
                deadParameter = "IsDead"
            };
            var profile = new MonsterEditorProfile
            {
                Character = character,
                AI = ai
            };

            profile.Abilities.Add(CreateReactionAbility(
                tid,
                $"{character.SubType.ToLowerInvariant()}.hit",
                AbilitySlotIds.HitReaction,
                AbilityExecutorIds.PlayAndWait,
                0.3f));
            profile.Abilities.Add(CreateReactionAbility(
                tid,
                $"{character.SubType.ToLowerInvariant()}.dead",
                AbilitySlotIds.DeathReaction,
                AbilityExecutorIds.Dead,
                0f));

            var idleRule = new MonsterBehaviorEditorRule
            {
                Rule = new MonsterBehaviorRow
                {
                    TID = tid,
                    Action = BehaviorActions.Idle,
                    AbilityId = string.Empty,
                    Slot = string.Empty
                }
            };
            profile.Rules.Add(idleRule);

            _characters[tid] = character;
            Monsters.Add(profile);
            SortMonsters();
            MarkDirty();
            return profile;
        }

        /// <summary>删除一只怪物及其 TID 关联数据。</summary>
        public void DeleteMonster(MonsterEditorProfile profile)
        {
            if (profile == null) return;
            Monsters.Remove(profile);
            _characters.Remove(profile.TID);
            MarkDirty();
        }

        /// <summary>为怪物新增一条空白能力。</summary>
        public MonsterAbilityRow AddAbility(MonsterEditorProfile profile)
        {
            var ability = new MonsterAbilityRow
            {
                TID = profile.TID,
                AbilityId = NextAbilityId(profile),
                Slot = AbilitySlotIds.PrimaryAttack,
                Executor = AbilityExecutorIds.Attack,
                Duration = 0.6f,
                Cooldown = 1f,
                DamageComboIndex = -1,
                MinRange = 0f,
                MaxRange = 2f,
                HitDelay = 0.2f,
                LockMovement = true,
                StopMovementAtEnd = true,
                DamageTakenMultiplier = 1f,
                CountsAsAttack = true
            };
            profile.Abilities.Add(ability);
            MarkDirty();
            return ability;
        }

        /// <summary>为怪物新增一条动画语义映射。</summary>
        public MonsterAnimationRow AddAnimation(MonsterEditorProfile profile)
        {
            var used = new HashSet<string>(
                profile.Animations.Select(row => row.SemanticKey),
                StringComparer.OrdinalIgnoreCase);
            var option = MonsterAIEditorOptions.AnimationSemanticOptions
                .FirstOrDefault(item => !string.IsNullOrEmpty(item.Value)
                    && !used.Contains(item.Value));
            var animation = new MonsterAnimationRow
            {
                TID = profile.TID,
                BindingIndex = profile.Animations.Count,
                SemanticKey = option != null
                    ? option.Value
                    : MonsterAIEditorOptions.AnimationSemanticOptions[1].Value,
                BindingType = nameof(AnimationBindingType.Trigger),
                ParameterName = string.Empty
            };
            profile.Animations.Add(animation);
            MarkDirty();
            return animation;
        }

        /// <summary>新增一条优先级最低的待机规则。</summary>
        public MonsterBehaviorEditorRule AddRule(MonsterEditorProfile profile)
        {
            var rule = new MonsterBehaviorEditorRule
            {
                Rule = new MonsterBehaviorRow
                {
                    TID = profile.TID,
                    Action = BehaviorActions.Idle,
                    AbilityId = string.Empty,
                    Slot = string.Empty
                }
            };
            profile.Rules.Add(rule);
            MarkDirty();
            return rule;
        }

        /// <summary>为行为规则新增一个默认“有目标”条件。</summary>
        public MonsterBehaviorConditionRow AddCondition(
            MonsterEditorProfile profile,
            MonsterBehaviorEditorRule rule)
        {
            var condition = new MonsterBehaviorConditionRow
            {
                TID = profile.TID,
                ConditionId = BehaviorConditions.HasTarget
            };
            rule.Conditions.Add(condition);
            MarkDirty();
            return condition;
        }

        /// <summary>为当前攻击组新增一个攻击判定段。</summary>
        public MonsterAttackRow AddAttack(MonsterEditorProfile profile)
        {
            string group = profile.Character?.SubType ?? string.Empty;
            var groupAttacks = GetAttacks(group).ToList();
            int nextIndex = groupAttacks.Count == 0
                ? 0
                : groupAttacks.Max(row => row.ComboIndex) + 1;
            int nextKey = Attacks.Count == 0
                ? 1
                : Attacks.Max(row => row.RowKey) + 1;
            var attack = new MonsterAttackRow
            {
                RowKey = nextKey,
                ComboGroup = group,
                ComboIndex = nextIndex,
                DmgRate = 1f,
                HitboxType = 2,
                OffsetZ = 1.5f,
                Param1 = 1f,
                Param2 = 2f,
                Knockback = 2f
            };
            Attacks.Add(attack);
            MarkDirty();
            return attack;
        }

        public IEnumerable<MonsterAttackRow> GetAttacks(string comboGroup)
        {
            return Attacks
                .Where(row => string.Equals(
                    row.ComboGroup,
                    comboGroup,
                    StringComparison.OrdinalIgnoreCase))
                .OrderBy(row => row.ComboIndex);
        }

        public MonsterAttackRow FindAttack(MonsterEditorProfile profile, int comboIndex)
        {
            if (profile?.Character == null) return null;
            return GetAttacks(profile.Character.SubType)
                .FirstOrDefault(row => row.ComboIndex == comboIndex);
        }

        public void MarkDirty()
        {
            IsDirty = true;
        }
        #endregion

        #region 保存与整理 (Save & Normalize)
        /// <summary>整理索引并把七张表写回 Data/Data。</summary>
        public void SaveAll()
        {
            Normalize();
            Directory.CreateDirectory(DataDirectory);

            WriteDictionary(CharacterFile,
                _characters.Values.OrderBy(row => row.TID)
                    .ToDictionary(row => row.TID, row => row));
            WriteDictionary(MonsterAIFile,
                Monsters.OrderBy(profile => profile.TID)
                    .ToDictionary(profile => profile.TID, profile => profile.AI));

            var abilities = Monsters.SelectMany(profile => profile.Abilities)
                .Concat(_orphanAbilities)
                .ToList();
            var animations = Monsters.SelectMany(profile => profile.Animations)
                .Concat(_orphanAnimations)
                .ToList();
            var behaviors = Monsters.SelectMany(profile => profile.Rules)
                .Select(rule => rule.Rule)
                .Concat(_orphanBehaviors)
                .ToList();
            var conditions = Monsters.SelectMany(profile => profile.Rules)
                .SelectMany(rule => rule.Conditions)
                .Concat(_orphanConditions)
                .ToList();

            WriteIndexedDictionary(AbilityFile, abilities);
            WriteIndexedDictionary(AnimationFile, animations);
            WriteIndexedDictionary(BehaviorFile, behaviors);
            WriteIndexedDictionary(ConditionFile, conditions);

            var orderedAttacks = Attacks
                .OrderBy(row => row.ComboGroup)
                .ThenBy(row => row.ComboIndex)
                .ToList();
            WriteIndexedDictionary(AttackFile, orderedAttacks);

            IsDirty = false;
        }

        private void Normalize()
        {
            foreach (var profile in Monsters)
            {
                profile.Character.TID = profile.TID;
                profile.Character.MainType = "Monster";
                profile.AI.TID = profile.TID;

                if (string.IsNullOrWhiteSpace(profile.AI.Name))
                    profile.AI.Name = profile.Character.Name;

                for (int i = 0; i < profile.Abilities.Count; i++)
                {
                    profile.Abilities[i].TID = profile.TID;
                }

                for (int i = 0; i < profile.Animations.Count; i++)
                {
                    profile.Animations[i].TID = profile.TID;
                    profile.Animations[i].BindingIndex = i;
                }

                for (int ruleIndex = 0; ruleIndex < profile.Rules.Count; ruleIndex++)
                {
                    var rule = profile.Rules[ruleIndex];
                    rule.Rule.TID = profile.TID;
                    rule.Rule.RuleIndex = ruleIndex;

                    for (int conditionIndex = 0;
                         conditionIndex < rule.Conditions.Count;
                         conditionIndex++)
                    {
                        var condition = rule.Conditions[conditionIndex];
                        condition.TID = profile.TID;
                        condition.RuleIndex = ruleIndex;
                        condition.ConditionIndex = conditionIndex;
                    }
                }
            }

            SortMonsters();
        }

        private void WriteIndexedDictionary<T>(string fileName, List<T> rows)
            where T : MonsterAIEditableRow
        {
            var dictionary = new Dictionary<int, T>();
            for (int i = 0; i < rows.Count; i++)
            {
                rows[i].RowKey = i + 1;
                dictionary[i + 1] = rows[i];
            }
            WriteDictionary(fileName, dictionary);
        }

        private void WriteDictionary<T>(string fileName, Dictionary<int, T> rows)
        {
            string path = Path.Combine(DataDirectory, fileName);
            string json = JsonConvert.SerializeObject(rows, JsonSettings);
            File.WriteAllText(path, json + Environment.NewLine, new UTF8Encoding(false));
        }

        #endregion

        #region 内部工具 (Internal Helpers)
        private void ClearRuntimeData()
        {
            Monsters.Clear();
            Attacks.Clear();
            _characters.Clear();
            _orphanAbilities.Clear();
            _orphanAnimations.Clear();
            _orphanBehaviors.Clear();
            _orphanConditions.Clear();
            IsDirty = false;
        }

        private void SortMonsters()
        {
            Monsters.Sort((left, right) =>
            {
                int tidCompare = left.TID.CompareTo(right.TID);
                if (tidCompare != 0) return tidCompare;
                return string.Compare(
                    left.DisplayName,
                    right.DisplayName,
                    StringComparison.OrdinalIgnoreCase);
            });
        }

        private int NextMonsterTid()
        {
            int maxCharacter = _characters.Count == 0
                ? 2000
                : _characters.Keys.Max();
            int maxMonster = Monsters.Count == 0
                ? 2000
                : Monsters.Max(profile => profile.TID);
            return Math.Max(2000, Math.Max(maxCharacter, maxMonster)) + 1;
        }

        private string NextAbilityId(MonsterEditorProfile profile)
        {
            int index = 1;
            string id;
            do
            {
                id = $"monster{profile.TID}.ability{index}";
                index++;
            }
            while (profile.Abilities.Any(row => string.Equals(
                row.AbilityId,
                id,
                StringComparison.OrdinalIgnoreCase)));
            return id;
        }

        private MonsterAbilityRow CreateReactionAbility(
            int tid,
            string abilityId,
            string slot,
            string executor,
            float duration)
        {
            return new MonsterAbilityRow
            {
                TID = tid,
                AbilityId = abilityId,
                Slot = slot,
                Executor = executor,
                Duration = duration,
                Cooldown = 0f,
                DamageComboIndex = -1,
                MinRange = 0f,
                MaxRange = 999999f,
                LockMovement = true,
                StopMovementAtEnd = true,
                DamageTakenMultiplier = 1f,
                CountsAsAttack = false
            };
        }

        private static MonsterCharacterRow CreateMissingCharacter(int tid, string name)
        {
            return new MonsterCharacterRow
            {
                TID = tid,
                Name = string.IsNullOrWhiteSpace(name) ? $"Monster{tid}" : name,
                MainType = "Monster",
                SubType = $"Monster{tid}",
                Resource = string.Empty,
                HP = 100f,
                AD = 10f,
                DEF = 10f
            };
        }
        #endregion
    }
}
