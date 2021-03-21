using OpenQA.Selenium;

namespace Ingestion.PageObjects
{
    public class MemberListItem : PageObject
    {
        private readonly IWebElement _memberListItemElem;

        private IWebElement NameText => _memberListItemElem.FindElement(By.CssSelector("td:nth-child(1) > span:nth-child(2)"));
        private IWebElement XPText => _memberListItemElem.FindElement(By.CssSelector("td:nth-child(2)"));
        private IWebElement KillsText => _memberListItemElem.FindElement(By.CssSelector("td:nth-child(3)"));
        private IWebElement DeathsText => _memberListItemElem.FindElement(By.CssSelector("td:nth-child(4)"));
        private IWebElement KDRText => _memberListItemElem.FindElement(By.CssSelector("td:nth-child(5)"));

        public MemberListItem(IWebElement memberListItemElem, IWebDriver webDriver, string url) : base(webDriver, url)
        {
            _memberListItemElem = memberListItemElem;
        }

        public string Name()
        {
            return NameText.Text;
        }

        public void Click()
        {
            _memberListItemElem.Click();
        }

        protected override void AssertOnScreenImpl()
        {
            throw new System.NotImplementedException();
        }
    }
}