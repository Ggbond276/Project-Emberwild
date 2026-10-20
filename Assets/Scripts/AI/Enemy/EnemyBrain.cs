using Emberwild.AI.Enemy.Ability;
using Emberwild.AI.Enemy.EAnimator;
using Emberwild.AI.Pathfinding;
using Emberwild.Combat;
using Emberwild.Core.AppLog;
using Emberwild.Core.Events;
using UnityEngine;

namespace Emberwild.AI.Enemy
{
    /// <summary>
    /// 单个怪物的运行时装配入口。
    /// 负责自动解析本地组件、加载怪物定义，并驱动感知、行为规则和能力执行。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CustomNavAgent))]
    [RequireComponent(typeof(EnemyPerception))]
    [RequireComponent(typeof(EnemyEntity))]
    [RequireComponent(typeof(EnemyCombat))]
    public class EnemyBrain : MonoBehaviour, IEnemyAbilityContext
    {
        private enum RuntimeMode
        {
            Idle,
            Combat,
            Stagger,
            Dead
        }

        [Header("Runtime snapshot")]
        [SerializeField] private string _debugMode;
        [SerializeField] private string _debugAbility;
        [SerializeField] private string _debugTarget;
        [SerializeField] private float _debugDistance;
        [SerializeField] private float _debugHpRatio;
        [SerializeField] private string _debugSource;
        [SerializeField] private string _debugDecision;
        [SerializeField] private string _debugMove;

        private CustomNavAgent _agent;
        private EnemyPerception _perception;
        private EnemyEntity _entity;
        private EnemyCombat _combat;
        private IEnemyAnimator _animator;

        private MonsterAIDefinition _definition;
        private EnemyAbilityRuntime _abilities;
        private BehaviourRuleSelector _behavior;
        private RuntimeMode _mode = RuntimeMode.Idle;
        private bool _initialized;
        private bool _startupFailed;
        private float _staggerEndsAt;
        private int _lastTid;

        /// <summary>
        /// 怪物共享黑板，包含目标、距离、血量和最近受击时间。
        /// </summary>
        public EnemyBlackboard BB { get; } = new EnemyBlackboard();

        /// <summary>
        /// 供能力上下文访问的黑板别名。
        /// </summary>
        public EnemyBlackboard Blackboard => BB;

        /// <summary>
        /// 自动解析到的寻路移动组件。
        /// </summary>
        public CustomNavAgent Agent => _agent;

        /// <summary>
        /// 自动解析到的感知组件。
        /// </summary>
        public EnemyPerception Perception => _perception;

        /// <summary>
        /// 动画端口；未配置有效适配器时返回空实现。
        /// </summary>
        public IEnemyAnimator Animator => _animator ?? NullEnemyAnimator.Instance;

        /// <summary>
        /// 怪物实体与伤害数据桥接组件。
        /// </summary>
        public EnemyEntity Entity => _entity;

        /// <summary>
        /// 能力执行器使用的攻击事件发布组件。
        /// </summary>
        public EnemyCombat Combat => _combat;

        /// <summary>
        /// 怪物的当前战斗属性。
        /// </summary>
        public CombatAttribute Attribute => _entity != null ? _entity.Attribute : null;

        /// <summary>
        /// 能力上下文使用的世界变换。
        /// </summary>
        public Transform Transform => transform;

        /// <summary>
        /// 当前加载的统一怪物定义。
        /// </summary>
        public MonsterAIDefinition Definition => _definition;

        /// <summary>
        /// 当前执行中的能力 ID。
        /// </summary>
        public string CurrentAbilityId => _abilities != null ? _abilities.ActiveAbilityId : string.Empty;

        /// <summary>
        /// 当前 HSM 模式名称，用于调试显示。
        /// </summary>
        public string CurrentMode => _mode.ToString();

        /// <summary>
        /// 当前怪物的奔跑速度。
        /// </summary>
        public float RunSpeed => _definition != null ? _definition.runSpeed : 4f;

