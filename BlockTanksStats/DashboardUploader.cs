using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Rest.Core;
using Serilog;
using System;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Syncfusion.XlsIO;
using Syncfusion.XlsIORenderer;
using System.Linq;

namespace BlockTanksStats
{
    public class DashboardUploader
    {
        private readonly IDiscordRestWebhookAPI _webhookAPI;
        private readonly IDiscordRestChannelAPI _channelAPI;
        private readonly Snowflake _webhookId;
        private readonly string _webhookToken;
        private readonly ILogger _logger;

        public DashboardUploader(IDiscordRestWebhookAPI webhookAPI, IDiscordRestChannelAPI channelAPI, Snowflake webhookId, string webhookToken, ILogger logger)
        {
            _webhookAPI = webhookAPI;
            _channelAPI = channelAPI;
            _webhookId = webhookId;
            _webhookToken = webhookToken;
            _logger = logger;
        }

        public async Task UploadAsync(string dashboardsPath, IEnumerable<string> dashboarsToPublishAsImage, CancellationToken cancellation = default)
        {
            var zipFile = "dashboards.zip";
            File.Delete(zipFile);
            ZipFile.CreateFromDirectory(dashboardsPath, zipFile);
            try
            {
                using var zipStream = new FileStream(zipFile, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true); // Buffersize of 4096 is default
                var file = new FileData("Dashboards.zip", zipStream);
                var result = await _webhookAPI.ExecuteWebhookAsync(_webhookId, _webhookToken, shouldWait: true,
                    attachments: new List<OneOf.OneOf<FileData, IPartialAttachment>> { file },
                    ct: cancellation);
                if (!result.IsSuccess)
                {
                    throw new Exception($"Posting the dashboards using the webhook failed: {result.Error?.Message}");
                }

                await PostDashboardScreenShotsToThread(dashboardsPath, result.Entity, dashboarsToPublishAsImage, cancellation);
            } finally
            {
                File.Delete(zipFile);
            }
        }

        private async Task PostDashboardScreenShotsToThread(
            string dashboardsPath, IMessage message, IEnumerable<string> dashboarsToPublishAsImage, CancellationToken cancellation = default)
        {
            var resultThread = await _channelAPI.StartThreadWithMessageAsync(message.ChannelID, message.ID, "Screenshots", AutoArchiveDuration.Day, reason: "Automated thread by clan-analytics bot", ct: cancellation);
            if (!resultThread.IsSuccess)
            {
                throw new Exception($"Posting the dashboards using the webhook failed: {resultThread.Error?.Message}");
            }

            var thread = resultThread.Entity;
            foreach (var file in Directory.GetFiles(dashboardsPath))
            {
                if (dashboarsToPublishAsImage.Any(t => file.EndsWith(t)))
                {
                    _logger.Information($"Posting images of {file}");
                    try
                    {
                        var imageFiles = ImagesFromDashboard(file);
                        foreach (var (imageStream, name) in imageFiles)
                        {
                            var fileData = new FileData(Path.ChangeExtension(file, "png"), imageStream);
                            var resultMessage = await _channelAPI.CreateMessageAsync(
                                thread.ID,
                                content: $"{Path.GetFileNameWithoutExtension(file)} {name}",
                                attachments: new List<OneOf.OneOf<FileData, IPartialAttachment>> { fileData },
                                ct: cancellation);
                            if (!resultMessage.IsSuccess)
                            {
                                _logger.Error($"Posting an image for dashboard {file} in a thread failed: {resultMessage.Error?.Message}");
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        _logger.Error($"Exception while posting an image for dashboard {file} in a thread failed: {e}");
                    }
                }
                else
                {
                    _logger.Debug($"Skipping posting images of {file}");
                }
            }
        }

        private static IEnumerable<(Stream, string)> ImagesFromDashboard(string dashboardFile)
        {
            using var excelEngine = new ExcelEngine();
            Syncfusion.XlsIO.IApplication application = excelEngine.Excel;
            application.DefaultVersion = ExcelVersion.Xlsx;
            using var excelStream = new FileStream(dashboardFile, FileMode.Open, FileAccess.Read);
            IWorkbook workbook = application.Workbooks.Open(excelStream);

            var renderer = new XlsIORenderer();
            renderer.ChartRenderingOptions.ImageFormat = ExportImageFormat.Png;
            var exportImageOptions = new ExportImageOptions { ImageFormat = ExportImageFormat.Png, ScalingMode = ScalingMode.Normal };

            foreach (var sheet in workbook.Worksheets)
            {
                var fileName = $"{Path.GetFileNameWithoutExtension(dashboardFile)}-{sheet.Name}.png";
                using (var fileStream = new FileStream(fileName, FileMode.OpenOrCreate))
                {
                    renderer.ConvertToImage(sheet.UsedRange, exportImageOptions, fileStream);
                }

                using (var fileStream = new FileStream(fileName, FileMode.Open))
                {
                    yield return (fileStream, sheet.Name);
                }
                File.Delete(fileName);
            }

            yield break;
        }
    }
}
