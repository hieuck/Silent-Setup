using System.IO;

namespace SilentSetup.Models
{
    /// <summary>
    /// Represents a patch manifest loaded from patches/*/manifest.yaml
    /// </summary>
    public class PatchManifest
    {
        // Required fields
        public string Name { get; set; } = string.Empty;
        public string Id { get; set; } = string.Empty;
        public string TargetApp { get; set; } = string.Empty;
        public string Type { get; set; } = "copy-files"; // copy-files, executable, registry, archive

        // Optional fields
        public CompatibilityConfig? Compatibility { get; set; }
        public List<PatchFile>? Files { get; set; }
        public ExecuteConfig? Execute { get; set; }
        public List<RegistryOperation>? Registry { get; set; }
        public ArchiveConfig? Archive { get; set; }
        public VerificationConfig? Verification { get; set; }
        public RollbackConfig? Rollback { get; set; }
        public PatchMetadata? Metadata { get; set; }
        public SecurityConfig? Security { get; set; }

        // Runtime properties
        public string PatchDirectory { get; set; } = string.Empty; // Full path to patches/patch-id/
        public string FilesDirectory => Path.Combine(PatchDirectory, "files");
    }

    public class CompatibilityConfig
    {
        public List<string>? AppVersions { get; set; }
        public string? MinVersion { get; set; }
        public string? MaxVersion { get; set; }
    }

    public class PatchFile
    {
        public string Name { get; set; } = string.Empty;
        public string Destination { get; set; } = string.Empty;
        public bool Backup { get; set; } = true;
        public bool Overwrite { get; set; } = true;
    }

    public class ExecuteConfig
    {
        public string File { get; set; } = string.Empty;
        public List<string>? Args { get; set; }
        public string? WorkingDir { get; set; }
        public bool RunAsAdmin { get; set; }
        public int Timeout { get; set; } = 300;
    }

    public class RegistryOperation
    {
        public string Action { get; set; } = "set"; // set, delete
        public string Root { get; set; } = "HKLM"; // HKLM, HKCU
        public string Path { get; set; } = string.Empty;
        public string? Name { get; set; }
        public string? Value { get; set; }
        public string Type { get; set; } = "string"; // string, dword, binary
    }

    public class ArchiveConfig
    {
        public string File { get; set; } = string.Empty;
        public string ExtractTo { get; set; } = string.Empty;
        public bool Overwrite { get; set; } = true;
        public string? Password { get; set; }
        public List<string>? Include { get; set; }
        public List<string>? Exclude { get; set; }
    }

    public class VerificationConfig
    {
        public List<string>? RequiredFiles { get; set; }
        public List<RegistryVerification>? RequiredRegistry { get; set; }
    }

    public class RegistryVerification
    {
        public string Root { get; set; } = "HKLM";
        public string Path { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Value { get; set; }
    }

    public class RollbackConfig
    {
        public bool Enabled { get; set; } = true;
        public bool KeepBackups { get; set; } = true;
        public string BackupDir { get; set; } = ".backup";
    }

    public class PatchMetadata
    {
        public string? Category { get; set; }
        public string? Author { get; set; }
        public string? Version { get; set; }
        public string? Description { get; set; }
        public string? SourceUrl { get; set; }
        public string? LastUpdated { get; set; }
        public List<string>? Tags { get; set; }
    }

    public class SecurityConfig
    {
        public string RiskLevel { get; set; } = "low"; // low, medium, high
        public string? Warning { get; set; }
    }

    public enum PatchStatus
    {
        NotApplied,
        Applied,
        Incompatible,
        Error
    }
}
