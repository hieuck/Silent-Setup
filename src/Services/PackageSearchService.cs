using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using SilentSetup.Models;

namespace SilentSetup.Services;

public class PackageSearchResult
{
    public string Name { get; set; } = "";
    public string Id { get; set; } = "";
    public string Description { get; set; } = "";
    public string DownloadUrl { get; set; } = "";
    public string SilentArgs { get; set; } = "";
    public string Publisher { get; set; } = "";
    public string Version { get; set; } = "";
    public string Source { get; set; } = ""; // "chocolatey", "winget", "manual"
    public string InstallerType { get; set; } = "exe";
}

public class PackageSearchService
{
    private readonly HttpClient _httpClient;

    public PackageSearchService()
    {
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "SilentSetup/1.0");
    }

    public async Task<List<PackageSearchResult>> SearchPackagesAsync(string query)
    {
        var results = new List<PackageSearchResult>();

        // Search from Chocolatey
        var chocoResults = await SearchChocolateyAsync(query);
        results.AddRange(chocoResults);

        // Search from Winget (using community API)
        var wingetResults = await SearchWingetAsync(query);
        results.AddRange(wingetResults);

        return results;
    }

    private async Task<List<PackageSearchResult>> SearchChocolateyAsync(string query)
    {
        var results = new List<PackageSearchResult>();

        try
        {
            // Chocolatey OData API
            var url = $"https://community.chocolatey.org/api/v2/Search()?$filter=IsLatestVersion&$top=10&searchTerm='{Uri.EscapeDataString(query)}'&targetFramework=''";

            var response = await _httpClient.GetStringAsync(url);

            // Parse XML response (OData returns Atom XML)
            var doc = System.Xml.Linq.XDocument.Parse(response);
            var ns = System.Xml.Linq.XNamespace.Get("http://www.w3.org/2005/Atom");
            var nsM = System.Xml.Linq.XNamespace.Get("http://schemas.microsoft.com/ado/2007/08/dataservices/metadata");
            var nsD = System.Xml.Linq.XNamespace.Get("http://schemas.microsoft.com/ado/2007/08/dataservices");

            foreach (var entry in doc.Descendants(ns + "entry"))
            {
                var properties = entry.Element(nsM + "properties");
                if (properties == null) continue;

                var id = properties.Element(nsD + "Id")?.Value ?? "";
                var title = entry.Element(ns + "title")?.Value ?? id;
                var summary = entry.Element(ns + "summary")?.Value ?? "";
                var version = properties.Element(nsD + "Version")?.Value ?? "";
                var authors = properties.Element(nsD + "Authors")?.Value ?? "";

                // Get package details for download URL and silent args
                var packageUrl = $"https://community.chocolatey.org/api/v2/package/{id}/{version}";

                results.Add(new PackageSearchResult
                {
                    Name = title,
                    Id = id.ToLowerInvariant(),
                    Description = summary,
                    DownloadUrl = packageUrl,
                    SilentArgs = "/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /SP-", // Common Chocolatey defaults
                    Publisher = authors,
                    Version = version,
                    Source = "chocolatey",
                    InstallerType = "exe"
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Chocolatey search error: {ex.Message}");
        }

        return results;
    }

    private async Task<List<PackageSearchResult>> SearchWingetAsync(string query)
    {
        var results = new List<PackageSearchResult>();

        try
        {
            // Use winget-pkgs-submission REST API (unofficial but works)
            var url = $"https://api.github.com/search/code?q={Uri.EscapeDataString(query)}+repo:microsoft/winget-pkgs+path:manifests+extension:yaml&per_page=10";

            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));

            var response = await _httpClient.GetStringAsync(url);
            var json = JsonDocument.Parse(response);

            if (json.RootElement.TryGetProperty("items", out var items))
            {
                foreach (var item in items.EnumerateArray())
                {
                    if (item.TryGetProperty("path", out var path))
                    {
                        var pathStr = path.GetString() ?? "";

                        // Extract package ID from path: manifests/p/Publisher/PackageName/...
                        var match = Regex.Match(pathStr, @"manifests/./([^/]+)/([^/]+)/");
                        if (match.Success)
                        {
                            var publisher = match.Groups[1].Value;
                            var packageName = match.Groups[2].Value;

                            results.Add(new PackageSearchResult
                            {
                                Name = packageName,
                                Id = $"{publisher}.{packageName}".ToLowerInvariant(),
                                Description = $"From Winget: {publisher} {packageName}",
                                DownloadUrl = "", // Would need to fetch manifest details
                                SilentArgs = "/S", // Common silent arg
                                Publisher = publisher,
                                Version = "",
                                Source = "winget",
                                InstallerType = "exe"
                            });
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Winget search error: {ex.Message}");
        }

        return results;
    }

    public async Task<PackageSearchResult?> FetchPackageDetailsAsync(PackageSearchResult package)
    {
        if (package.Source == "chocolatey")
        {
            return await FetchChocolateyDetailsAsync(package);
        }
        else if (package.Source == "winget")
        {
            return await FetchWingetDetailsAsync(package);
        }

        return package;
    }

    private async Task<PackageSearchResult?> FetchChocolateyDetailsAsync(PackageSearchResult package)
    {
        try
        {
            // Get nuspec for more details
            var nuspecUrl = $"https://community.chocolatey.org/api/v2/package/{package.Id}";

            // Chocolatey packages are NuGet packages - would need to extract .nuspec
            // For now, return enhanced guesses based on package name

            if (package.Id.Contains("7zip"))
            {
                package.SilentArgs = "/S";
                package.DownloadUrl = "https://www.7-zip.org/a/7z2408-x64.exe";
            }
            else if (package.Id.Contains("vlc"))
            {
                package.SilentArgs = "/S";
                package.DownloadUrl = "https://get.videolan.org/vlc/last/win64/vlc-3.0.21-win64.exe";
            }
            else if (package.Id.Contains("notepadplusplus"))
            {
                package.SilentArgs = "/S";
                package.DownloadUrl = "https://github.com/notepad-plus-plus/notepad-plus-plus/releases/download/v8.7.1/npp.8.7.1.Installer.x64.exe";
            }

            return package;
        }
        catch
        {
            return package;
        }
    }

    private async Task<PackageSearchResult?> FetchWingetDetailsAsync(PackageSearchResult package)
    {
        // Would need to fetch actual manifest YAML from GitHub
        return package;
    }
}
