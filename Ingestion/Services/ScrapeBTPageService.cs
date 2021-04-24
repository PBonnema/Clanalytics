using Ingestion.PageObjects;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Remote;
using Polly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ingestion.Services
{
    public class ScrapeBTPageService : IScrapeBTPageService
    {
        public record SeleniumConfig(bool UseRemoteSeleniumChrome, string SeleniumChromeUrl, int SeleniumConnectionRetries, double SeleniumConnectionRetryPeriodSec);

        private static IWebDriver ConfigureWebDriver(SeleniumConfig seleniumConfig)
        {
            var chromeOptions = new ChromeOptions();

            chromeOptions.AddArguments(new List<string> {
                "--disable-web-security", // Disable CORS check in browser
            });
            chromeOptions.SetLoggingPreference(LogType.Browser, LogLevel.All);

            IWebDriver driver = null;
            if (seleniumConfig.UseRemoteSeleniumChrome)
            {
                chromeOptions.AddArguments(new List<string> {
                    "--headless",
                    "--disable-dev-shm-usage",
                    "--no-sandbox",
                });

                // Connect to the selenium container with retries.
                var policy = Policy.Handle<WebDriverException>()
                  .WaitAndRetry(seleniumConfig.SeleniumConnectionRetries,
                      r => TimeSpan.FromSeconds(seleniumConfig.SeleniumConnectionRetryPeriodSec));
                policy.Execute(() => driver = new RemoteWebDriver(new Uri(seleniumConfig.SeleniumChromeUrl), chromeOptions));
            }
            else
            {
                driver = new ChromeDriver(chromeOptions);
            }

            if (driver != null)
            {
                driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(3);
                return driver;
            }
            else
            {
                throw new Exception("Could not connect to the selenium container.");
            }
        }

        private readonly IWebDriver _webDriver;
        private readonly string _baseUrl;

        public ScrapeBTPageService(SeleniumConfig configuration, string baseUrl)
        {
            _webDriver = ConfigureWebDriver(configuration);
            _baseUrl = baseUrl;
        }

        public async Task<IList<string>> GetClanMembersAsync(string clanTag, CancellationToken cancellation = default)
        {
            var memberListItems = (await App.ViewClanPageAsync(clanTag, _webDriver, _baseUrl, cancellation)).MemberListItems();
            return memberListItems.Select(m => m.Name()).ToList();
        }

        public async Task<bool> ArePlayerStatsHiddenAsync(string playerName, CancellationToken cancellation = default)
        {
            return (await App.ViewPlayerPageAsync(playerName, _webDriver, _baseUrl, cancellation)).AreStatsHidden();
        }

        public void Dispose()
        {
            _webDriver.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
