namespace SmartToolbox.Models;

public class FileMoveInfo
{
    public string SourceRelativePath { get; set; } = "";
    public string TargetDisplayName { get; set; } = "";
    public long FileSize { get; set; }

    public string FormattedSize => FileSize switch
    {
        < 1024 => $"{FileSize} B",
        < 1024 * 1024 => $"{FileSize / 1024.0:F1} KB",
        < 1024L * 1024 * 1024 => $"{FileSize / (1024.0 * 1024):F1} MB",
        _ => $"{FileSize / (1024.0 * 1024 * 1024):F2} GB"
    };
}
