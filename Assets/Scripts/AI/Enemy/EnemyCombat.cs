using Emberwild.AI.Enemy.EAnimator;
using Emberwild.Core.AppLog;
using Emberwild.Core.Events;
using UnityEngine;

namespace Emberwild.AI.Enemy
{
    /// <summary>
    /// 怪物攻击执行层：按动作发作攻击事件，维护攻击窗口。
    /// </summary>
    [DisallowMultipleComponent]
    public class EnemyCombat : MonoBehaviour
    {
        [Header("组件引用")]
        [SerializeField] private MonoBehaviour _animator;
        [SerializeField] private EnemyEntity _entity;

        [Header("攻击参数")]
        [Tooltip("默认攻击窗口时长，会被 EnemyAIConfig.attackDuration 覆盖")]
        [SerializeField] private float _attackDuration = 0.6f;

        private float _attackEndTime;
        public bool IsAttacking => Time.time < _attackEndTime;

        private IEnemyAnimator Animator => _animator as IEnemyAnimator;

        private EnemyAIConfig _config;

        private void Awake()
        {
            if (_animator == null) _animator = FindAnimator();
            if (_entity == null) _entity = GetComponent<EnemyEntity>();
        }

        /// <summary>注入配置（攻击窗口 + 招式段号映射）。</summary>
        public void Configure(EnemyAIConfig config)
        {
            _config = config;

            if (config != null && config.attackDuration > 0f)
                _attackDuration = config.attackDuration;
        }

        /// <summary>发起攻击，返回是否成功发出。</summary>
        public bool DoAttack(EnemyAction action = EnemyAction.Melee, float duration = -1f)
        {
            if (IsAttacking) return false;

            float window = duration > 0f ? duration : _attackDuration;
            _attackEndTime = Time.time + window;

            Animator?.PlayAction(action);

            int comboIndex = ResolveComboIndex(action);
            EventBus.Publish(new CharacterAttackHitEvent(_entity, comboIndex));

            AppLog.Info("Combat", $"{name} 攻击 [{action}] 段={comboIndex} 窗口={window}s");
            return true;
        }

        /// <summary>发起远程攻击，返回是否成功发出。</summary>
        public bool DoRangedAttack(Vector3 targetPos, EnemyAction action = EnemyAction.Ranged)
        {
            if (IsAttacking) return false;

            _attackEndTime = Time.time + _attackDuration;

            Animator?.PlayAction(action);

            int comboIndex = ResolveComboIndex(action);
            EventBus.Publish(new CharacterAttackHitEvent(_entity, comboIndex, targetPos));

            AppLog.Info("Combat", $"{name} 远程 [{action}] 段={comboIndex} target={targetPos}");
            return true;
        }

        /// <summary>
        /// 从配置取该动作的攻击表段号。
        /// 配置为空列表时按单段怪处理，静默用段 0；配置了但缺该动作则警告。
        /// </summary>
        private int ResolveComboIndex(EnemyAction action)
        {
            if (_config == null)
            {
                AppLog.Warning("Combat", $"{name} 未注入 EnemyAIConfig，[{action}] 使用段 0");
                return 0;
            }

            if (_config.attackActions == null || _config.attackActions.Count == 0)
                return 0;

            int index = _config.GetComboIndex(action);
            if (index >= 0) return index;

            AppLog.Warning("Combat", $"{name} 配置里缺少 [{action}] 的段号映射，使用段 0");
            return 0;
        }

        /// <summary>查找实现 IEnemyAnimator 的组件。</summary>
        private MonoBehaviour FindAnimator()
        {
            foreach (var mb in GetComponents<MonoBehaviour>())
            {
                if (mb is IEnemyAnimator) return mb;
            }
            return null;
        }
    }
}
