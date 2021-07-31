using BlockTanksStats.ViewModels;
using ClosedXML.Report;
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
            CancellationToken cancellation = default)
        {
            Console.WriteLine($"Saving dashboard {Path.GetFileNameWithoutExtension(dashboardFile)}...");
            await viewModel.OnGenerateAsync(now, days, periodLengthDays, cancellation);
            using var template = new XLTemplate(viewModel.TemplateFile);
            template.AddVariable(viewModel);
            template.Generate();
            template.SaveAs(dashboardFile);
            Console.WriteLine($"Saved dashboard {Path.GetFileNameWithoutExtension(dashboardFile)}...");
        }
    }
}
