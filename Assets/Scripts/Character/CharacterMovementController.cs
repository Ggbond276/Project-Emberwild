using UnityEngine;
using Emberwild.CameraControl;
using Emberwild.Core.AppLog;
using Emberwild.Input;
using Emberwild.Core.Events;

namespace Emberwild.Character
{
    /// <summary>
    /// 角色移动控制器 — Camera Relative 移动 + Animator Locomotion 驱动。
    ///
    /// 职责:
    /// - 订阅 <see cref="InputReader.OnMove"/>,获得 LS 二维向量。
    /// - 从 <see cref="CameraController"/>.transform 拿 forward / right,投影到 XZ 平面。
    /// - 算相机相对移动方向,以 <see cref="_moveSpeed"/> 修改 transform.position。
    /// - 让 transform.forward 始终朝向移动方向(静止时保留当前朝向)。
    /// - 把 <c>_moveInput.magnitude</c>(0~1)喂给 Animator 的 <c>Speed</c> float 参数,
    ///   驱动 Locomotion Blend Tree。
    ///
    /// 严格不做:
    /// - 不广播 EventBus。
    /// - 不做碰撞检测 / 物理 / 地面检测 / 重力。
    /// - 不引入 Rigidbody / CharacterController。
    /// - 不处理 Sprint / Crouch / Jump / Attack 等其他动画参数。
    /// </summary>
    [DisallowMultipleComponent]
    public class CharacterMovementController : MonoBehaviour
    {


        [Header("攻击")]
        [Tooltip("每段伤害(段位 0~3)")]
        [SerializeField] private float[] _attackDamages = { 10f, 12f, 15f, 20f };

        [Tooltip("每段攻击动画时长(秒);命中帧 = 时长/2")]
        [SerializeField] private float _attackDuration = 0.5f;

        [Tooltip("段间衔接窗口(秒)")]
        [SerializeField] private float _comboWindow = 0.4f;



        [Header("输入")]
        [Tooltip("场景中的 InputReader(挂在 InputManager GameObject 上)")]
        [SerializeField] private InputReader _inputReader;

        [Header("摄像机")]
        [Tooltip("Main Camera 上的 CameraController(用于取相机方向)")]
        [SerializeField] private CameraController _cameraController;

        [Header("动画")]
        [Tooltip("角色 Animator(挂在角色 GameObject 上,可选;为空时只警告一次不报错)")]
        [SerializeField] private Animator _animator;

        [Tooltip("Animator 上驱动 Locomotion 的 float 参数名")]
        [SerializeField] private string _speedParameter = "Speed";

        [Header("移动")]
        [Tooltip("角色移动速度(米/秒)")]
        [SerializeField] private float _moveSpeed = 5f;

        // 缓存 OnMove 回调值,Update 中读取。
        private Vector2 _moveInput;

        // Animator.StringToHash 的参数 id,避免每帧字符串查找。
        private int _speedHash;

        private bool _isAttacking;
        private int _comboIndex;
        private float _attackEndTime;   // 当前攻击结束时间(Time.time)
        private float _comboDeadline;   // 衔接窗口截止时间

        private void Start()
        {
            if (_inputReader == null)
            {
                AppLog.Error("Player", "CharacterMovementController: InputReader is null, please assign in Inspector");
                enabled = false;
                return;
            }

            if (_cameraController == null)
            {
                AppLog.Error("Player", "CharacterMovementController: CameraController is null, please assign in Inspector");
                enabled = false;
                return;
            }

            if (_animator == null)
            {
                AppLog.Warning("Player", "CharacterMovementController: Animator is null, locomotion animation will not play");
            }
            else
            {
                _speedHash = Animator.StringToHash(_speedParameter);
            }

            _inputReader.OnMove += HandleMove;
            _inputReader.OnAttack += HandleAttack;
            AppLog.Info("Player", "Attack subscribed");
            AppLog.Info("Player", "CharacterMovementController initialized");
        }

        private void OnDestroy()
        {
            if (_inputReader != null)
            {
                _inputReader.OnMove -= HandleMove;
                _inputReader.OnAttack -= HandleAttack;
            }

        }

        private void HandleMove(Vector2 move)
        {
            _moveInput = move;
        }

        private void HandleAttack()
        {
            // 衔接窗口外:从段 0 开始
            // 衔接窗口内:连段(0→1→2→3,满 4 回 0)
            if (Time.time > _comboDeadline || _comboIndex >= 3)
            {
                _comboIndex = 0;
            }
            else
            {
                _comboIndex++;
            }

            _isAttacking = true;
            _attackEndTime = Time.time + _attackDuration;
            _comboDeadline = _attackEndTime + _comboWindow;

            float damage = _attackDamages[Mathf.Clamp(_comboIndex, 0, _attackDamages.Length - 1)];

            // 命中帧 = 动画中点
            var hitPoint = transform.position + transform.forward * 1.2f;
            var hitDir = transform.forward;

            EventBus.Publish(new PlayerAttackedEvent(_comboIndex, hitPoint, hitDir, damage));

            AppLog.Info("Player", $"Attack combo={_comboIndex} damage={damage}");
        }

        private void Update()
        {

            // 攻击中:跳过移动与朝向
            if (_isAttacking)
            {
                if (Time.time >= _attackEndTime)
                {
                    _isAttacking = false;
                }
                // 攻击期间不调用 transform 移动 / 转向
                if (_animator != null && _speedHash != 0)
                {
                    _animator.SetFloat(_speedHash, 0f);
                }
                return;
            }


            var input = _moveInput;
            var magnitude = input.magnitude;

            // 喂 Animator Locomotion 参数(只要 Animator 存在就每帧喂,即使静止也写 0)。
            // Animator 对不存在的参数名静默忽略,所以参数还没建也不会报错。
            if (_animator != null && _speedHash != 0)
            {
                _animator.SetFloat(_speedHash, magnitude);
            }

            // 静止:不移动、不朝向。
            if (magnitude < 0.0001f) return;

            // 摄像机 forward / right 投影到 XZ 平面,避免俯仰影响移动方向。
            var camForward = _cameraController.transform.forward;
            camForward.y = 0f;
            if (camForward.sqrMagnitude < 0.0001f) return; // 摄像机垂直朝上/下时退化
            camForward.Normalize();

            var camRight = _cameraController.transform.right;
            camRight.y = 0f;
            camRight.Normalize();

            // 相机相对移动方向。
            // input.x = 左右(+ 向相机视角的右), input.y = 前后(+ 向相机视角的前)。
            var moveDir = camRight * input.x + camForward * input.y;
            if (moveDir.sqrMagnitude < 0.0001f) return;

            moveDir.Normalize();

            // 位移。
            transform.position += moveDir * _moveSpeed * Time.deltaTime;

            // 朝向:角色面朝移动方向。
            // 静止时不再调用,保留上一次的朝向,避免回中时 LookRotation(zero) 报警。
            transform.rotation = Quaternion.LookRotation(moveDir, Vector3.up);
        }
    }
}
