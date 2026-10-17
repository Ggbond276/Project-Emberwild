using Emberwild.AI.BT;
using Emberwild.AI.Enemy.State;
using Emberwild.AI.Pathfinding;
using Emberwild.Combat;
using Emberwild.Core.AppLog;
using Emberwild.Core.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Emberwild.AI.Enemy
{
    /// <summary>
    /// 怪物决策核心 -- 唯一的状态切换入口
    /// 
    /// 职责：
    /// - 持有所有状态实例(字典)
    /// - 每帧驱动感知 -> 驱动当前状态
    /// - 提供 ChangeState() 给状态类调用
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CustomNavAgent))]
    public class EnemyBrain : MonoBehaviour
    {
        [Header("组件引用")]
        [SerializeField] private CustomNavAgent _agent; // 腿
        [SerializeField] private EnemyPerception _perception; // 眼睛
        [SerializeField] private EnemyAnimator _animator; // 动画
        [SerializeField] private EnemyEntity _entity; // 数据
        [SerializeField] private EnemyCombat _combat; // 手


        public CustomNavAgent Agent => _agent;
        public EnemyPerception Perception => _perception;
        public EnemyAnimator Animator => _animator;
        public EnemyEntity Entity => _entity;
        public EnemyCombat Combat => _combat;


        [Header("决策参数")]
        [Tooltip("奔跑速度。必须与 CustomNavAgent.moveSpeed 一致,用于把实际速度归一化成 0~1")]
        [SerializeField] private float _runSpeed = 4f;
        [Tooltip("近战攻击触发距离(米)")]
        [SerializeField] private float _meleeRange = 1.6f;
        [Tooltip("受击硬直时长(秒)")]
        [SerializeField] private float _hitstunDuration = 0.4f;

        [Header("滚动参数")]
        [Tooltip("滚动冲刺的速度(米/秒)")]
        [SerializeField] private float _rollSpeed = 8f;
        [Tooltip("滚动冲刺的持续时长(秒)")]
        [SerializeField] private float _rollDuration = 1f;
        [Tooltip("滚动冷却(秒)")]
        [SerializeField] private float _rollCooldown = 4f;
        [Tooltip("触发滚动的最小距离(米)")]
        [SerializeField] private float _rollMinRange = 3f;
        [Tooltip("触发滚动的最大距离(米)")]
        [SerializeField] private float _rollMaxRange = 7f;
        
        [Header("远程攻击参数")]
        [Tooltip("远程攻击最小距离(米)")]
        [SerializeField] private float _rangedMinRange = 4f;
        [Tooltip("远程攻击最大距离(米)")]
        [SerializeField] private float _rangedMaxRange = 8f;
        [Tooltip("远程攻击冷却(秒)")]
        [SerializeField] private float _attackCooldown = 2f;

        [Header("缩壳防御参数")]
        [Tooltip("触发缩壳的血量比例阈值(0~1)")]
        [SerializeField] private float _hideHpThreshold = 0.3f;
        [Tooltip("缩壳持续时长(秒)")]
        [SerializeField] private float _hideDuration = 10f;

        public float RunSpeed => _runSpeed;
        public float MeleeRange => _meleeRange;
        public float HitStunDuration => _hitstunDuration;
         public float RollSpeed => _rollSpeed;
        public float RollDuration => _rollDuration;
        public float RollCooldown => _rollCooldown;
        public float RollMinRange => _rollMinRange;
        public float RollMaxRange => _rollMaxRange;
        public float RangedMinRange => _rangedMinRange;
        public float RangedMaxRange => _rangedMaxRange;
        public float AttackCooldown => _attackCooldown;
        public float HideHpThreshold => _hideHpThreshold;
        public float HideDuration => _hideDuration;


        [Tooltip("出生时进入的状态")]
        [SerializeField] private EnemyStateType _defaultState = EnemyStateType.Idle;
        private IEnemyState _currentState;
        private EnemyStateType _currentStateType;
        private bool _initialized;
        public EnemyBlackboard BB { get; } = new EnemyBlackboard();
        

        /// <summary>
        /// 把当前速度归一化为 0 - 1
        /// </summary>
        public float NormalizedSpeed
        {
            get
            {
                if (_agent == null || _runSpeed <= 0.01f) return 0f;
                return Mathf.Clamp01(_agent.VelocityMagnitude / _runSpeed);
            }
        }

        /// <summary>
        /// 状态字典
        /// </summary>
        private readonly Dictionary<EnemyStateType, IEnemyState> _states = new Dictionary<EnemyStateType, IEnemyState>();

        /// <summary>
        /// 行为树根节点 
        /// </summary>
        private BTNode _root;

        private void Awake()
        {
            // 自动补齐引用(减少 Inspector 手拖出错)
            if (_agent == null) _agent = GetComponent<CustomNavAgent>();
            if (_perception == null) _perception = GetComponent<EnemyPerception>();
            if (_animator == null) _animator = GetComponent<EnemyAnimator>();
            if (_entity == null) _entity = GetComponent<EnemyEntity>();
            if (_combat == null) _combat = GetComponent<EnemyCombat>();

            // 注册状态(新增状态就在这里加一行)
            _states[EnemyStateType.Idle] = new IdleState();
            _states[EnemyStateType.Chase] = new ChaseState();
            _states[EnemyStateType.AttackMelee] = new AttackMeleeState();
            _states[EnemyStateType.Hit] = new HitState();
            _states[EnemyStateType.Dead] = new DeadState();
            _states[EnemyStateType.RollChase] = new RollChaseState();
            _states[EnemyStateType.AttackRanged] = new AttackRangedState();
            _states[EnemyStateType.Hide] = new HideState();


            _root = BuildTree();
        }

        private void Start()
        {
            if (_agent == null)
            {
                AppLog.Error("Enemy", "EnemyBrain: 缺少 CustomNavAgent, 无法移动");
                enabled = false;
                return;
            }
            if (_perception == null)
            {
                AppLog.Error("Enemy", "EnemyBrain: 缺少 EnemyPerception, 螃蟹将无法发现玩家");
                enabled = false;
                return;
            }

            _initialized = true;
            ChangeState(_defaultState);
        }

        private void OnEnable()
        {
            EventBus.Subscribe<EnemyDamagedEvent>(OnDamaged);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<EnemyDamagedEvent>(OnDamaged);
        }

        private void Update()
        {
            if (!_initialized) return;
            _perception.Tick(BB); // 1. 感知改写通用数据黑板
            _root.Evaluate(this); // 2.行为树做决策
            _currentState?.OnUpdate(this); // 3.状态执行
        }

        public void ChangeState(EnemyStateType type)
        {
            if (_currentState != null && _currentStateType == type) return;

            if( !_states.TryGetValue(type, out var next))
            {
                AppLog.Error("Enemy", $"EnemyBrain: 状态 {type} 未注册");
                return;
            }

            _currentState?.OnExit(this);
            _currentStateType = type;
            _currentState = next;
            _currentState.OnEnter(this);

            AppLog.Info("Enemy", $"{name} → {type}");
        }

        /// <summary>
        /// 受击：打断当前状态 切换到Hit
        /// </summary>
        /// <param name="evt"></param>
        private void OnDamaged(EnemyDamagedEvent evt)
        {
            if (!_initialized) return;
            if (evt.Victim != (ICombatEntity)_entity) return;

            BB.HpRatio = Entity != null ? evt.CurrentHp / Entity.MaxHp : 1f;
            BB.LastHitTime = Time.time;
        }

        /// <summary>
        /// 构建决策树:8 个状态的优先级(从上到下,命中即停)。
        /// 顺序:死亡 > 受击 > 缩壳 > 近战 > 滚动 > 远程 > 追击 > 待机。
        /// </summary>
        private BTNode BuildTree()
        {
            return new Selector(
                // 1. 死亡(最高优先级,终止)
                new Sequence(
                    new Condition(b => b.Entity != null && b.Entity.Attribute != null && b.Entity.Attribute.IsDead),
                    new ActionNode(EnemyStateType.Dead)
                ),
                // 2. 受击硬直
                new Sequence(
                    new Condition(b => Time.time - b.BB.LastHitTime < b.HitStunDuration),
                    new ActionNode(EnemyStateType.Hit)
                ),
                // 3. 缩壳(残血)
                new Sequence(
                    new Condition(b => b.BB.CanSeeTarget && b.BB.HpRatio < b.HideHpThreshold),
                    new ActionNode(EnemyStateType.Hide)
                ),
                // 4. 近战
                new Sequence(
                    new Condition(b => b.BB.CanSeeTarget && b.BB.DistanceToTarget <= b.MeleeRange),
                    new ActionNode(EnemyStateType.AttackMelee)
                ),
                // 5. 滚动冲刺(中距离 + 冷却)
                new Sequence(
                    new Condition(b => b.BB.CanSeeTarget
                        && b.BB.DistanceToTarget >= b.RollMinRange && b.BB.DistanceToTarget <= b.RollMaxRange
                        && Time.time - b.BB.LastRollTime >= b.RollCooldown),
                    new ActionNode(EnemyStateType.RollChase)
                ),
                // 6. 远程攻击(中远距离 + 冷却)
                new Sequence(
                    new Condition(b => b.BB.CanSeeTarget
                        && b.BB.DistanceToTarget >= b.RangedMinRange && b.BB.DistanceToTarget <= b.RangedMaxRange
                        && Time.time - b.BB.LastAttackTime >= b.AttackCooldown),
                    new ActionNode(EnemyStateType.AttackRanged)
                ),
                // 7. 追击
                new Sequence(
                    new Condition(b => b.BB.CanSeeTarget),
                    new ActionNode(EnemyStateType.Chase)
                ),
                // 8. 待机(兜底)
                new ActionNode(EnemyStateType.Idle)
            );
        }
    }
}
