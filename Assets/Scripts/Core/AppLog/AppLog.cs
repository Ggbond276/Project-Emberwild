using System.Collections.Generic;
using UnityEngine;

namespace Emberwild.Core.AppLog
{
    /// <summary>
    /// 项目统一日志系统。
    /// - 三种等级:Info / Warning / Error(等级颜色由 Unity Console 默认处理)
    /// - 统一格式:[模块] [等级] 信息
    /// - 模块名使用 rich text color tag 高亮,便于在 Console 中快速识别模块来源
    /// </summary>
    public static class AppLog
    {
        // 模块-颜色映射。模块不在此表时使用默认白色。
        private static readonly Dictionary<string, string> _moduleColors = new Dictionary<string, string>
        {
            { "Input",    "#00FFFF" }, // 青色
            { "Resource", "#FFA500" }, // 橙色
            { "Combat",   "#FF4444" }, // 红色
        };

        public static void Info(string module, string message)
        {
            Debug.Log(Format(module, "Info", message));
        }

        public static void Warning(string module, string message)
        {
            Debug.LogWarning(Format(module, "Warning", message));
        }

        public static void Error(string module, string message)
        {
            Debug.LogError(Format(module, "Error", message));
        }

        private static string Format(string module, string level, string message)
        {
            if (_moduleColors.TryGetValue(module, out var color))
            {
                module = $"<color={color}>{module}</color>";
            }
            return $"[{module}] [{level}] {message}";
        }
    }
}