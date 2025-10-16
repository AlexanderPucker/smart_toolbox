using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace SmartToolbox.Models
{
    /// <summary>
    /// 大小写转换配置类，提供各种大小写转换选项
    /// </summary>
    public static class CaseConversionConfig
    {
        /// <summary>
        /// 获取可用的大小写转换选项集合
        /// 包括：大写、小写、首字母大写、保持原样
        /// </summary>
        public static ObservableCollection<CaseConversionItem> ConversionOptions { get; } = new()
        {
            new CaseConversionItem("转换为大写", CaseConversionType.UpperCase),
            new CaseConversionItem("转换为小写", CaseConversionType.LowerCase),
            new CaseConversionItem("转换为首字母大写", CaseConversionType.TitleCase),
            new CaseConversionItem("保持原始大小写", CaseConversionType.None)
        };
    }

    /// <summary>
    /// 大小写转换类型枚举
    /// </summary>
    public enum CaseConversionType
    {
        /// <summary>
        /// 保持原样，不进行转换
        /// </summary>
        None = 0,
        
        /// <summary>
        /// 转换为大写
        /// </summary>
        UpperCase = 1,
        
        /// <summary>
        /// 转换为小写
        /// </summary>
        LowerCase = 2,
        
        /// <summary>
        /// 转换为首字母大写（每个单词首字母大写）
        /// </summary>
        TitleCase = 3
    }

    /// <summary>
    /// 大小写转换选项项类
    /// 用于在UI中显示转换选项
    /// </summary>
    public class CaseConversionItem
    {
        /// <summary>
        /// 显示名称，用于UI展示
        /// </summary>
        public string DisplayName { get; set; }
        
        /// <summary>
        /// 转换类型枚举值
        /// </summary>
        public CaseConversionType ConversionType { get; set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="displayName">显示名称</param>
        /// <param name="conversionType">转换类型</param>
        public CaseConversionItem(string displayName, CaseConversionType conversionType)
        {
            DisplayName = displayName;
            ConversionType = conversionType;
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

    /// <summary>
    /// 文件过滤配置类，提供常见的文件类型过滤选项
    /// </summary>
    public static class FileFilterConfig
    {
        /// <summary>
        /// 常用文件过滤器集合
        /// 包括各种常见文件类型分类
        /// </summary>
        public static ObservableCollection<FileFilterItem> CommonFilters { get; } = new()
        {
            new FileFilterItem("所有文件", "*.*"),
            new FileFilterItem("文本文件", "*.txt"),
            new FileFilterItem("图片文件", "*.jpg,*.jpeg,*.png,*.gif,*.bmp,*.ico,*.svg,*.webp"),
            new FileFilterItem("视频文件", "*.mp4,*.avi,*.mkv,*.mov,*.wmv,*.flv,*.webm,*.m4v"),
            new FileFilterItem("音频文件", "*.mp3,*.wav,*.flac,*.aac,*.ogg,*.wma,*.m4a"),
            new FileFilterItem("文档文件", "*.pdf,*.doc,*.docx,*.xls,*.xlsx,*.ppt,*.pptx"),
            new FileFilterItem("代码文件", "*.cs,*.js,*.ts,*.html,*.css,*.cpp,*.c,*.h,*.java,*.py"),
            new FileFilterItem("压缩文件", "*.zip,*.rar,*.7z,*.tar,*.gz,*.bz2"),
            new FileFilterItem("可执行文件", "*.exe,*.msi,*.app,*.deb,*.rpm"),
            new FileFilterItem("配置文件", "*.json,*.xml,*.yaml,*.yml,*.ini,*.config,*.settings")
        };

        /// <summary>
        /// 获取默认文件过滤器（所有文件）
        /// </summary>
        /// <returns>默认过滤器模式 "*.*"</returns>
        public static string GetDefaultFilter()
        {
            return "*.*";
        }
    }

    /// <summary>
    /// 文件过滤器项类
    /// 用于表示特定类型的文件过滤规则
    /// </summary>
    public class FileFilterItem
    {
        /// <summary>
        /// 显示名称，用于UI展示
        /// </summary>
        public string DisplayName { get; set; }
        
        /// <summary>
        /// 文件模式，支持通配符，多个模式用逗号分隔
        /// </summary>
        public string Pattern { get; set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="displayName">显示名称</param>
        /// <param name="pattern">文件模式</param>
        public FileFilterItem(string displayName, string pattern)
        {
            DisplayName = displayName;
            Pattern = pattern;
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