using System;

namespace Emberwild.Define
{
    /// <summary>动画语义到真实 Animator 参数的一条绑定。</summary>
    [Serializable]
    public class MonsterAnimationBindingDefine
    {
        public int TID;
        public int BindingIndex;
        public string SemanticKey;
        public string BindingType;
        public string ParameterName;
    }
}
