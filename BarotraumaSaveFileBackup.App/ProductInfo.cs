using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Runtime.InteropServices;

namespace BarotraumaSaveFileBackup.App
{
    internal static class ProductInfo
    {
        public const string ProductName = "BarotraumaSaveFileBackupApp";

        public static Version? ProductVersion => Assembly.GetEntryAssembly()?.GetName().Version;

        internal const string LATEST_RELEASE_URL = "https://api.github.com/repos/Dimi1010/BarotraumaSaveFileBackup/releases/latest";

        public static async Task<Version?> GetLatestVersionAsync(CancellationToken cancellationToken = default)
        {
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.UserAgent.Add(
                new System.Net.Http.Headers.ProductInfoHeaderValue(ProductName, ProductVersion?.ToString()));

            var responseBody = await httpClient.GetStreamAsync(LATEST_RELEASE_URL, cancellationToken);
            var responseValues = await JsonSerializer.DeserializeAsync<Dictionary<string, dynamic>>(responseBody, cancellationToken: cancellationToken);
            string? versionTag = ((JsonElement)responseValues?["tag_name"]).GetString();

            Version.TryParse(versionTag?[1..], out var version);
            return version;
        }
    }
}
