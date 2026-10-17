using Emberwild.AI.BT;
using Emberwild.AI.Enemy.EnemyAnimator;
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
        [SerializeField] private EnemyEntity _entity; // 数据
        [SerializeField] private EnemyCombat _combat; // 手

        [SerializeField] private EnemyAIConfig _config;          // 配置(每怪一份)
        [SerializeField] private MonoBehaviour _animator;        // 挂 ShellAnimator/WolfAnimator(实现 IEnemyAnimator)

        public CustomNavAgent Agent => _agent;
        public EnemyPerception Perception => _perception;
        public IEnemyAnimator Animator => _animator as IEnemyAnimator ?? NullEnemyAnimator.Instance;
        public EnemyEntity Entity => _entity;
        public EnemyCombat Combat => _combat;


        public float RunSpeed => _config != null ? _config.runSpeed : 4f;
        public float RollSpeed => _config != null ? _config.rollSpeed : 8f;
        public float RollDuration => _config != null ? _config.rollDuration : 1f;
        public float HitStunDuration => _config != null ? _config.hitstunDuration : 0.4f;
        public float HideDuration => _config != null ? _config.hideDuration : 10f;


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
                if (_agent == null || RunSpeed <= 0.01f) return 0f;
                return Mathf.Clamp01(_agent.VelocityMagnitude / RunSpeed);
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
            if (_animator == null) _animator = FindAnimator();
            if (_entity == null) _entity = GetComponent<EnemyEntity>();
            if (_combat == null) _combat = GetComponent<EnemyCombat>();
            if (_config != null)
            {
                // 注入感知/移动/战斗参数(判空,避免缺组件时 Awake 直接崩)
                if (_perception != null) _perception.Configure(_config.viewRange, _config.loseTargetRange, _config.scanInterval);
                if (_agent != null)
                {
                    _agent.moveSpeed = _config.runSpeed;
                    _agent.turnSpeed = _config.turnSpeed;
                    _agent.stoppingDistance = _config.stoppingDistance;
                }
                if (_combat != null) _combat.Configure(_config.attackDuration);
            }

            // 用工厂创建所有状态(新状态只需在工厂注册)
            foreach (EnemyStateType type in Enum.GetValues(typeof(EnemyStateType)))
            {
                var state = EnemyStateFactory.Create(type);
                if (state != null) _states[type] = state;
            }

            // 从配置构建行为树
            if (_config != null)
            {
                _root = BTBuilder.Build(_config.decisionTree);
            }
            else
            {
                AppLog.Error("Enemy", "EnemyBrain: 未配置 EnemyAIConfig");
                enabled = false;
            }
        }

        /// <summary>
        /// 在自身组件中查找第一个实现 IEnemyAnimator 的组件(接口类型无法用 GetComponent 泛型直接查询)。
        /// </summary>
        private MonoBehaviour FindAnimator()
        {
            foreach (var mb in GetComponents<MonoBehaviour>())
            {
                if (mb is IEnemyAnimator) return mb;
            }
            return null;
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
            // 初始状态由行为树第一帧决策,无需手动 ChangeState
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

            // 死亡后短路:不再感知、不再决策、不再执行
            if (_currentStateType == EnemyStateType.Dead) return;

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
        /// 死亡:0 帧立即切 Dead(最高优先级、不可逆)。
        /// 行为树里的 IsDead 条件保留作兜底(万一事件未触发)。
        /// </summary>
        private void OnDied(EnemyDiedEvent evt)
        {
            if (!_initialized) return;
            if (evt.Victim != (ICombatEntity)_entity) return;
            ChangeState(EnemyStateType.Dead);
        }
    }
}
