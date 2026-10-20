using System;
using System.Collections.Generic;

namespace Emberwild.AI.Enemy.Ability
{
    /// <summary>
    /// 一只怪物使用的 Animator 映射表。
    /// </summary>
    [Serializable]
    public class AnimationProfileDefinition
    {
        public string speedParameter = "MoveSpeed";
        public string deadParameter = "IsDead";
        public List<AnimationBinding> bindings = new List<AnimationBinding>();
    }
}
