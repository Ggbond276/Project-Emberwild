using UnityEngine;
using Emberwild.Core.AppLog;
using Emberwild.Input;

namespace Emberwild.CameraControl
{
    /// <summary>
    /// 第三人称摄像机控制器 — 基础跟随 + RS 旋转。
    ///
    /// 职责:
    /// - 跟随一个 <see cref="Transform"/> target(玩家角色)。
    /// - 订阅 <see cref="InputReader.OnLook"/>,把 RS 二维向量映射为绕 target 的
    ///   yaw(水平)和 pitch(垂直,带 [_minPitch, _maxPitch] 限位)。
    /// - <c>LateUpdate</c> 中把摄像机放到 target 后的固定偏移位置并 LookAt target。
    ///
    /// 严格不做:
    /// - 不移动 target(角色移动由 CharacterMovementController 负责)。
    /// - 不访问 Animator、不广播 EventBus。
    /// - 不做平滑 / 阻尼 / 镜头碰撞 / 震动 / 回中动画。
    ///
    /// 后续 CharacterMovementController 会读取本组件 transform 上的方向信息,
    /// 实现"相机相对移动"。
    /// </summary>
    [DisallowMultipleComponent]
    public class CameraController : MonoBehaviour
    {
        [Header("Target")]
        [Tooltip("摄像机跟随的目标(玩家角色 Transform)")]
        [SerializeField] private Transform _target;

        [Header("跟随偏移(相对 target)")]
        [Tooltip("摄像机到 target 的水平距离")]
        [SerializeField] private float _distance = 4.0f;

        [Tooltip("摄像机相对 target 的额外抬升高度")]
        [SerializeField] private float _height = 2.0f;

        [Header("俯仰限位")]
        [Tooltip("摄像机最低俯角(度)")]
        [SerializeField] private float _minPitch = -20f;

        [Tooltip("摄像机最高仰角(度)")]
        [SerializeField] private float _maxPitch = 60f;

        [Header("灵敏度")]
        [Tooltip("RS 旋转灵敏度(度/秒)")]
        [SerializeField] private float _lookSensitivity = 120f;

        [Header("输入")]
        [Tooltip("场景中的 InputReader(推荐挂在独立的 InputManager GameObject 上)")]
        [SerializeField] private InputReader _inputReader;

        // 摄像机绕 target 的当前角度。
        private float _yaw;   // 水平(度)
        private float _pitch; // 垂直(度,负为俯视,正为仰视)

        private void Start()
        {
            if (_target == null)
            {
                AppLog.Error("Camera", "CameraController: target is null, please assign in Inspector");
                enabled = false;
                return;
            }

            if (_inputReader == null)
            {
                AppLog.Error("Camera", "CameraController: InputReader is null, please assign in Inspector");
                enabled = false;
                return;
            }

            // 初始化 yaw/pitch 为当前摄像机与 target 的相对角度,避免推 RS 前就跳变。
            var dir = transform.position - _target.position;
            _yaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg + 180f;
            _pitch = Mathf.Asin(dir.normalized.y) * Mathf.Rad2Deg;
            _pitch = Mathf.Clamp(_pitch, _minPitch, _maxPitch);

            _inputReader.OnLook += HandleLook;
            AppLog.Info("Camera", "CameraController initialized");
        }

        private void OnDestroy()
        {
            if (_inputReader != null)
            {
                _inputReader.OnLook -= HandleLook;
            }
        }

        private void HandleLook(Vector2 look)
        {
            // 标准第三人称:水平累加,垂直反向累加(RS 推上 → 视角朝上看)。
            _yaw   += look.x * _lookSensitivity * Time.deltaTime;
            _pitch -= look.y * _lookSensitivity * Time.deltaTime;
            _pitch  = Mathf.Clamp(_pitch, _minPitch, _maxPitch);
        }

        private void LateUpdate()
        {
            if (_target == null) return;

            // 球坐标 → 偏移向量:摄像机位于 target 后方 _distance 处,额外抬升 _height。
            var rotation = Quaternion.Euler(_pitch, _yaw, 0f);
            var offset   = rotation * new Vector3(0f, 0f, -_distance);

            var focusPoint = _target.position + Vector3.up * _height;
            transform.position = focusPoint + offset;
            transform.LookAt(focusPoint);
        }
    }
}
