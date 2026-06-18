using System.Collections.Generic;

namespace ArchiveFlow.Application.DTOs;

/// <summary>
/// Represents the overall health and statistics of the digital archive.
/// </summary>
public class StatisticsDto
{
    public int TotalFiles { get; set; }
    public long TotalSizeBytes { get; set; }
    public int TotalMetadataEntries { get; set; }
    
    /// <summary>
    /// Percentage of files that have at least one metadata entry (0-100).
    /// </summary>
    public double MetadataCompleteness { get; set; }

    /// <summary>
    /// Distribution of files by extension (e.g., ".png": 50, ".txt": 20).
    /// </summary>
    public Dictionary<string, int> FileTypeDistribution { get; set; } = new();
}

/// <summary>
/// Represents a single bar in the file type distribution chart.
/// </summary>
public class FileTypeBar
{
    public string Extension { get; set; } = string.Empty;
    public int Count { get; set; }
    
    /// <summary>
    /// Width percentage for the UI bar (0-100).
    /// </summary>
    public double WidthPercentage { get; set; }
}