        /// <summary>
        /// 当前怪物的受击硬直时间。
        /// </summary>
        public float HitStunDuration => _definition != null ? _definition.hitStunDuration : 0.4f;

        /// <summary>
        /// 将当前实际速度归一化到 0 到 1，用于 Animator 移动混合。
        /// </summary>
        public float NormalizedSpeed
        {
            get
            {
                if (_agent == null || RunSpeed <= 0.01f) return 0f;
                return Mathf.Clamp01(_agent.VelocityMagnitude / RunSpeed);
            }
        }

        /// <summary>
        /// 自动补齐本地组件、解析动画端口并准备运行时定义。
        /// </summary>
        private void Awake()
        {
            ResolveComponents();
            ResolveAnimator();
            if (_startupFailed) return;

            ConfigureRuntime(_entity != null ? _entity.TID : 0);
        }

        /// <summary>
        /// 完成必要依赖和战斗数据校验后进入初始待机模式。
        /// </summary>
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
            if (!MonsterAIDefinitionValidator.ValidateCombatLinks(_definition, _lastTid, out string combatError))
            {
                FailStartup(combatError);
                return;
            }

            _initialized = true;
            EnterIdle();
        }

        /// <summary>
        /// 将池对象恢复成一只刚刚出生的怪物，并按 TID 重新装配全部运行时状态。
        /// </summary>
        /// <param name="tid">本次出生要使用的怪物 TID。</param>
        /// <returns>成功完成数据和组件重置时返回 true。</returns>
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
            _debugMode = string.Empty;
            _debugAbility = string.Empty;
            _debugTarget = string.Empty;
            _debugDistance = 0f;
            _debugHpRatio = 1f;
            _debugSource = string.Empty;
            _debugDecision = string.Empty;
            _debugMove = string.Empty;

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

            if (!MonsterAIDefinitionValidator.ValidateCombatLinks(_definition, tid, out string combatError))
            {
                FailStartup(combatError);
                return false;
            }

