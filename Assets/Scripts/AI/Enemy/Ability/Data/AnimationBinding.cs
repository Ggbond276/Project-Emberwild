using System;

namespace Emberwild.AI.Enemy.Ability
{
    [Serializable]
    public class AnimationBinding
    {
        #region 绑定数据 (Binding Data)
        public string semanticKey;
        public AnimationBindingType bindingType;
        public string parameterName;
        #endregion
    }
}
