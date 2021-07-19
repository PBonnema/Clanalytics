using OpenQA.Selenium;
using System.Collections.Generic;
using System.Linq;

namespace Ingestion.PageObjects
{
    public class ClanPage : BlockTanksCommunityPage
    {
        private static readonly string CLAN_DOESNT_EXIST_MESSAGE = "No clan exists with that name!";

        private IWebElement MemberList => WebDriver.FindElement(By.CssSelector(".table-sortable > tbody:nth-child(2)"));
        private IWebElement ClanDoesntExistMessage => WebDriver.FindElement(By.CssSelector(".contentWrapper > tbody:nth-child(1) > tr:nth-child(1) > td:nth-child(1) > h2:nth-child(2)"));

        public ClanPage(IWebDriver webDriver, string clanTag, bool assertOnScreen = true) : base(webDriver, $"clan/{clanTag}", assertOnScreen)
        { }

        public IEnumerable<MemberListItem> MemberListItems()
        {
            return MemberList.FindElements(By.CssSelector("tr.clickAble"))
                .Select(e => new MemberListItem(e, WebDriver, URL));
        }

        public bool Exists()
        {
            return ClanDoesntExistMessage.Text != CLAN_DOESNT_EXIST_MESSAGE;
        }
    }
}
