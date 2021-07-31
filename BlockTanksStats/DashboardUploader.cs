using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.Core;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;

namespace BlockTanksStats
{
    public class DashboardUploader
    {
        private readonly IDiscordRestWebhookAPI _API;
        private readonly Snowflake _webhookId;
        private readonly string _webhookToken;

        public DashboardUploader(IDiscordRestWebhookAPI API, Snowflake webhookId, string webhookToken)
        {
            _API = API;
            _webhookId = webhookId;
            _webhookToken = webhookToken;
        }

        public async Task UploadAsync(string dashboardsPath, CancellationToken cancellation = default)
        {
            var zipFile = "dashboards.zip";
            File.Delete(zipFile);
            ZipFile.CreateFromDirectory(dashboardsPath, zipFile);
            try
            {
                using var zipStream = new FileStream(zipFile, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true);
                var file = new FileData("Dashboards.zip", zipStream);
                await _API.ExecuteWebhookAsync(_webhookId, _webhookToken, shouldWait: true, file: file, ct: cancellation);
            } finally
            {
                File.Delete(zipFile);
            }
        }
    }
}
