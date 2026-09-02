using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using SilentSetup.Models;

namespace SilentSetup.Services
{
    /// <summary>
    /// Applies patches to installed applications
    /// </summary>
    public class PatchService
    {
        private readonly LoggerService _logger;

        public PatchService(LoggerService logger)
        {
            _logger = logger;
        }

        public async Task<PatchResult> ApplyPatchAsync(PatchManifest patch, AppManifest app)
        {
            try
            {
                _logger.Info($"Applying patch: {patch.Name} to {app.Name}");

                // Resolve placeholders
                var appDir = GetAppDirectory(app);
                if (string.IsNullOrWhiteSpace(appDir))
                {
                    return new PatchResult
                    {
                        Success = false,
                        Message = "Could not determine app directory"
                    };
                }

                // Apply patch based on type
                PatchResult result = patch.Type.ToLower() switch
                {
                    "copy-files" => await ApplyCopyFiles(patch, appDir),
                    "executable" => await ApplyExecutable(patch, appDir),
                    "registry" => ApplyRegistry(patch),
                    "archive" => await ApplyArchive(patch, appDir),
                    "download-extract" => await ApplyDownloadExtract(patch, appDir),
                    _ => new PatchResult
                    {
                        Success = false,
                        Message = $"Unknown patch type: {patch.Type}"
                    }
                };

                if (result.Success)
                {
                    _logger.Info($"Patch applied successfully: {patch.Name}");
                }
                else
                {
                    _logger.Error($"Patch failed: {patch.Name} - {result.Message}");
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.Error($"Patch error for {patch.Name}: {ex.Message}");
                return new PatchResult
                {
                    Success = false,
                    Message = "Patch exception",
                    ErrorDetails = ex.Message
                };
            }
        }

        private async Task<PatchResult> ApplyCopyFiles(PatchManifest patch, string appDir)
        {
            var backedUpFiles = new List<string>();

            try
            {
                if (patch.Files == null || !patch.Files.Any())
                {
                    return new PatchResult
                    {
                        Success = false,
                        Message = "No files specified for copy-files patch"
                    };
                }

                foreach (var file in patch.Files)
                {
                    var sourcePath = Path.Combine(patch.FilesDirectory, file.Name);
                    if (!File.Exists(sourcePath))
                    {
                        return new PatchResult
                        {
                            Success = false,
                            Message = $"Patch file not found: {file.Name}",
                            BackedUpFiles = backedUpFiles
                        };
                    }

                    var destPath = ResolvePath(file.Destination, appDir, patch.PatchDirectory);
                    var destDir = Path.GetDirectoryName(destPath);
                    if (!string.IsNullOrWhiteSpace(destDir))
                    {
                        Directory.CreateDirectory(destDir);
                    }

                    // Backup existing file
                    if (file.Backup && File.Exists(destPath))
                    {
                        var backupPath = $"{destPath}.backup";
                        _logger.Info($"Backing up: {destPath} -> {backupPath}");
                        File.Copy(destPath, backupPath, overwrite: true);
                        backedUpFiles.Add(backupPath);
                    }

                    // Copy file
                    _logger.Info($"Copying: {sourcePath} -> {destPath}");
                    await Task.Run(() => File.Copy(sourcePath, destPath, file.Overwrite));
                }

                return new PatchResult
                {
                    Success = true,
                    Message = $"Copied {patch.Files.Count} file(s)",
                    BackedUpFiles = backedUpFiles
                };
            }
            catch (Exception ex)
            {
                return new PatchResult
                {
                    Success = false,
                    Message = "File copy error",
                    ErrorDetails = ex.Message,
                    BackedUpFiles = backedUpFiles
                };
            }
        }

        private async Task<PatchResult> ApplyExecutable(PatchManifest patch, string appDir)
        {
            try
            {
                if (patch.Execute == null || string.IsNullOrWhiteSpace(patch.Execute.File))
                {
                    return new PatchResult
                    {
                        Success = false,
                        Message = "No executable specified"
                    };
                }

                var exePath = Path.Combine(patch.FilesDirectory, patch.Execute.File);
                if (!File.Exists(exePath))
                {
                    return new PatchResult
                    {
                        Success = false,
                        Message = $"Executable not found: {patch.Execute.File}"
                    };
                }

                var workingDir = string.IsNullOrWhiteSpace(patch.Execute.WorkingDir)
                    ? patch.FilesDirectory
                    : ResolvePath(patch.Execute.WorkingDir, appDir, patch.PatchDirectory);

                var args = patch.Execute.Args != null
                    ? string.Join(" ", patch.Execute.Args.Select(a => ResolvePath(a, appDir, patch.PatchDirectory)))
                    : string.Empty;

                var startInfo = new ProcessStartInfo
                {
                    FileName = exePath,
                    Arguments = args,
                    WorkingDirectory = workingDir,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                if (patch.Execute.RunAsAdmin)
                {
                    startInfo.Verb = "runas";
                    startInfo.UseShellExecute = true;
                    startInfo.RedirectStandardOutput = false;
                    startInfo.RedirectStandardError = false;
                }

                _logger.Info($"Executing: {exePath} {args}");

                using var process = Process.Start(startInfo);
                if (process == null)
                {
                    return new PatchResult
                    {
                        Success = false,
                        Message = "Failed to start patch executable"
                    };
                }

                var completed = await Task.Run(() => process.WaitForExit(patch.Execute.Timeout * 1000));

                if (!completed)
                {
                    _logger.Error($"Patch executable timeout after {patch.Execute.Timeout}s");
                    try
                    {
                        process.Kill();
                    }
                    catch { }

                    return new PatchResult
                    {
                        Success = false,
                        Message = $"Executable timeout ({patch.Execute.Timeout}s)"
                    };
                }

                var exitCode = process.ExitCode;
                _logger.Info($"Patch executable exit code: {exitCode}");

                return new PatchResult
                {
                    Success = exitCode == 0,
                    Message = exitCode == 0 ? "Patch executed successfully" : $"Patch failed with exit code {exitCode}"
                };
            }
            catch (Exception ex)
            {
                return new PatchResult
                {
                    Success = false,
                    Message = "Executable patch error",
                    ErrorDetails = ex.Message
                };
            }
        }

        private PatchResult ApplyRegistry(PatchManifest patch)
        {
            try
            {
                if (patch.Registry == null || !patch.Registry.Any())
                {
                    return new PatchResult
                    {
                        Success = false,
                        Message = "No registry operations specified"
                    };
                }

                foreach (var op in patch.Registry)
                {
                    var root = GetRegistryRoot(op.Root);
                    if (root == null)
                    {
                        return new PatchResult
                        {
                            Success = false,
                            Message = $"Invalid registry root: {op.Root}"
                        };
                    }

                    using var key = root.CreateSubKey(op.Path, writable: true);
                    if (key == null)
                    {
                        return new PatchResult
                        {
                            Success = false,
                            Message = $"Could not open registry key: {op.Path}"
                        };
                    }

                    if (op.Action.ToLower() == "set")
                    {
                        var valueKind = op.Type.ToLower() switch
                        {
                            "dword" => RegistryValueKind.DWord,
                            "binary" => RegistryValueKind.Binary,
                            _ => RegistryValueKind.String
                        };

                        object? value = op.Type.ToLower() switch
                        {
                            "dword" => int.TryParse(op.Value, out var i) ? i : 0,
                            _ => op.Value
                        };

                        _logger.Info($"Setting registry: {op.Root}\\{op.Path}\\{op.Name} = {value}");
                        key.SetValue(op.Name ?? string.Empty, value ?? string.Empty, valueKind);
                    }
                    else if (op.Action.ToLower() == "delete")
                    {
                        _logger.Info($"Deleting registry value: {op.Root}\\{op.Path}\\{op.Name}");
                        key.DeleteValue(op.Name ?? string.Empty, throwOnMissingValue: false);
                    }
                }

                return new PatchResult
                {
                    Success = true,
                    Message = $"Applied {patch.Registry.Count} registry operation(s)"
                };
            }
            catch (Exception ex)
            {
                return new PatchResult
                {
                    Success = false,
                    Message = "Registry patch error",
                    ErrorDetails = ex.Message
                };
            }
        }

        private async Task<PatchResult> ApplyArchive(PatchManifest patch, string appDir)
        {
            try
            {
                if (patch.Archive == null || string.IsNullOrWhiteSpace(patch.Archive.File))
                {
                    return new PatchResult
                    {
                        Success = false,
                        Message = "No archive file specified"
                    };
                }

                var archivePath = Path.Combine(patch.FilesDirectory, patch.Archive.File);
                if (!File.Exists(archivePath))
                {
                    return new PatchResult
                    {
                        Success = false,
                        Message = $"Archive not found: {patch.Archive.File}"
                    };
                }

                var extractTo = ResolvePath(patch.Archive.ExtractTo, appDir, patch.PatchDirectory);
                Directory.CreateDirectory(extractTo);

                _logger.Info($"Extracting archive to: {extractTo}");

                await Task.Run(() =>
                {
                    if (!string.IsNullOrWhiteSpace(patch.Archive.Password))
                    {
                        // Password-protected archive extraction requires third-party library
                        _logger.Warn("Password-protected archive requires SharpZipLib package");
                        throw new NotSupportedException("Password-protected archive extraction not yet implemented. Use SharpZipLib for password support.");
                    }
                    else
                    {
                        ZipFile.ExtractToDirectory(archivePath, extractTo, overwriteFiles: patch.Archive.Overwrite);
                    }
                });

                return new PatchResult
                {
                    Success = true,
                    Message = "Archive extracted successfully"
                };
            }
            catch (Exception ex)
            {
                return new PatchResult
                {
                    Success = false,
                    Message = "Archive extraction error",
                    ErrorDetails = ex.Message
                };
            }
        }

        private async Task<PatchResult> ApplyDownloadExtract(PatchManifest patch, string appDir)
        {
            var backedUpFiles = new List<string>();
            string? tempZipPath = null;
            string? tempExtractDir = null;

            try
            {
                if (patch.Download == null || string.IsNullOrWhiteSpace(patch.Download.Url))
                {
                    return new PatchResult
                    {
                        Success = false,
                        Message = "No download URL specified"
                    };
                }

                if (patch.Files == null || !patch.Files.Any())
                {
                    return new PatchResult
                    {
                        Success = false,
                        Message = "No files specified for extraction"
                    };
                }

                // Download ZIP file
                _logger.Info($"Downloading from: {patch.Download.Url}");
                tempZipPath = Path.Combine(Path.GetTempPath(), $"{patch.Id}_{Guid.NewGuid()}.zip");

                using (var httpClient = new HttpClient())
                {
                    httpClient.Timeout = TimeSpan.FromMinutes(5);
                    var response = await httpClient.GetAsync(patch.Download.Url);
                    response.EnsureSuccessStatusCode();

                    await using var fs = new FileStream(tempZipPath, FileMode.Create, FileAccess.Write, FileShare.None);
                    await response.Content.CopyToAsync(fs);
                }

                _logger.Info($"Downloaded to: {tempZipPath}");

                // Extract to temp directory
                tempExtractDir = Path.Combine(Path.GetTempPath(), $"{patch.Id}_{Guid.NewGuid()}");
                Directory.CreateDirectory(tempExtractDir);

                _logger.Info($"Extracting to: {tempExtractDir}");
                await Task.Run(() => ZipFile.ExtractToDirectory(tempZipPath, tempExtractDir));

                // Copy files from extracted archive to destinations
                foreach (var file in patch.Files)
                {
                    var sourcePath = Path.Combine(tempExtractDir, file.Name);
                    if (!File.Exists(sourcePath))
                    {
                        return new PatchResult
                        {
                            Success = false,
                            Message = $"Extracted file not found: {file.Name}",
                            BackedUpFiles = backedUpFiles
                        };
                    }

                    var destPath = ResolvePath(file.Destination, appDir, patch.PatchDirectory);
                    var destDir = Path.GetDirectoryName(destPath);
                    if (!string.IsNullOrWhiteSpace(destDir))
                    {
                        Directory.CreateDirectory(destDir);
                    }

                    // Backup existing file
                    if (file.Backup && File.Exists(destPath))
                    {
                        var backupPath = $"{destPath}.backup";
                        _logger.Info($"Backing up: {destPath} -> {backupPath}");
                        File.Copy(destPath, backupPath, overwrite: true);
                        backedUpFiles.Add(backupPath);
                    }

                    // Copy file
                    _logger.Info($"Copying: {sourcePath} -> {destPath}");
                    File.Copy(sourcePath, destPath, file.Overwrite);
                }

                return new PatchResult
                {
                    Success = true,
                    Message = $"Downloaded and copied {patch.Files.Count} file(s)",
                    BackedUpFiles = backedUpFiles
                };
            }
            catch (Exception ex)
            {
                return new PatchResult
                {
                    Success = false,
                    Message = "Download-extract error",
                    ErrorDetails = ex.Message,
                    BackedUpFiles = backedUpFiles
                };
            }
            finally
            {
                // Clean up temp files
                try
                {
                    if (tempZipPath != null && File.Exists(tempZipPath))
                    {
                        File.Delete(tempZipPath);
                    }
                    if (tempExtractDir != null && Directory.Exists(tempExtractDir))
                    {
                        Directory.Delete(tempExtractDir, recursive: true);
                    }
                }
                catch (Exception ex)
                {
                    _logger.Warn($"Failed to clean up temp files: {ex.Message}");
                }
            }
        }

        private string? GetAppDirectory(AppManifest app)
        {
            // Try install_dir from manifest
            if (!string.IsNullOrWhiteSpace(app.Install.InstallDir))
            {
                var dir = Environment.ExpandEnvironmentVariables(app.Install.InstallDir);
                if (Directory.Exists(dir))
                    return dir;
            }

            // Try detection paths
            if (app.Detection?.File != null)
            {
                foreach (var file in app.Detection.File)
                {
                    var path = Environment.ExpandEnvironmentVariables(file.Path);
                    if (File.Exists(path))
                    {
                        return Path.GetDirectoryName(path);
                    }
                }
            }

            return null;
        }

        private string ResolvePath(string path, string appDir, string patchDir)
        {
            return path
                .Replace("{app_dir}", appDir)
                .Replace("{patch_dir}", patchDir)
                .Replace("{patch_files}", Path.Combine(patchDir, "files"))
                .Replace("{temp}", Path.GetTempPath())
                .Replace("{ProgramFiles}", Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles))
                .Replace("{LocalAppData}", Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData))
                .Replace("{AppData}", Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData));
        }

        private RegistryKey? GetRegistryRoot(string root)
        {
            return root.ToUpper() switch
            {
                "HKLM" => Registry.LocalMachine,
                "HKCU" => Registry.CurrentUser,
                "HKCR" => Registry.ClassesRoot,
                "HKU" => Registry.Users,
                "HKCC" => Registry.CurrentConfig,
                _ => null
            };
        }
    }
}
