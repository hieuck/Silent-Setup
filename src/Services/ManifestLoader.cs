using System.IO;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using SilentSetup.Models;

namespace SilentSetup.Services
{
    /// <summary>
    /// Loads and validates app and patch manifests from YAML files
    /// </summary>
    public class ManifestLoader
    {
        private readonly LoggerService _logger;
        private readonly IDeserializer _yamlDeserializer;

        public ManifestLoader(LoggerService logger)
        {
            _logger = logger;
            _yamlDeserializer = new DeserializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .IgnoreUnmatchedProperties()
                .Build();
        }

        public List<AppManifest> LoadApps(string appsDirectory = "apps")
        {
            var manifests = new List<AppManifest>();

            if (!Directory.Exists(appsDirectory))
            {
                _logger.Warn($"Apps directory not found: {appsDirectory}");
                return manifests;
            }

            var yamlFiles = Directory.GetFiles(appsDirectory, "*.yaml")
                .Concat(Directory.GetFiles(appsDirectory, "*.yml"))
                .Where(f => !Path.GetFileName(f).StartsWith("_")); // Skip templates

            foreach (var file in yamlFiles)
            {
                try
                {
                    var yaml = File.ReadAllText(file);
                    var manifest = _yamlDeserializer.Deserialize<AppManifest>(yaml);

                    if (ValidateAppManifest(manifest))
                    {
                        manifests.Add(manifest);
                        _logger.Info($"Loaded app manifest: {manifest.Name} ({manifest.Id})");
                    }
                    else
                    {
                        _logger.Error($"Invalid app manifest: {file}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error($"Failed to load app manifest {file}: {ex.Message}");
                }
            }

            _logger.Info($"Loaded {manifests.Count} app manifests");
            return manifests;
        }

        public List<PatchManifest> LoadPatches(string patchesDirectory = "patches")
        {
            var patches = new List<PatchManifest>();

            if (!Directory.Exists(patchesDirectory))
            {
                _logger.Warn($"Patches directory not found: {patchesDirectory}");
                return patches;
            }

            // Support both folder structure (local) and flat YAML files (remote cache)
            // 1. Check for flat YAML files first (remote cache format)
            var yamlFiles = Directory.GetFiles(patchesDirectory, "*.yaml")
                .Concat(Directory.GetFiles(patchesDirectory, "*.yml"))
                .Where(f => !Path.GetFileName(f).StartsWith("_"));

            foreach (var file in yamlFiles)
            {
                try
                {
                    var yaml = File.ReadAllText(file);
                    var manifest = _yamlDeserializer.Deserialize<PatchManifest>(yaml);

                    if (ValidatePatchManifest(manifest))
                    {
                        patches.Add(manifest);
                        _logger.Info($"Loaded patch manifest: {manifest.Name} ({manifest.Id}) for {manifest.TargetApp}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error($"Failed to load patch from {file}: {ex.Message}");
                }
            }

            // 2. Check for folder structure (local format)
            var patchDirs = Directory.GetDirectories(patchesDirectory)
                .Where(d => !Path.GetFileName(d).StartsWith("_")); // Skip templates

            foreach (var dir in patchDirs)
            {
                var manifestFile = Path.Combine(dir, "manifest.yaml");
                if (!File.Exists(manifestFile))
                {
                    manifestFile = Path.Combine(dir, "manifest.yml");
                }

                if (!File.Exists(manifestFile))
                {
                    _logger.Warn($"No manifest found in patch directory: {dir}");
                    continue;
                }

                try
                {
                    var yaml = File.ReadAllText(manifestFile);
                    var patch = _yamlDeserializer.Deserialize<PatchManifest>(yaml);
                    patch.PatchDirectory = dir;

                    if (ValidatePatchManifest(patch))
                    {
                        patches.Add(patch);
                        _logger.Info($"Loaded patch manifest: {patch.Name} ({patch.Id}) for {patch.TargetApp}");
                    }
                    else
                    {
                        _logger.Error($"Invalid patch manifest: {manifestFile}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error($"Failed to load patch manifest {manifestFile}: {ex.Message}");
                }
            }

            _logger.Info($"Loaded {patches.Count} patch manifests");
            return patches;
        }

        private bool ValidateAppManifest(AppManifest manifest)
        {
            if (string.IsNullOrWhiteSpace(manifest.Name))
            {
                _logger.Error("App manifest missing required field: name");
                return false;
            }

            if (string.IsNullOrWhiteSpace(manifest.Id))
            {
                _logger.Error($"App manifest {manifest.Name} missing required field: id");
                return false;
            }

            if (string.IsNullOrWhiteSpace(manifest.Homepage))
            {
                _logger.Error($"App manifest {manifest.Name} missing required field: homepage");
                return false;
            }

            if (string.IsNullOrWhiteSpace(manifest.Download.Url))
            {
                _logger.Error($"App manifest {manifest.Name} missing required field: download.url");
                return false;
            }

            if (!manifest.Download.Url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                _logger.Error($"App manifest {manifest.Name}: download URL must use HTTPS");
                return false;
            }

            if (string.IsNullOrWhiteSpace(manifest.Install.SilentArgs) && manifest.Install.Type != "zip")
            {
                _logger.Error($"App manifest {manifest.Name} missing required field: install.silent_args");
                return false;
            }

            return true;
        }

        private bool ValidatePatchManifest(PatchManifest patch)
        {
            if (string.IsNullOrWhiteSpace(patch.Name))
            {
                _logger.Error("Patch manifest missing required field: name");
                return false;
            }

            if (string.IsNullOrWhiteSpace(patch.Id))
            {
                _logger.Error($"Patch manifest {patch.Name} missing required field: id");
                return false;
            }

            if (string.IsNullOrWhiteSpace(patch.TargetApp))
            {
                _logger.Error($"Patch manifest {patch.Name} missing required field: target_app");
                return false;
            }

            // Validate type-specific requirements
            switch (patch.Type.ToLower())
            {
                case "copy-files":
                    if (patch.Files == null || !patch.Files.Any())
                    {
                        _logger.Error($"Patch {patch.Name}: type 'copy-files' requires 'files' array");
                        return false;
                    }
                    break;

                case "executable":
                    if (patch.Execute == null || string.IsNullOrWhiteSpace(patch.Execute.File))
                    {
                        _logger.Error($"Patch {patch.Name}: type 'executable' requires 'execute.file'");
                        return false;
                    }
                    break;

                case "registry":
                    if (patch.Registry == null || !patch.Registry.Any())
                    {
                        _logger.Error($"Patch {patch.Name}: type 'registry' requires 'registry' array");
                        return false;
                    }
                    break;

                case "archive":
                    if (patch.Archive == null || string.IsNullOrWhiteSpace(patch.Archive.File))
                    {
                        _logger.Error($"Patch {patch.Name}: type 'archive' requires 'archive.file'");
                        return false;
                    }
                    break;

                case "download-extract":
                    if (patch.Download == null || string.IsNullOrWhiteSpace(patch.Download.Url))
                    {
                        _logger.Error($"Patch {patch.Name}: type 'download-extract' requires 'download.url'");
                        return false;
                    }
                    if (patch.Files == null || !patch.Files.Any())
                    {
                        _logger.Error($"Patch {patch.Name}: type 'download-extract' requires 'files' array");
                        return false;
                    }
                    break;

                default:
                    _logger.Error($"Patch {patch.Name}: unknown type '{patch.Type}'");
                    return false;
            }

            return true;
        }
    }
}
