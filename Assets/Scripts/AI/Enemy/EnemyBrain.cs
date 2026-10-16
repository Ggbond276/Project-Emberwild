using Emberwild.AI.Enemy.State;
using Emberwild.AI.Pathfinding;
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

        public float RunSpeed => _runSpeed;
        public float MeleeRange => _meleeRange;
        public float HitStunDuration => _hitstunDuration;


        [Tooltip("出生时进入的状态")]
        [SerializeField] private EnemyStateType _defaultState = EnemyStateType.Idle;


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

        private readonly Dictionary<EnemyStateType, IEnemyState> _states = new Dictionary<EnemyStateType, IEnemyState>();

        private IEnemyState _currentState;
        private EnemyStateType _currentStateType;
        private bool _initialized;


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
            EventBus.Subscribe<EnemyDiedEvent>(OnDied);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<EnemyDamagedEvent>(OnDamaged);
            EventBus.Unsubscribe<EnemyDiedEvent>(OnDied);
        }

        private void Update()
        {
            if (!_initialized) return;
            _perception.Tick(BB);

            _currentState?.OnUpdate(this);
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
            ChangeState(EnemyStateType.Hit);
        }

        /// <summary>
        /// 死亡：最高有点级别 切到Dead
        /// </summary>
        /// <param name="evt"></param>
        private void OnDied(EnemyDiedEvent evt)
        {
            if (!_initialized) return;
            ChangeState(EnemyStateType.Dead);
        }
    }
}
