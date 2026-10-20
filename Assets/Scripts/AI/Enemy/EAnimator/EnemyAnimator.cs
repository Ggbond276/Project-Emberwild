using Emberwild.AI.Enemy.EAnimator;
using Emberwild.Core.AppLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Emberwild.AI.Enemy.EAnimator
{
    /// <summary>
    /// 通用动画驱动，服务于所有的怪物
    /// 
    /// 要求：
    /// - 一定要按照动作的命名契约对动作进行归类命名
    /// - 如果要新增动作类型需要修改三处地方
    /// - EnemyAction枚举
    /// - EnemyStateFlag枚举
    /// - EnemyAnimator中的枚举映射
    /// - 并且动画状态机的参数一定要与这三份表格中的参数一一对应
    /// - 只要做到以上几点 本动画播放器就是通用动画播放器
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Animator))]
    public class EnemyAnimator : MonoBehaviour, IEnemyAnimator
    {

        [SerializeField] private Animator _animator;

        /// <summary>
        /// 动作 → Animator 参数名，下标与 EnemyAction 顺序一致。
        /// </summary>
        private static readonly string[] ActionParameters =
        {
            "Melee",        // Melee
            "Heavy",        // Heavy
            "Pounce",       // Pounce
            "Leap",         // Leap
            "RangedAttack", // Ranged
            "Spell",        // Spell
            "Alert",        // Alert
            "TakeDamage",   // TakeDamage
        };

        /// <summary>
        /// 状态 → Animator 参数名，下标与 EnemyStateFlag 顺序一致。
        /// </summary>
        private static readonly string[] StateParameters =
        {
            "Dash",     // Dash
            "Stance",   // Stance
        };

        private const string MoveSpeedParameter = "MoveSpeed";
        private const string DeadParameter = "IsDead";



        private int _moveSpeedHash;
        private int _deadHash;
        private int[] _actionHashes;
        private int[] _stateHashes;


        private void Awake()
        {

            // 1. 判断动画播放器是否存在
            if (_animator == null) _animator = GetComponentInChildren<Animator>(true);
            if (_animator == null)
            {
                AppLog.Warning("Enemy", name + "：找不到 Animator");
                return;
            }

            // 2.检查映射表长度是否与枚举数量一致
            AssertContractSize();

            // 3.收集当前怪物Animator实际拥有的参数名
            HashSet<string> actualParams = new HashSet<string>();
            foreach (var p in _animator.parameters)
            {
                actualParams.Add(p.name);
            }

            // 4.根据能力填充Hash表
            _moveSpeedHash = actualParams.Contains(MoveSpeedParameter) ? Animator.StringToHash(MoveSpeedParameter) : 0;
            _deadHash = actualParams.Contains(DeadParameter) ? Animator.StringToHash(DeadParameter) : 0;
           
            _actionHashes = new int[ActionParameters.Length];
            for (int i = 0; i < ActionParameters.Length; i++)
            {
                _actionHashes[i] = actualParams.Contains(ActionParameters[i])
                    ? Animator.StringToHash(ActionParameters[i])
                    : 0;
            }

            _stateHashes = new int[StateParameters.Length];
            for (int i = 0; i < StateParameters.Length; i++)
            {
                _stateHashes[i] = actualParams.Contains(StateParameters[i])
                    ? Animator.StringToHash(StateParameters[i])
                    : 0;
            }

            // 3. 打印日志（把临时名册传过去）
            LogCapabilities(actualParams);
        }

        /// <summary>
        /// 检查映射表长度与枚举数量是否一致。
        /// </summary>
        private static void AssertContractSize()
        {
            int actionCount = System.Enum.GetValues(typeof(EnemyAction)).Length;
            int stateCount = System.Enum.GetValues(typeof(EnemyStateFlag)).Length;

            if (ActionParameters.Length != actionCount)
                AppLog.Error("Enemy", "ActionParameters 长度 " + ActionParameters.Length
                    + " 与 EnemyAction 数量 " + actionCount + " 不一致");

            if (StateParameters.Length != stateCount)
                AppLog.Error("Enemy", "StateParameters 长度 " + StateParameters.Length
                    + " 与 EnemyStateFlag 数量 " + stateCount + " 不一致");
        }

        /// <summary>
        /// 打印这只怪拥有哪些能力。
        /// </summary>
        private void LogCapabilities(HashSet<string> actualParams)
        {
            if (_moveSpeedHash == 0) AppLog.Warning("Enemy", name + "：缺少 MoveSpeed");
            if (_deadHash == 0) AppLog.Warning("Enemy", name + "：缺少 IsDead");

            var actions = new List<string>();
            for (int i = 0; i < ActionParameters.Length; i++)
                if (actualParams.Contains(ActionParameters[i]))
                    actions.Add(((EnemyAction)i).ToString());

            var states = new List<string>();
            for (int i = 0; i < StateParameters.Length; i++)
                if (actualParams.Contains(StateParameters[i]))
                    states.Add(((EnemyStateFlag)i).ToString());

            AppLog.Info("Enemy", name + "：动作 [" + string.Join(", ", actions)
                + "]  状态 [" + string.Join(", ", states) + "]");
        }




        /// <summary>
        /// 设置移动强度，0~1。
        /// </summary>
        public void SetMoveSpeed(float normalizedSpeed)
        {
            // 如果 Hash 为 0，说明预加载时发现没有这个参数，直接静默跳过
            if (_animator == null || _moveSpeedHash == 0) return;
            _animator.SetFloat(_moveSpeedHash, Mathf.Clamp01(normalizedSpeed));
        }

        /// <summary>
        /// 播放一次性动作。该怪没有此能力时静默跳过。
        /// </summary>
        public void PlayAction(EnemyAction action)
        {
            if (_animator == null) return;

            int index = (int)action;
            if (index < 0 || index >= _actionHashes.Length) return;

            // O(1) 直接从预加载数组中抓取 Hash
            int hash = _actionHashes[index];
            if (hash == 0) return; // 0 代表美术没配这个动作，静默拦截防报错

            _animator.ResetTrigger(hash);
            _animator.SetTrigger(hash);
        }

        /// <summary>
        /// 开关可逆状态。该怪没有此能力时静默跳过。
        /// </summary>
        public void SetState(EnemyStateFlag state, bool active)
        {
            if (_animator == null) return;

            int index = (int)state;
            if (index < 0 || index >= _stateHashes.Length) return;

            int hash = _stateHashes[index];
            if (hash == 0) return;

            _animator.SetBool(hash, active);
        }

        /// <summary>
        /// 设置死亡状态。
        /// </summary>
        public void SetDead(bool dead)
        {
            if (_animator == null || _deadHash == 0) return;
            _animator.SetBool(_deadHash, dead);
        }


# if UNITY_EDITOR

        [ContextMenu("测试：近战")]
        private void TestMelee() => PlayAction(EnemyAction.Melee);

        [ContextMenu("测试：受击")]
        private void TestTakeDamage() => PlayAction(EnemyAction.TakeDamage);

        [ContextMenu("测试：冲刺开")]
        private void TestDashOn() => SetState(EnemyStateFlag.Dash, true);

        [ContextMenu("测试：冲刺关")]
        private void TestDashOff() => SetState(EnemyStateFlag.Dash, false);

# endif
    }
}
