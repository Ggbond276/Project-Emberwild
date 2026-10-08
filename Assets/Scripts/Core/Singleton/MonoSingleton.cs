using UnityEngine;

namespace Emberwild.Core.Singleton
{
    /// <summary>
    /// Unity 场景型单例基类。
    /// 继承 MonoBehaviour,必须挂载在场景中的 GameObject 上。
    /// 全局唯一,跨场景 DontDestroyOnLoad。
    /// 仅提供最基础的 Instance 访问,不引入额外生命周期或管理机制。
    /// </summary>
    public abstract class MonoSingleton<T> : MonoBehaviour where T : MonoSingleton<T>
    {
        private static T _instance;

        public static T Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<T>();
                }
                return _instance;
            }
        }

        protected virtual void Awake()
        {
            if (_instance == null)
            {
                _instance = this as T;
                DontDestroyOnLoad(gameObject);
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }
    }
}