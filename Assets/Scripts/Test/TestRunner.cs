using Emberwild.Data;
using UnityEngine;
// 解决命名空间冲突:Emberwild.Input.InputReader 与 UnityEngine.Input 重名
using UnityInput = UnityEngine.Input;

namespace Emberwild.Core.Test
{
    /// <summary>
    /// 运行时手动测试入口 — 触发伤害/重置,验证 PlayerDataManager + 事件流。
    /// 通过 Inspector 启用后,使用以下键盘按键:
    /// - T: 受伤 10
    /// - Y: 受伤 30
    /// - U: 受伤 999 (致命)
    /// - R: 重置血量
    /// </summary>
    public class TestRunner : MonoBehaviour
    {
        [Header("启用测试按键")]
        [SerializeField] private bool _enableTestKeys = true;

        [Header("受击方向")]
        [SerializeField] private Vector3 _hitDirection = new Vector3(0f, 0f, 1f);

        [Header("按键设置")]
        [SerializeField] private KeyCode _damageSmallKey = KeyCode.T;
        [SerializeField] private KeyCode _damageBigKey = KeyCode.Y;
        [SerializeField] private KeyCode _fatalDamageKey = KeyCode.U;
        [SerializeField] private KeyCode _resetKey = KeyCode.R;

        private void Update()
        {
            if (!_enableTestKeys) return;
            if (UnityInput.GetKeyDown(_damageSmallKey)) TriggerDamage(10f);
            if (UnityInput.GetKeyDown(_damageBigKey)) TriggerDamage(30f);
            if (UnityInput.GetKeyDown(_fatalDamageKey)) TriggerDamage(999f);
            if (UnityInput.GetKeyDown(_resetKey)) PlayerDataManager.Instance.ResetHealth();
        }

        private void TriggerDamage(float damage)
        {
            PlayerDataManager.Instance.TakeDamage(damage, _hitDirection);
        }
    }
}
