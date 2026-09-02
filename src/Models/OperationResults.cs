namespace SilentSetup.Models
{
    /// <summary>
    /// Result of an install operation
    /// </summary>
    public class InstallResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int ExitCode { get; set; }
        public string? ErrorDetails { get; set; }
        public TimeSpan Duration { get; set; }
    }

    /// <summary>
    /// Result of a patch operation
    /// </summary>
    public class PatchResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? ErrorDetails { get; set; }
        public List<string> BackedUpFiles { get; set; } = new();
    }

    /// <summary>
    /// Result of a download operation
    /// </summary>
    public class DownloadResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? FilePath { get; set; }
        public long FileSize { get; set; }
        public bool ChecksumVerified { get; set; }
        public string? ErrorDetails { get; set; }
    }

    /// <summary>
    /// Progress information for long-running operations
    /// </summary>
    public class OperationProgress
    {
        public string Operation { get; set; } = string.Empty;
        public string AppName { get; set; } = string.Empty;
        public int ProgressPercentage { get; set; }
        public string Status { get; set; } = string.Empty;
        public long? BytesDownloaded { get; set; }
        public long? TotalBytes { get; set; }
    }
}
