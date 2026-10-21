using System.Collections;
using UnityEngine;

namespace Emberwild.Core.Pooling
{
    /// <summary>
    /// 池化实例的运行时标记，记录来源 Prefab 和所属池，防止重复回收。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PooledObject : MonoBehaviour
    {
        /// <summary>
        /// 当前实例正在等待执行的延迟回收协程。
        /// </summary>
        private Coroutine _releaseRoutine;

        /// <summary>
        /// 创建并负责管理该实例的对象池管理中心。
        /// </summary>
        internal ObjectPoolManager Owner { get; private set; }

        /// <summary>
        /// 生成该实例时使用的原始 Prefab。
        /// </summary>
        internal GameObject SourcePrefab { get; private set; }

        /// <summary>
        /// 标记该实例当前是否位于空闲池中。
        /// </summary>
        internal bool IsInPool { get; private set; }

        /// <summary>
        /// 绑定该实例所属的管理器和原始 Prefab。
        /// </summary>
        /// <param name="owner">负责管理该实例的对象池管理中心。</param>
        /// <param name="sourcePrefab">生成该实例时使用的原始 Prefab。</param>
        internal void Bind(ObjectPoolManager owner, GameObject sourcePrefab)
        {
            Owner = owner;
            SourcePrefab = sourcePrefab;
        }

        /// <summary>
        /// 记录实例已经离开对象池并进入活动状态。
        /// </summary>
        internal void MarkTaken()
        {
            IsInPool = false;
        }

        /// <summary>
        /// 记录实例已经停用并回到对象池。
        /// </summary>
        internal void MarkReturned()
        {
            IsInPool = true;
        }

        /// <summary>
        /// 安排在指定秒数后自动回池，供命中特效等短生命周期对象使用。
        /// </summary>
        /// <param name="delay">从调用开始计算的等待秒数。</param>
        internal void ScheduleRelease(float delay)
        {
            CancelRelease();
            if (delay <= 0f)
            {
                Owner?.Despawn(gameObject);
                return;
            }

            _releaseRoutine = StartCoroutine(ReleaseAfter(delay));
        }

        /// <summary>
        /// 取消尚未执行的延迟回收。
        /// </summary>
        internal void CancelRelease()
        {
            if (_releaseRoutine == null) return;
            StopCoroutine(_releaseRoutine);
            _releaseRoutine = null;
        }

        /// <summary>
        /// 等待指定时间后通知对象池回收当前实例。
        /// </summary>
        /// <param name="delay">等待的秒数。</param>
        /// <returns>供 Unity 协程调度器使用的迭代器。</returns>
        private IEnumerator ReleaseAfter(float delay)
        {
            yield return new WaitForSeconds(delay);
            _releaseRoutine = null;
            Owner?.Despawn(gameObject);
        }
    }
}
