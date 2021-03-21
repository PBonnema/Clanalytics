using FluentAssertions;
using FluentAssertions.Extensions;
using OpenQA.Selenium;

namespace Ingestion.PageObjects
{
    public abstract class PageObject
    {
        public string URL { get; private set; }
        public IWebDriver WebDriver { get; private set; }

        protected PageObject(IWebDriver webDriver, string url)
        {
            URL = url;
            WebDriver = webDriver;
        }

        public void AssertOnScreen(double waitTimeSec = 3.0, double pollIntervalSec = 0.1) =>
            this.Invoking(p => p.AssertOnScreenImpl()).Should().NotThrowAfter(waitTimeSec.Seconds(), pollIntervalSec.Seconds());

        protected abstract void AssertOnScreenImpl();
    }
}