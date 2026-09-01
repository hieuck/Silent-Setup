using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using SilentSetup.Models;

namespace SilentSetup.Services
{
    /// <summary>
    /// Executes silent installers
    /// </summary>
    public class InstallService
    {
        private readonly LoggerService _logger;
        private readonly AppConfig _config;

        public InstallService(LoggerService logger, AppConfig config)
        {
            _logger = logger;
            _config = config;
        }

        public async Task<InstallResult> InstallAsync(AppManifest app, string installerPath)
        {
            var startTime = DateTime.Now;

            try
            {
                _logger.Info($"Starting installation: {app.Name}");

                // Pre-install actions
                if (app.Advanced?.PreInstall != null)
                {
                    ExecutePreInstall(app.Advanced.PreInstall);
                }

                // Install based on type
                InstallResult result = app.Install.Type.ToLower() switch
                {
                    "exe" => await InstallExe(app, installerPath),
                    "msi" => await InstallMsi(app, installerPath),
                    "zip" => await InstallZip(app, installerPath),
                    _ => new InstallResult
                    {
                        Success = false,
                        Message = $"Unknown installer type: {app.Install.Type}"
                    }
                };

                result.Duration = DateTime.Now - startTime;

                if (result.Success)
                {
                    _logger.Info($"Installation successful: {app.Name} (duration: {result.Duration.TotalSeconds:F1}s)");

                    // Post-install actions
                    if (app.Advanced?.PostInstall != null)
                    {
                        ExecutePostInstall(app.Advanced.PostInstall);
                    }
                }
                else
                {
                    _logger.Error($"Installation failed: {app.Name} - {result.Message}");
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.Error($"Installation error for {app.Name}: {ex.Message}");
                return new InstallResult
                {
                    Success = false,
                    Message = "Installation exception",
                    ErrorDetails = ex.Message,
                    Duration = DateTime.Now - startTime
                };
            }
        }

        private async Task<InstallResult> InstallExe(AppManifest app, string installerPath)
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = installerPath,
                    Arguments = app.Install.SilentArgs,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                if (app.Install.RequireAdmin)
                {
                    startInfo.Verb = "runas";
                    startInfo.UseShellExecute = true;
                    startInfo.RedirectStandardOutput = false;
                    startInfo.RedirectStandardError = false;
                }

                _logger.Info($"Executing: {installerPath} {app.Install.SilentArgs}");

                using var process = Process.Start(startInfo);
                if (process == null)
                {
                    return new InstallResult
                    {
                        Success = false,
                        Message = "Failed to start installer process"
                    };
                }

                var timeoutMs = app.Install.Timeout * 1000;
                var completed = await Task.Run(() => process.WaitForExit(timeoutMs));

                if (!completed)
                {
                    _logger.Error($"Installation timeout after {app.Install.Timeout}s");
                    try
                    {
                        process.Kill();
                    }
                    catch { }

                    return new InstallResult
                    {
                        Success = false,
                        Message = $"Installation timeout ({app.Install.Timeout}s)",
                        ExitCode = -1
                    };
                }

                var exitCode = process.ExitCode;
                _logger.Info($"Installer exit code: {exitCode}");

                // Exit code 0 = success, 3010 = success but reboot required
                var success = exitCode == 0 || exitCode == 3010;

                return new InstallResult
                {
                    Success = success,
                    Message = success ? "Installation completed" : $"Installation failed with exit code {exitCode}",
                    ExitCode = exitCode
                };
            }
            catch (Exception ex)
            {
                return new InstallResult
                {
                    Success = false,
                    Message = "EXE installation error",
                    ErrorDetails = ex.Message
                };
            }
        }

