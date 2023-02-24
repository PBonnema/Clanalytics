using BlockTanksStats.ViewModels;
using ClosedXML.Report;
using Serilog;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace BlockTanksStats
{
    public static class DashboardGenerator
    {
        public static async Task GenerateAsync(
            ViewModel viewModel,
            string dashboardFile,
            DateTime now,
            int days,
            int periodLengthDays,
            ILogger logger,
            CancellationToken cancellation = default)
        {
            logger.Information($"Saving dashboard {Path.GetFileNameWithoutExtension(dashboardFile)}...");
            await viewModel.OnGenerateAsync(now, days, periodLengthDays, cancellation);
            using var template = new XLTemplate(viewModel.TemplateFile);
            template.AddVariable(viewModel);
            template.Generate(); // TODO: bug. This cannot handle viewModels with empty collections to display. Will result in nullreference exception.
            template.Workbook.NamedRanges.DeleteAll();
            template.SaveAs(dashboardFile, true);
            logger.Information($"Saved dashboard {Path.GetFileNameWithoutExtension(dashboardFile)}...");
        }
    }
}
