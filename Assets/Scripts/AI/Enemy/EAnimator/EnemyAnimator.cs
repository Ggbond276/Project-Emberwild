using System;
using System.Collections.Generic;
using Emberwild.AI.Enemy.Ability;
using Emberwild.Core.AppLog;
using UnityEngine;

namespace Emberwild.AI.Enemy.EAnimator
{
    /// <summary>
    /// 将稳定的动画语义映射到具体 Animator Controller。
    /// AI 和配置不直接依赖真实参数名，因此不同模型可以通过映射复用同一套能力逻辑。
    /// </summary>
    [DisallowMultipleComponent]
    public class EnemyAnimator : MonoBehaviour, IEnemyAnimator
    {
        private sealed class BindingRuntime
        {
            public AnimationBindingType Type;
            public int Hash;
        }

        private Animator _animator;

        private readonly Dictionary<string, BindingRuntime> _bindings =
            new Dictionary<string, BindingRuntime>(StringComparer.OrdinalIgnoreCase);

        private int _moveSpeedHash;
        private int _deadHash;
        /// <summary>
        /// 组件启动时自动查找动画控制器。
        /// </summary>
        private void Awake()
        {
            ResolveAnimator();
        }

        /// <summary>
        /// 根据怪物动画配置重建语义绑定，并验证每个参数的名称和类型。
        /// 返回 false 表示配置存在不可用参数。
        /// </summary>
        /// <param name="profile">包含移动、死亡和动作语义绑定的动画配置。</param>
        /// <returns>所有必要参数都存在且类型正确时返回 true。</returns>
        public bool Configure(AnimationProfileDefinition profile)
        {
            _bindings.Clear();
            _moveSpeedHash = 0;
            _deadHash = 0;

            ResolveAnimator();
            if (_animator == null)
            {
                AppLog.Error("Enemy", $"{name}: no Animator found.");
                return false;
            }

            if (profile == null)
            {
                AppLog.Error("Enemy", $"{name}: animation profile is missing.");
                return false;
            }
            var parameters = new Dictionary<string, AnimatorControllerParameterType>(
                StringComparer.OrdinalIgnoreCase);
            foreach (var parameter in _animator.parameters)
                parameters[parameter.name] = parameter.type;

            bool valid = true;
            if (!string.IsNullOrWhiteSpace(profile.speedParameter))
            {
                if (!TryResolve(profile.speedParameter, AnimatorControllerParameterType.Float, parameters, out _moveSpeedHash))
                {
                    AppLog.Error("Enemy", $"{name}: missing Float parameter '{profile.speedParameter}'.");
                    valid = false;
                }
            }

            if (!string.IsNullOrWhiteSpace(profile.deadParameter))
            {
                if (!TryResolve(profile.deadParameter, AnimatorControllerParameterType.Bool, parameters, out _deadHash))
                {
                    AppLog.Error("Enemy", $"{name}: missing Bool parameter '{profile.deadParameter}'.");
                    valid = false;
                }
            }

            if (profile.bindings != null)
            {
                foreach (var binding in profile.bindings)
                {
                    if (binding == null || string.IsNullOrWhiteSpace(binding.semanticKey)
                        || string.IsNullOrWhiteSpace(binding.parameterName))
                        continue;

                    int hash;
                    bool bindingValid;
                    switch (binding.bindingType)
                    {
                        case AnimationBindingType.Trigger:
                            bindingValid = TryResolve(binding.parameterName,
                                AnimatorControllerParameterType.Trigger, parameters, out hash);
                            break;
                        case AnimationBindingType.Bool:
                            bindingValid = TryResolve(binding.parameterName,
                                AnimatorControllerParameterType.Bool, parameters, out hash);
                            break;
                        case AnimationBindingType.Float:
                            bindingValid = TryResolve(binding.parameterName,
                                AnimatorControllerParameterType.Float, parameters, out hash);
                            break;
                        case AnimationBindingType.State:
                            hash = Animator.StringToHash(binding.parameterName);
                            bindingValid = _animator.HasState(0, hash);
                            break;
                        default:
                            hash = 0;
                            bindingValid = false;
                            break;
                    }

                    if (!bindingValid)
                    {
                        AppLog.Error("Enemy",
                            $"{name}: animation binding '{binding.semanticKey}' expected {binding.bindingType} "
                            + $"'{binding.parameterName}' was not found with the expected type.");
                        valid = false;
                        continue;
                    }

                    _bindings[binding.semanticKey] = new BindingRuntime
                    {
                        Type = binding.bindingType,
                        Hash = hash
                    };
                }
            }

            AppLog.Info("Enemy", $"{name}: animation semantics [{string.Join(", ", _bindings.Keys)}]");
            return valid;
        }

