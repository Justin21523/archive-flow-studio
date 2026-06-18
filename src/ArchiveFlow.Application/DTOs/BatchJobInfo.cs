namespace ArchiveFlow.Application.DTOs;

/// <summary>
/// Data transfer object representing the current status of a batch job in the UI.
/// </summary>
public class BatchJobInfo
{
    public string JobName { get; set; } = string.Empty;
    public string StatusMessage { get; set; } = "Pending...";
    public double ProgressPercentage { get; set; }
    public bool IsActive { get; set; }
}