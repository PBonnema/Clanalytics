using OpenQA.Selenium;

namespace Ingestion.PageObjects
{
    public abstract class BlockTanksCommunityPage : Page
    {
        public BlockTanksCommunityPage(IWebDriver webDriver, string relativePath, bool assertOnScreen)
            : base(webDriver, $"{App.BaseUrl}/{relativePath}", assertOnScreen)
        { }
    }
}