        private async Task<InstallResult> InstallMsi(AppManifest app, string installerPath)
        {
            try
            {
                // MSI uses msiexec
                var args = $"/i \"{installerPath}\" {app.Install.SilentArgs}";

                var startInfo = new ProcessStartInfo
                {
                    FileName = "msiexec.exe",
                    Arguments = args,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                _logger.Info($"Executing: msiexec.exe {args}");

                using var process = Process.Start(startInfo);
                if (process == null)
                {
                    return new InstallResult
                    {
                        Success = false,
                        Message = "Failed to start msiexec"
                    };
                }

                var timeoutMs = app.Install.Timeout * 1000;
                var completed = await Task.Run(() => process.WaitForExit(timeoutMs));

                if (!completed)
                {
                    _logger.Error($"MSI installation timeout after {app.Install.Timeout}s");
                    try
                    {
                        process.Kill();
                    }
                    catch { }

                    return new InstallResult
                    {
                        Success = false,
                        Message = $"Installation timeout ({app.Install.Timeout}s)",
                        ExitCode = -1
                    };
                }

                var exitCode = process.ExitCode;
                _logger.Info($"MSI exit code: {exitCode}");

                // MSI exit codes: 0 = success, 3010 = reboot required
                var success = exitCode == 0 || exitCode == 3010;

                return new InstallResult
                {
                    Success = success,
                    Message = success ? "Installation completed" : $"Installation failed with exit code {exitCode}",
                    ExitCode = exitCode
                };
            }
            catch (Exception ex)
            {
                return new InstallResult
                {
                    Success = false,
                    Message = "MSI installation error",
                    ErrorDetails = ex.Message
                };
            }
        }

        private async Task<InstallResult> InstallZip(AppManifest app, string installerPath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(app.Install.InstallDir))
                {
                    return new InstallResult
                    {
                        Success = false,
                        Message = "ZIP installation requires install_dir to be specified"
                    };
                }

                var targetDir = Environment.ExpandEnvironmentVariables(app.Install.InstallDir);
                Directory.CreateDirectory(targetDir);

                _logger.Info($"Extracting ZIP to: {targetDir}");

                await Task.Run(() =>
                {
                    if (!string.IsNullOrWhiteSpace(app.Install.Password))
                    {
                        // Password-protected ZIP extraction requires third-party library
                        _logger.Warn("Password-protected ZIP requires SharpZipLib package");
                        throw new NotSupportedException("Password-protected ZIP extraction not yet implemented. Use SharpZipLib for password support.");
                    }
                    else
                    {
                        ZipFile.ExtractToDirectory(installerPath, targetDir, overwriteFiles: true);
                    }
                });

                _logger.Info("ZIP extraction complete");

                return new InstallResult
                {
                    Success = true,
                    Message = "Portable app extracted successfully",
                    ExitCode = 0
                };
            }
            catch (Exception ex)
            {
                return new InstallResult
                {
                    Success = false,
                    Message = "ZIP extraction error",
                    ErrorDetails = ex.Message
                };
            }
        }

        private void ExecutePreInstall(PreInstallConfig preInstall)
        {
            // Kill processes
            if (preInstall.KillProcesses != null)
            {
                foreach (var processName in preInstall.KillProcesses)
                {
                    try
                    {
                        var processes = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(processName));
                        foreach (var process in processes)
                        {
                            _logger.Info($"Killing process: {processName}");
                            process.Kill();
                            process.WaitForExit(5000);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Warn($"Failed to kill process {processName}: {ex.Message}");
                    }
                }
            }

            // Cleanup files/folders
            if (preInstall.Cleanup != null)
            {
                foreach (var path in preInstall.Cleanup)
                {
                    try
                    {
                        var expandedPath = Environment.ExpandEnvironmentVariables(path);

                        if (Directory.Exists(expandedPath))
                        {
                            _logger.Info($"Deleting directory: {expandedPath}");
                            Directory.Delete(expandedPath, recursive: true);
                        }
                        else if (File.Exists(expandedPath))
                        {
                            _logger.Info($"Deleting file: {expandedPath}");
                            File.Delete(expandedPath);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Warn($"Failed to cleanup {path}: {ex.Message}");
                    }
                }
            }
        }

        private void ExecutePostInstall(PostInstallConfig postInstall)
        {
            // Create shortcuts (simplified implementation)
            if (postInstall.Shortcuts != null)
            {
                foreach (var shortcut in postInstall.Shortcuts)
                {
                    _logger.Info($"Post-install: Create shortcut {shortcut.Name} (not implemented)");
                    // TODO: Implement shortcut creation using IWshRuntimeLibrary
                }
            }
        }
    }
}
