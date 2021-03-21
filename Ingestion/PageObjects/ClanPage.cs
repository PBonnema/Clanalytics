using OpenQA.Selenium;
using System.Collections.Generic;
using System.Linq;

namespace Ingestion.PageObjects
{
    public class ClanPage : BlockTanksCommunityPage
    {
        private IWebElement MemberList => WebDriver.FindElement(By.CssSelector(".table-sortable > tbody:nth-child(2)"));

        public ClanPage(IWebDriver webDriver, string clanTag, bool assertOnScreen = true) : base(webDriver, $"clan/{clanTag}", assertOnScreen)
        { }

        public IEnumerable<MemberListItem> MemberListItems()
        {
            return MemberList.FindElements(By.CssSelector("tr.clickAble"))
                .Select(e => new MemberListItem(e, WebDriver, URL));
        }
    }
}
