using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace SmartToolbox.Models
{
    public static class CaseConversionConfig
    {
        public static ObservableCollection<CaseConversionItem> ConversionOptions { get; } = new()
        {
            new CaseConversionItem("保持原始大小写", CaseConversionType.None),
            new CaseConversionItem("转换为大写", CaseConversionType.UpperCase),
            new CaseConversionItem("转换为小写", CaseConversionType.LowerCase),
            new CaseConversionItem("转换为首字母大写", CaseConversionType.TitleCase)
        };
    }

    public enum CaseConversionType
    {
        None = 0,
        UpperCase = 1,
        LowerCase = 2,
        TitleCase = 3
    }

    public class CaseConversionItem
    {
        public string DisplayName { get; set; }
        public CaseConversionType ConversionType { get; set; }

        public CaseConversionItem(string displayName, CaseConversionType conversionType)
        {
            DisplayName = displayName;
            ConversionType = conversionType;
        }

        public override string ToString()
        {
            return DisplayName;
        }
    }

    public static class FileFilterConfig
    {
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

        public static string GetDefaultFilter()
        {
            return "*.*";
        }
    }

    public class FileFilterItem
    {
        public string DisplayName { get; set; }
        public string Pattern { get; set; }

        public FileFilterItem(string displayName, string pattern)
        {
            DisplayName = displayName;
            Pattern = pattern;
        }

        public override string ToString()
        {
            return DisplayName;
        }
    }
} 