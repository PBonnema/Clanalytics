using OpenQA.Selenium;
using System.Threading;
using System.Threading.Tasks;

namespace Ingestion.PageObjects
{
    public class App
    {
        public static Page CurrentPage { get; set; }
        public static string BaseUrl { get; private set; }

        public static async Task<ClanPage> ViewClanPageAsync(string clanTag, IWebDriver webDriver, string baseUrl, CancellationToken cancellation = default)
        {
            BaseUrl = baseUrl;
            var page = new ClanPage(webDriver, clanTag, false);
            await Task.Run(() => webDriver.Navigate().GoToUrl(page.URL), cancellation);
            page.AssertOnScreen();
            return page;
        }

        public static async Task<PlayerPage> ViewPlayerPageAsync(string playerName, IWebDriver webDriver, string baseUrl, CancellationToken cancellation = default)
        {
            BaseUrl = baseUrl;
            var page = new PlayerPage(webDriver, playerName, false);
            await Task.Run(() => webDriver.Navigate().GoToUrl(page.URL), cancellation);
            page.AssertOnScreen();
            return page;
        }
    }
}