            _initialized = true;
            EnterIdle();
            AppLog.Info("Enemy", $"{name}: pooled spawn ready TID={tid}");
            return true;
        }

        /// <summary>
        /// 订阅怪物受伤和死亡事件。
        /// </summary>
        private void OnEnable()
        {
            EventBus.Subscribe<EnemyDamagedEvent>(OnDamaged);
            EventBus.Subscribe<EnemyDiedEvent>(OnDied);
        }

        /// <summary>
        /// 取消事件订阅并清理正在执行的能力。
        /// </summary>
        private void OnDisable()
        {
            EventBus.Unsubscribe<EnemyDamagedEvent>(OnDamaged);
            EventBus.Unsubscribe<EnemyDiedEvent>(OnDied);
            _abilities?.Abort();
        }

        /// <summary>
        /// 每帧更新感知、短板中断、能力执行或行为决策。
        /// </summary>
        private void Update()
        {
            if (!_initialized || _startupFailed || _mode == RuntimeMode.Dead) return;

            _perception.Tick(BB);
            if (_entity != null && _entity.Attribute != null && _entity.MaxHp > 0f)
                BB.HpRatio = _entity.Attribute.CurrentHp / _entity.MaxHp;

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

        /// <summary>
        /// 把关键运行状态写入 Inspector，便于现场调试。
        /// </summary>
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

        /// <summary>
        /// 执行行为规则，并把选择结果转换为能力、追击或待机。
        /// </summary>
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

        /// <summary>
        /// 优先尝试直接能力 ID，其次尝试能力槽。
        /// </summary>
        /// <param name="decision">行为选择器返回的本次决策。</param>
        /// <returns>成功激活能力时返回 true。</returns>
        private bool TryActivateDecision(BehaviourDecision decision)
        {
            if (!string.IsNullOrWhiteSpace(decision.AbilityId)
                && _abilities.TryActivateAbility(decision.AbilityId))
                return true;
            return !string.IsNullOrWhiteSpace(decision.Slot)
                && _abilities.TryActivateSlot(decision.Slot);
        }

        /// <summary>
        /// 进入待机模式并锁定不必要的位移。
        /// </summary>
        private void EnterIdle()
        {
            if (_mode == RuntimeMode.Dead || _mode == RuntimeMode.Stagger) return;
            _mode = RuntimeMode.Idle;
            _agent.SetMotionLocked(true);
            Animator.SetMoveSpeed(0f);
        }

        /// <summary>
        /// 进入战斗追击模式并持续更新寻路目的地。
        /// </summary>
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

        /// <summary>
        /// 中断当前能力并进入受击硬直。
        /// </summary>
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

        /// <summary>
        /// 硬直结束后根据是否仍有目标恢复追击或待机。
        /// </summary>
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

        /// <summary>
        /// 进入不可逆死亡模式并执行死亡能力。
        /// </summary>
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

        /// <summary>
        /// 记录伤害数据并触发受击状态。
        /// </summary>
        /// <param name="evt">包含受伤怪物、伤害值和剩余血量的受击事件。</param>
        private void OnDamaged(EnemyDamagedEvent evt)
        {
            if (!_initialized || _startupFailed || _mode == RuntimeMode.Dead) return;
            if (evt.Victim != (ICombatEntity)_entity) return;

            BB.HpRatio = _entity.MaxHp > 0f ? evt.CurrentHp / _entity.MaxHp : 0f;
            BB.LastHitTime = Time.time;
            EnterStagger();
        }

        /// <summary>
        /// 立即切换到死亡状态。
        /// </summary>
        /// <param name="evt">包含死亡怪物战斗实体引用的死亡事件。</param>
        private void OnDied(EnemyDiedEvent evt)
        {
            if (!_initialized || _startupFailed || _mode == RuntimeMode.Dead) return;
            if (evt.Victim != (ICombatEntity)_entity) return;
            EnterDead();
        }

        /// <summary>
        /// 检查实体属性是否已经死亡。
        /// </summary>
        /// <returns>当前生命值状态为死亡时返回 true。</returns>
        private bool IsDead()
        {
            return _entity != null && _entity.Attribute != null && _entity.Attribute.IsDead;
        }

        /// <summary>
        /// 从外联表仓库加载怪物定义，然后配置感知、移动、动画、能力和行为。
        /// </summary>
        /// <param name="tid">要从外联表加载的怪物 TID。</param>
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
                _perception.Configure(definition.viewRange, definition.loseTargetRange, definition.scanInterval);
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

            AppLog.Info("Enemy",
                $"{name} ready: TID={definition.TID} abilities={definition.abilities.Count} "
                + $"rules={definition.behavior.Count} source=monster-tables");
        }

        /// <summary>
        /// 从当前 GameObject 获取所有必需的本地组件。
        /// </summary>
        private void ResolveComponents()
        {
            if (_agent == null) _agent = GetComponent<CustomNavAgent>();
            if (_perception == null) _perception = GetComponent<EnemyPerception>();
            if (_entity == null) _entity = GetComponent<EnemyEntity>();
            if (_combat == null) _combat = GetComponent<EnemyCombat>();
        }

        /// <summary>
        /// 查找动画端口，必要时在真实 Animator 对象上自动挂载适配器。
        /// </summary>
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

        /// <summary>
        /// 查找已存在的 IEnemyAnimator 实现。
        /// </summary>
        /// <returns>找到的动画适配器；没有实现时返回 null。</returns>
        private IEnemyAnimator FindAnimator()
        {
            foreach (var component in GetComponents<MonoBehaviour>())
            {
                if (component is IEnemyAnimator animator) return animator;
            }
            return null;
        }

        /// <summary>
        /// 记录不可恢复错误并停止怪物 AI。
        /// </summary>
        /// <param name="reason">导致启动失败的详细原因。</param>
        private void FailStartup(string reason)
        {
            _startupFailed = true;
            AppLog.Error("Enemy", $"{name} (TID={_lastTid}): startup failed: {reason}");
            enabled = false;
        }

    }
}
