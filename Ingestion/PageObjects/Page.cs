using FluentAssertions;
using OpenQA.Selenium;

namespace Ingestion.PageObjects
{
    public abstract class Page : PageObject
    {
        protected Page(IWebDriver webDriver, string url, bool assertOnScreen) : base(webDriver, url)
        {
            App.CurrentPage = this;
            if (assertOnScreen)
            {
                AssertOnScreen();
            }
        }

        protected override void AssertOnScreenImpl() => WebDriver.Url.Should().Be(URL);
    }
}