using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace SmartToolbox.Models
{
    /// <summary>
    /// 系统设置配置类，提供各种系统设置选项配置
    /// </summary>
    public static class SystemSettingsConfig
    {
        /// <summary>
        /// 获取可用的应用主题选项集合
        /// </summary>
        public static ObservableCollection<SystemSettingsItem> ThemeOptions { get; } = new()
        {
            new SystemSettingsItem("浅色", "Light"),
            new SystemSettingsItem("深色", "Dark"),
            new SystemSettingsItem("跟随系统", "System")
        };

        /// <summary>
        /// 获取可用的语言选项集合
        /// </summary>
        public static ObservableCollection<SystemSettingsItem> LanguageOptions { get; } = new()
        {
            new SystemSettingsItem("中文", "zh-CN"),
            new SystemSettingsItem("English", "en-US")
        };

        /// <summary>
        /// 获取可用的文件命名规则选项集合
        /// </summary>
        public static ObservableCollection<SystemSettingsItem> FileNamingRuleOptions { get; } = new()
        {
            new SystemSettingsItem("原文件名_时间戳", "OriginalName_Timestamp"),
            new SystemSettingsItem("原文件名_序号", "OriginalName_Sequence"),
            new SystemSettingsItem("时间戳_原文件名", "Timestamp_OriginalName")
        };

        /// <summary>
        /// 获取可用的日志级别选项集合
        /// </summary>
        public static ObservableCollection<SystemSettingsItem> LogLevelOptions { get; } = new()
        {
            new SystemSettingsItem("信息", "Info"),
            new SystemSettingsItem("警告", "Warning"),
            new SystemSettingsItem("错误", "Error")
        };
    }

    /// <summary>
    /// 系统设置选项项类
    /// 用于在UI中显示系统设置选项
    /// </summary>
    public class SystemSettingsItem
    {
        /// <summary>
        /// 显示名称，用于UI展示
        /// </summary>
        public string DisplayName { get; set; }
        
        /// <summary>
        /// 值，用于实际设置的存储和处理
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="displayName">显示名称</param>
        /// <param name="value">值</param>
        public SystemSettingsItem(string displayName, string value)
        {
            DisplayName = displayName;
            Value = value;
        }

        /// <summary>
        /// 重写ToString方法，返回显示名称
        /// </summary>
        /// <returns>显示名称</returns>
        public override string ToString()
        {
            return DisplayName;
        }
    }
}