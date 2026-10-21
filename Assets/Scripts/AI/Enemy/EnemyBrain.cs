using Emberwild.AI.Enemy.Ability;
using Emberwild.AI.Enemy.EAnimator;
using Emberwild.AI.Pathfinding;
using Emberwild.Combat;
using Emberwild.Core.AppLog;
using Emberwild.Core.Events;
using UnityEngine;

namespace Emberwild.AI.Enemy
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CustomNavAgent))]
    [RequireComponent(typeof(EnemyPerception))]
    [RequireComponent(typeof(EnemyEntity))]
    [RequireComponent(typeof(EnemyCombat))]
    public class EnemyBrain : MonoBehaviour, IEnemyAbilityContext
    {
        #region 枚举与定义 (Enums & Types)

        private enum RuntimeMode
        {
            Idle,
            Combat,
            Stagger,
            Dead
        }

        #endregion

        #region 检视面板调试 (Inspector Debug Snapshots)
        // 这些变量仅用于在 Unity 编辑器中实时查看 AI 状态，不参与逻辑运算
        [Header("Runtime snapshot")]
        [SerializeField] private string _debugMode;
        [SerializeField] private string _debugAbility;
        [SerializeField] private string _debugTarget;
        [SerializeField] private float _debugDistance;
        [SerializeField] private float _debugHpRatio;
        [SerializeField] private string _debugSource;
        [SerializeField] private string _debugDecision;
        [SerializeField] private string _debugMove;
        #endregion

        #region 本地核心组件 (Local Components)
        // 构成怪物的“物理躯体”部件
        private CustomNavAgent _agent;              // 脚：负责移动
        private EnemyPerception _perception;     // 眼睛：负责索敌感知
        private EnemyEntity _entity;                     // 躯干：负责血量与基础属性
        private EnemyCombat _combat;               // 手：负责触发攻击事件
        private IEnemyAnimator _animator;         // 皮肤骨骼：负责动画表现
        #endregion

        #region 运行时逻辑与状态 (Runtime Logic & State)
        // 构成怪物的“大脑记忆”与运行状态
        private MonsterAIDefinition _definition;      // 静态基因：外部配置表数据
        private EnemyAbilityRuntime _abilities;      // 技能库：当前可用的技能集
        private BehaviourRuleSelector _behavior;  // 决策树：行为逻辑判断器

        private RuntimeMode _mode = RuntimeMode.Idle; // 当前状态机模式
        private bool _initialized;                                           // 是否初始化完成
        private bool _startupFailed;                                     // 启动是否失败
        private float _staggerEndsAt;                                  // 硬直结束时间戳
        private int _lastTid;                                                  // 当前加载的配置表 TID
        #endregion

        #region 公共属性与接口 (Public API / IEnemyAbilityContext)
        // 对外暴露的只读属性，供其他系统（如技能系统、UI系统）读取状态

        // 1. 黑板与数据
        public EnemyBlackboard BB { get; } = new EnemyBlackboard();
        public EnemyBlackboard Blackboard => BB;
        public CombatAttribute Attribute => _entity != null ? _entity.Attribute : null;
        public MonsterAIDefinition Definition => _definition;

        // 2. 组件暴露
        public CustomNavAgent Agent => _agent;
        public EnemyPerception Perception => _perception;
        public IEnemyAnimator Animator => _animator ?? NullEnemyAnimator.Instance;
        public EnemyEntity Entity => _entity;
        public EnemyCombat Combat => _combat;
        public Transform Transform => transform;

        // 3. 实时状态查询
        public string CurrentAbilityId => _abilities != null ? _abilities.ActiveAbilityId : string.Empty;
        public string CurrentMode => _mode.ToString();
        public float RunSpeed => _definition != null ? _definition.runSpeed : 4f;
        public float HitStunDuration => _definition != null ? _definition.hitStunDuration : 0.4f;

        // 4. 计算属性
        public float NormalizedSpeed
        {
            get
            {
                if (_agent == null || RunSpeed <= 0.01f) return 0f;
                return Mathf.Clamp01(_agent.VelocityMagnitude / RunSpeed);
            }
        }
        #endregion

        #region Unity 生命周期与装配 (Lifecycle & Setup)

        /// <summary>解析本地组件并加载当前 TID 的怪物配置。</summary>
        private void Awake()
        {
            ResolveComponents();
            ResolveAnimator();
            if (_startupFailed) return;

            ConfigureRuntime(_entity != null ? _entity.TID : 0);
        }

        /// <summary>校验运行时依赖并进入待机状态。</summary>
        private void Start()
        {
            if (_initialized) return;
            if (_startupFailed) return;

            if (_agent == null || _perception == null || _entity == null || _combat == null)
            {
                FailStartup("required local components are missing.");
                return;
            }

            if (_definition == null || _abilities == null || _behavior == null)
            {
                FailStartup("monster runtime definition is not configured.");
                return;
            }

            if (!MonsterAIDefinitionValidator.ValidateCombatLinks(
                    _definition,
                    _lastTid,
                    out string combatError))
            {
                FailStartup(combatError);
                return;
            }

            _initialized = true;
            EnterIdle();
        }

        /// <summary>重置池对象并按 TID 重新初始化怪物。</summary>
        public bool PrepareForSpawn(int tid)
        {
            _lastTid = tid;
            if (tid <= 0)
            {
                FailStartup($"invalid spawn TID {tid}.");
                return false;
            }

            _startupFailed = false;
            _initialized = false;
            enabled = true;

            ResolveComponents();
            if (_agent == null || _perception == null || _entity == null || _combat == null)
            {
                FailStartup("required local components are missing during pool spawn.");
                return false;
            }

            ResolveAnimator();
            if (_startupFailed) return false;

            _abilities?.Abort();
            BB.ResetRuntime();
            _staggerEndsAt = 0f;
            ResetDebugSnapshot();

            _agent.enabled = true;
            _perception.enabled = true;
            _combat.enabled = true;
            _entity.enabled = true;

            _agent.ResetForSpawn();
            _perception.ResetForSpawn();
            Animator.ResetForSpawn();

            if (!_entity.PrepareForSpawn(tid))
            {
                FailStartup($"failed to initialize entity data for TID {tid}.");
                return false;
            }

            ConfigureRuntime(tid);
            if (_startupFailed) return false;

            if (!MonsterAIDefinitionValidator.ValidateCombatLinks(
                    _definition,
                    tid,
                    out string combatError))
            {
                FailStartup(combatError);
                return false;
            }

            _initialized = true;
            EnterIdle();
            AppLog.Info("Enemy", $"{name}: pooled spawn ready TID={tid}");
            return true;
        }

        /// <summary>按 TID 向各运行时组件注入怪物配置。</summary>
        private void ConfigureRuntime(int tid)
        {
            _startupFailed = false;
            _lastTid = tid;

            var definition = tid > 0 ? MonsterAIDefinitionRepository.Get(tid) : null;
            if (MonsterAIDefinitionRepository.HasLoadError)
            {
                FailStartup(MonsterAIDefinitionRepository.LoadError);
                return;
            }

            if (definition == null)
            {
                FailStartup($"no MonsterAI external table entry for TID {tid}.");
                return;
            }

            definition.TID = tid;
            if (!MonsterAIDefinitionValidator.Validate(definition, out string error))
            {
                FailStartup(error);
                return;
            }

            if (!Animator.Configure(definition.animation))
            {
                FailStartup("animation profile validation failed.");
                return;
            }

            if (_perception != null)
            {
                _perception.Configure(
                    definition.viewRange,
                    definition.loseTargetRange,
                    definition.scanInterval);
            }

            if (_agent != null)
            {
                _agent.moveSpeed = definition.runSpeed;
                _agent.turnSpeed = definition.turnSpeed;
                _agent.stoppingDistance = definition.stoppingDistance;
            }

            _definition = definition;
            _abilities = new EnemyAbilityRuntime(this, definition.abilities);
            _behavior = new BehaviourRuleSelector(definition);
            _mode = RuntimeMode.Idle;

            AppLog.Info(
                "Enemy",
                $"{name} ready: TID={definition.TID} abilities={definition.abilities.Count} "
                + $"rules={definition.behavior.Count} source=monster-tables");
        }

        /// <summary>获取怪物根节点上的核心组件。</summary>
        private void ResolveComponents()
        {
            if (_agent == null) _agent = GetComponent<CustomNavAgent>();
            if (_perception == null) _perception = GetComponent<EnemyPerception>();
            if (_entity == null) _entity = GetComponent<EnemyEntity>();
            if (_combat == null) _combat = GetComponent<EnemyCombat>();
        }

        /// <summary>查找动画适配器；缺失时将 EnemyAnimator 挂载到角色 Animator 上。</summary>
        private void ResolveAnimator()
        {
            if (_animator != null) return;

            _animator = FindAnimator();
            if (_animator != null) return;

            Animator unityAnimator = GetComponentInChildren<Animator>(true);
            if (unityAnimator == null)
            {
                FailStartup("no Animator exists in this monster hierarchy.");
                return;
            }

            var adapter = unityAnimator.GetComponent<EnemyAnimator>();
            if (adapter == null)
                adapter = unityAnimator.gameObject.AddComponent<EnemyAnimator>();

            _animator = adapter;
        }

        /// <summary>查找怪物根节点上的动画适配器实现。</summary>
        private IEnemyAnimator FindAnimator()
        {
            foreach (var component in GetComponents<MonoBehaviour>())
            {
                if (component is IEnemyAnimator animator) return animator;
            }

            return null;
        }

        /// <summary>清空 Inspector 中的调试快照。</summary>
        private void ResetDebugSnapshot()
        {
            _debugMode = string.Empty;
            _debugAbility = string.Empty;
            _debugTarget = string.Empty;
            _debugDistance = 0f;
            _debugHpRatio = 1f;
            _debugSource = string.Empty;
            _debugDecision = string.Empty;
            _debugMove = string.Empty;
        }

        /// <summary>记录启动错误并停止怪物 AI。</summary>
        private void FailStartup(string reason)
        {
            _startupFailed = true;
            AppLog.Error("Enemy", $"{name} (TID={_lastTid}): startup failed: {reason}");
            enabled = false;
        }

        #endregion

        #region 每帧心跳与大脑决策 (Update & Thinking)

        /// <summary>每帧刷新感知并推进当前怪物行为。</summary>
        private void Update()
        {
            if (!_initialized || _startupFailed || _mode == RuntimeMode.Dead) return;

            _perception.Tick(BB);
            if (_entity != null && _entity.Attribute != null && _entity.MaxHp > 0f)
            {
                BB.HpRatio = _entity.Attribute.CurrentHp / _entity.MaxHp;
            }

            if (IsDead())
            {
                EnterDead();
                return;
            }

            if (_mode == RuntimeMode.Stagger)
            {
                _abilities.Tick(Time.deltaTime);
                if (!_abilities.IsActive && Time.time >= _staggerEndsAt)
                    ResumeAfterInterrupt();
                return;
            }

            if (_abilities.IsActive)
            {
                _abilities.Tick(Time.deltaTime);
                return;
            }

            EvaluateBehavior();
        }

        /// <summary>更新 Inspector 调试快照。</summary>
        private void LateUpdate()
        {
            if (!_initialized) return;

            _debugMode = _mode.ToString();
            _debugAbility = CurrentAbilityId;
            _debugTarget = BB.Target != null ? BB.Target.name : "None";
            _debugDistance = BB.DistanceToTarget;
            _debugHpRatio = BB.HpRatio;
            _debugSource = _definition != null ? "Monster external tables" : "None";
            _debugMove = _agent != null
                ? $"dest={_agent.HasDestination} locked={_agent.IsMotionLocked} "
                    + $"path={_agent.PathNodeCount} velocity={_agent.CurrentVelocity.magnitude:F2}"
                : "none";
        }

        /// <summary>执行行为规则并切换到对应动作。</summary>
        private void EvaluateBehavior()
        {
            var decision = _behavior.Select(BB, _abilities, Attribute, Time.time);

            _debugDecision = string.IsNullOrEmpty(decision.AbilityId)
                ? decision.Action
                : $"{decision.Action}:{decision.AbilityId}";

            switch (decision.Action)
            {
                case BehaviorActions.Ability:
                    if (TryActivateDecision(decision))
                    {
                        _mode = RuntimeMode.Combat;
                        return;
                    }

                    if (BB.Target != null) EnterChase();
                    else EnterIdle();
                    break;

                case BehaviorActions.Chase:
                    EnterChase();
                    break;

                default:
                    EnterIdle();
                    break;
            }
        }

        /// <summary>尝试激活决策指定的能力。</summary>
        private bool TryActivateDecision(BehaviourDecision decision)
        {
            if (!string.IsNullOrWhiteSpace(decision.AbilityId)
                && _abilities.TryActivateAbility(decision.AbilityId))
            {
                return true;
            }

            return !string.IsNullOrWhiteSpace(decision.Slot)
                && _abilities.TryActivateSlot(decision.Slot);
        }

        /// <summary>返回实体是否已经死亡。</summary>
        private bool IsDead()
        {
            return _entity != null
                && _entity.Attribute != null
                && _entity.Attribute.IsDead;
        }

        #endregion

        #region 状态机切换指令 (State Machine Commands)

        /// <summary>切换到待机并停止移动。</summary>
        private void EnterIdle()
        {
            if (_mode == RuntimeMode.Dead || _mode == RuntimeMode.Stagger) return;

            _mode = RuntimeMode.Idle;
            _agent.SetMotionLocked(true);
            Animator.SetMoveSpeed(0f);
        }

        /// <summary>切换到追击并持续更新目标位置。</summary>
        private void EnterChase()
        {
            if (_mode == RuntimeMode.Dead || _mode == RuntimeMode.Stagger) return;

            if (BB.Target == null)
            {
                EnterIdle();
                return;
            }

            _mode = RuntimeMode.Combat;
            _agent.SetMotionLocked(false);
            _agent.SetDestination(BB.Target.position);
            Animator.SetMoveSpeed(NormalizedSpeed);
        }

        /// <summary>中断当前能力并切换到受击硬直。</summary>
        private void EnterStagger()
        {
            if (_mode == RuntimeMode.Dead) return;

            _abilities.Abort();
            _mode = RuntimeMode.Stagger;
            _staggerEndsAt = Time.time + Mathf.Max(0.01f, HitStunDuration);

            if (!_abilities.TryActivateSlot(AbilitySlotIds.HitReaction))
            {
                _agent.SetMotionLocked(true);
                Animator.SetMoveSpeed(0f);
            }
        }

        /// <summary>硬直结束后恢复追击或待机。</summary>
        private void ResumeAfterInterrupt()
        {
            if (IsDead())
            {
                EnterDead();
                return;
            }

            _mode = RuntimeMode.Idle;
            if (BB.Target != null) EnterChase();
            else EnterIdle();
        }

        /// <summary>切换到死亡状态并停止怪物 AI。</summary>
        private void EnterDead()
        {
            if (_mode == RuntimeMode.Dead) return;

            _abilities.Abort();
            _mode = RuntimeMode.Dead;
            _agent.SetMotionLocked(true);
            Animator.SetMoveSpeed(0f);
            Animator.SetDead(true);

            if (!_abilities.TryActivateSlot(AbilitySlotIds.DeathReaction))
            {
                _agent.enabled = false;
                _perception.enabled = false;
                return;
            }

            _perception.enabled = false;
        }

        #endregion

        #region 外部事件响应 (Event Handlers)

        /// <summary>订阅怪物受击和死亡事件。</summary>
        private void OnEnable()
        {
            EventBus.Subscribe<EnemyDamagedEvent>(OnDamaged);
            EventBus.Subscribe<EnemyDiedEvent>(OnDied);
        }

        /// <summary>取消事件订阅并终止当前能力。</summary>
        private void OnDisable()
        {
            EventBus.Unsubscribe<EnemyDamagedEvent>(OnDamaged);
            EventBus.Unsubscribe<EnemyDiedEvent>(OnDied);
            _abilities?.Abort();
        }

        /// <summary>记录受击数据并进入硬直。</summary>
        private void OnDamaged(EnemyDamagedEvent evt)
        {
            if (!_initialized || _startupFailed || _mode == RuntimeMode.Dead) return;
            if (evt.Victim != (ICombatEntity)_entity) return;

            BB.HpRatio = _entity.MaxHp > 0f ? evt.CurrentHp / _entity.MaxHp : 0f;
            BB.LastHitTime = Time.time;
            EnterStagger();
        }

        /// <summary>收到死亡事件后立即进入死亡状态。</summary>
        private void OnDied(EnemyDiedEvent evt)
        {
            if (!_initialized || _startupFailed || _mode == RuntimeMode.Dead) return;
            if (evt.Victim != (ICombatEntity)_entity) return;

            EnterDead();
        }

        #endregion
    }
}
