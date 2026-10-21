using System;
using System.Collections.Generic;

namespace Emberwild.AI.Enemy.Ability
{
    [Serializable]
    public class AnimationProfileDefinition
    {
        #region 动画配置数据 (Animation Profile Data)
        // Animator 根参数与语义绑定
        public string speedParameter = "MoveSpeed";
        public string deadParameter = "IsDead";
        public List<AnimationBinding> bindings = new List<AnimationBinding>();
        #endregion
    }
}
