namespace Emberwild.Core.Singleton
{
    /// <summary>
    /// 普通 C# 单例基类。
    /// 不依赖 Unity 场景,不继承 MonoBehaviour,无需挂载 GameObject。
    /// 仅提供最基础的 Instance 访问,不引入额外生命周期或管理机制。
    /// </summary>
    public abstract class Singleton<T> where T : Singleton<T>, new()
    {
        private static readonly System.Lazy<T> _instance = new System.Lazy<T>(() => new T());

        public static T Instance => _instance.Value;

        protected Singleton() { }
    }
}