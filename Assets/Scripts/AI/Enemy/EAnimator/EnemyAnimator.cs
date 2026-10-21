using System;
using System.Collections.Generic;
using Emberwild.AI.Enemy.Ability;
using Emberwild.Core.AppLog;
using UnityEngine;

namespace Emberwild.AI.Enemy.EAnimator
{
    [DisallowMultipleComponent]
    public class EnemyAnimator : MonoBehaviour, IEnemyAnimator
    {
        #region 动画绑定内部类型 (Binding Types)
        private sealed class BindingRuntime
        {
            public AnimationBindingType Type;
            public int Hash;
        }
        #endregion

        #region 动画组件 (Animation Dependencies)
        private Animator _animator;
        #endregion

        #region 动画绑定状态 (Animation Binding State)
        private readonly Dictionary<string, BindingRuntime> _bindings =
            new Dictionary<string, BindingRuntime>(StringComparer.OrdinalIgnoreCase);

        private int _moveSpeedHash;
        private int _deadHash;
        #endregion

        #region 动画配置装配与解析 (Configuration & Resolution)
        /// <summary>获取角色层级中的 Animator。</summary>
        private void Awake()
        {
            ResolveAnimator();
        }

        /// <summary>应用动画配置并验证参数名称和类型。</summary>
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
                if (!TryResolve(
                        profile.speedParameter,
                        AnimatorControllerParameterType.Float,
                        parameters,
                        out _moveSpeedHash))
                {
                    AppLog.Error("Enemy", $"{name}: missing Float parameter '{profile.speedParameter}'.");
                    valid = false;
                }
            }

            if (!string.IsNullOrWhiteSpace(profile.deadParameter))
            {
                if (!TryResolve(
                        profile.deadParameter,
                        AnimatorControllerParameterType.Bool,
                        parameters,
                        out _deadHash))
                {
                    AppLog.Error("Enemy", $"{name}: missing Bool parameter '{profile.deadParameter}'.");
                    valid = false;
                }
            }

            if (profile.bindings != null)
            {
                foreach (var binding in profile.bindings)
                {
                    if (binding == null
                        || string.IsNullOrWhiteSpace(binding.semanticKey)
                        || string.IsNullOrWhiteSpace(binding.parameterName))
                    {
                        continue;
                    }

                    int hash;
                    bool bindingValid;
                    switch (binding.bindingType)
                    {
                        case AnimationBindingType.Trigger:
                            bindingValid = TryResolve(
                                binding.parameterName,
                                AnimatorControllerParameterType.Trigger,
                                parameters,
                                out hash);
                            break;
                        case AnimationBindingType.Bool:
                            bindingValid = TryResolve(
                                binding.parameterName,
                                AnimatorControllerParameterType.Bool,
                                parameters,
                                out hash);
                            break;
                        case AnimationBindingType.Float:
                            bindingValid = TryResolve(
                                binding.parameterName,
                                AnimatorControllerParameterType.Float,
                                parameters,
                                out hash);
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
                            $"{name}: animation binding '{binding.semanticKey}' expected "
                            + $"{binding.bindingType} '{binding.parameterName}' "
                            + "was not found with the expected type.");
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

            AppLog.Info("Enemy",
                $"{name}: animation semantics [{string.Join(", ", _bindings.Keys)}]");
            return valid;
        }

        /// <summary>恢复 Animator 默认状态并清理池复用残留参数。</summary>
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

        /// <summary>在角色层级中查找 Animator。</summary>
        private void ResolveAnimator()
        {
            if (_animator != null) return;
            _animator = GetComponentInChildren<Animator>(true);
        }

        /// <summary>解析指定类型的 Animator 参数哈希。</summary>
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
        #endregion

        #region 动画指令 (Animation Commands)
        /// <summary>播放一次语义动作。</summary>
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

        /// <summary>开启或关闭持续语义状态。</summary>
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

        /// <summary>设置移动动画混合值。</summary>
        public void SetMoveSpeed(float normalizedSpeed)
        {
            if (_animator == null || _moveSpeedHash == 0) return;
            _animator.SetFloat(_moveSpeedHash, Mathf.Clamp01(normalizedSpeed));
        }

        /// <summary>设置死亡动画参数。</summary>
        public void SetDead(bool dead)
        {
            if (_animator == null || _deadHash == 0) return;
            _animator.SetBool(_deadHash, dead);
        }
        #endregion

        #region 编辑器测试入口 (Editor Test Hooks)
#if UNITY_EDITOR
        /// <summary>在 Inspector 菜单中测试近战动画映射。</summary>
        [ContextMenu("Test: Melee")]
        private void TestMelee() => PlaySemanticAction(AnimationSemantics.Melee);

        /// <summary>在 Inspector 菜单中测试受击动画映射。</summary>
        [ContextMenu("Test: Take Damage")]
        private void TestTakeDamage() => PlaySemanticAction(AnimationSemantics.TakeDamage);
#endif
        #endregion
    }
}
