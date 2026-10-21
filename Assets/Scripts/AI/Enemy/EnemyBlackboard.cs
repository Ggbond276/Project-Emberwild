using UnityEngine;

namespace Emberwild.AI.Enemy
{
    public class EnemyBlackboard
    {
        #region 黑板数据 (Blackboard Data)
        // 当前目标与感知结果
        public Transform Target;
        public float DistanceToTarget;
        public bool CanSeeTarget;
        public float HpRatio = 1f;

        // 受击硬直判断时间
        public float LastHitTime = -999f;
        #endregion

        #region 黑板重置 (Blackboard Reset)
        /// <summary>清空目标、感知和受击状态。</summary>
        public void ResetRuntime()
        {
            Target = null;
            DistanceToTarget = float.MaxValue;
            CanSeeTarget = false;
            HpRatio = 1f;
            LastHitTime = -999f;
        }
        #endregion
    }
}
