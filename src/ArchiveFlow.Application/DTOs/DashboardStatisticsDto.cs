namespace ArchiveFlow.Application.DTOs;

/// <summary>
/// Data transfer object containing all statistics needed for the Dashboard view.
/// </summary>
public class DashboardStatisticsDto
{
    public int TotalFiles { get; set; }
    public long TotalSizeBytes { get; set; }
    public int TotalMetadataEntries { get; set; }
    public double MetadataCompleteness { get; set; }
    
    /// <summary>
    /// Distribution of files by extension (e.g., ".png": 50).
    /// </summary>
    public Dictionary<string, int> FileTypeDistribution { get; set; } = new();
}

/// <summary>
/// Represents a single bar in the file type distribution chart.
/// </summary>
public class FileTypeStat
{
    public string Extension { get; set; } = string.Empty;
    public int Count { get; set; }
    public double Percentage { get; set; } // 0 to 100
}