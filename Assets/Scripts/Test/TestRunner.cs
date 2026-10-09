using UnityEngine;
using Emberwild.Core.AppLog;
using Emberwild.AI;
using Emberwild.Core.Events;

namespace Emberwild.Test
{
    /// <summary>
    /// 项目集中测试入口。
    /// 所有 Stage 的运行时验证都在此脚本中,按 Stage 顺序排列。
    /// 使用方法:挂载到 SampleScene 任意 GameObject,运行场景后查看 Console 输出。
    ///
    /// 规则:每个 Stage 测试通过后,清理该 Stage 的测试代码,只留类骨架与 hooks。
    /// </summary>
    public class TestRunner : MonoBehaviour
    {
        // 自身引用,供后续 Stage 的静态方法 AddComponent 与事件订阅使用。
        private static TestRunner _instance;

        private void Awake()
        {
            _instance = this;
        }

        private void Start()
        {
            // 新 Stage 测试在此处按顺序调用,例如:
            //   TestStage8_Dodge();
            TestStage7_6_EnemyBase();
        }

        // 日志节流(后续 Stage 可继续使用)。
        protected const float LogInterval = 0.2f;


        private static EnemyBase _enemy;
        private static float _lastEnemyDamageLogTime;
        private static float _lastEnemyDeathLogTime;
        private static int _enemyDamageCount;
        private static int _enemyDeathCount;
        private static bool _enemyTestDamageFired;

        private static void TestStage7_6_EnemyBase()
        {
            if (_instance == null) return;
            _enemy = _instance.gameObject.AddComponent<EnemyBase>();
            EventBus.Subscribe<EnemyDamagedEvent>(OnTestEnemyDamaged);
            EventBus.Subscribe<EnemyDiedEvent>(OnTestEnemyDied);
            AppLog.Info("AI", "EnemyBase attached; will damage once after Start finishes");
        }
        private void Update()
        {
            if (_enemyTestDamageFired) return;
            if (TestRunner._enemy == null) return;
            TestRunner._enemy.TakeDamage(10f, UnityEngine.Vector3.forward);
            _enemyTestDamageFired = true;
        }
        private static void OnTestEnemyDamaged(EnemyDamagedEvent e)
        {
            _enemyDamageCount++;
            if (Time.unscaledTime - _lastEnemyDamageLogTime < LogInterval) return;
            AppLog.Info("AI", $"[Test] Enemy Damaged #{_enemyDamageCount} dmg={e.Damage:F1} hp={e.CurrentHp:F1}");
            _lastEnemyDamageLogTime = Time.unscaledTime;
        }
        private static void OnTestEnemyDied(EnemyDiedEvent _)
        {
            _enemyDeathCount++;
            if (Time.unscaledTime - _lastEnemyDeathLogTime < LogInterval) return;
            AppLog.Info("AI", $"[Test] EnemyDied fired (count={_enemyDeathCount})");
            _lastEnemyDeathLogTime = Time.unscaledTime;
        }
    }
}