        /// <summary>
        /// 播放一次性语义动作，例如攻击、受击或警戒。
        /// </summary>
        /// <param name="semanticKey">与具体 Animator 参数解耦的动作语义键。</param>
        public void PlaySemanticAction(string semanticKey)
        {
            if (_animator == null || string.IsNullOrWhiteSpace(semanticKey)) return;
            if (!_bindings.TryGetValue(semanticKey, out var binding))
            {
                AppLog.Error("Enemy", $"{name}: no animation binding for action '{semanticKey}'.");
                return;
            }

            switch (binding.Type)
            {
                case AnimationBindingType.Trigger:
                    _animator.ResetTrigger(binding.Hash);
                    _animator.SetTrigger(binding.Hash);
                    break;
                case AnimationBindingType.Bool:
                    _animator.SetBool(binding.Hash, true);
                    break;
                case AnimationBindingType.State:
                    _animator.CrossFade(binding.Hash, 0.1f, 0);
                    break;
            }
        }

        /// <summary>
        /// 开启或关闭持续语义状态，例如滚动和防御姿态。
        /// </summary>
        /// <param name="semanticKey">与具体 Animator 参数解耦的状态语义键。</param>
        /// <param name="active">true 表示启用该状态；false 表示关闭。</param>
        public void SetSemanticState(string semanticKey, bool active)
        {
            if (_animator == null || string.IsNullOrWhiteSpace(semanticKey)) return;
            if (!_bindings.TryGetValue(semanticKey, out var binding))
            {
                AppLog.Error("Enemy", $"{name}: no animation binding for state '{semanticKey}'.");
                return;
            }

            if (binding.Type == AnimationBindingType.Bool)
                _animator.SetBool(binding.Hash, active);
            else if (binding.Type == AnimationBindingType.State && active)
                _animator.CrossFade(binding.Hash, 0.1f, 0);
        }

        /// <summary>
        /// 设置移动动画混合值，范围会被限制在 0 到 1。
        /// </summary>
        /// <param name="normalizedSpeed">归一化移动速度。</param>
        public void SetMoveSpeed(float normalizedSpeed)
        {
            if (_animator == null || _moveSpeedHash == 0) return;
            _animator.SetFloat(_moveSpeedHash, Mathf.Clamp01(normalizedSpeed));
        }

        /// <summary>
        /// 设置 Animator 的死亡布尔参数。
        /// </summary>
        /// <param name="dead">true 表示进入死亡动画；false 表示恢复正常状态。</param>
        public void SetDead(bool dead)
        {
            if (_animator == null || _deadHash == 0) return;
            _animator.SetBool(_deadHash, dead);
        }

        /// <summary>
        /// 恢复控制器默认状态，并清除池复用可能残留的 Trigger、Bool 和 Float 参数。
        /// </summary>
        public void ResetForSpawn()
        {
            ResolveAnimator();
            if (_animator == null) return;

            _animator.Rebind();
            _animator.Update(0f);

            foreach (BindingRuntime binding in _bindings.Values)
            {
                switch (binding.Type)
                {
                    case AnimationBindingType.Trigger:
                        _animator.ResetTrigger(binding.Hash);
                        break;
                    case AnimationBindingType.Bool:
                        _animator.SetBool(binding.Hash, false);
                        break;
                    case AnimationBindingType.Float:
                        _animator.SetFloat(binding.Hash, 0f);
                        break;
                }
            }

            if (_moveSpeedHash != 0)
                _animator.SetFloat(_moveSpeedHash, 0f);
            if (_deadHash != 0)
                _animator.SetBool(_deadHash, false);
        }

        /// <summary>
        /// 在当前怪物层级中自动查找 Animator。
        /// </summary>
        private void ResolveAnimator()
        {
            if (_animator != null) return;
            _animator = GetComponentInChildren<Animator>(true);
        }

        /// <summary>
        /// 根据预期类型解析 Animator 参数哈希。
        /// </summary>
        /// <param name="parameterName">动画配置中填写的真实参数名称。</param>
        /// <param name="expected">该语义要求的 Animator 参数类型。</param>
        /// <param name="parameters">当前 Animator Controller 的全部参数类型表。</param>
        /// <param name="hash">解析成功时输出的 Animator 参数哈希。</param>
        /// <returns>参数存在且类型匹配时返回 true。</returns>
        private static bool TryResolve(
            string parameterName,
            AnimatorControllerParameterType expected,
            Dictionary<string, AnimatorControllerParameterType> parameters,
            out int hash)
        {
            hash = 0;
            if (!parameters.TryGetValue(parameterName, out var actual) || actual != expected)
                return false;
            hash = Animator.StringToHash(parameterName);
            return true;
        }

#if UNITY_EDITOR
        /// <summary>
        /// 在 Inspector 右键菜单中测试近战动作语义映射。
        /// </summary>
        [ContextMenu("Test: Melee")]
        private void TestMelee() => PlaySemanticAction(AnimationSemantics.Melee);

        /// <summary>
        /// 在 Inspector 右键菜单中测试受击动作语义映射。
        /// </summary>
        [ContextMenu("Test: Take Damage")]
        private void TestTakeDamage() => PlaySemanticAction(AnimationSemantics.TakeDamage);
#endif
    }
}
