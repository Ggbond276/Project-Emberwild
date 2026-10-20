using System;

namespace Emberwild.AI.Enemy.Ability
{
    /// <summary>
    /// 单条动画语义到真实 Animator 参数或状态的映射。
    /// </summary>
    [Serializable]
    public class AnimationBinding
    {
        public string semanticKey;
        public AnimationBindingType bindingType;
        public string parameterName;
    }
}
