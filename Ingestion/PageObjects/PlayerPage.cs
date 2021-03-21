using OpenQA.Selenium;
using System;
using System.Linq;

namespace Ingestion.PageObjects
{
    public class PlayerPage : BlockTanksCommunityPage
    {
        private IWebElement StatsTable => WebDriver.FindElements(By.CssSelector("#statsTable")).FirstOrDefault();
        private IWebElement HoursPlayedField => WebDriver.FindElement(By.CssSelector("#statsTable > tbody:nth-child(1) > tr:nth-child(2) > td:nth-child(2) > bignumber:nth-child(1)"));
        private IWebElement KillsField => WebDriver.FindElement(By.CssSelector("#statsTable > tbody:nth-child(1) > tr:nth-child(3) > td:nth-child(1) > bignumber:nth-child(1)"));
        private IWebElement DeathsField => WebDriver.FindElement(By.CssSelector("#statsTable > tbody:nth-child(1) > tr:nth-child(3) > td:nth-child(2) > bignumber:nth-child(1)"));
        private IWebElement BulletsFiredField => WebDriver.FindElement(By.CssSelector("#statsTable > tbody:nth-child(1) > tr:nth-child(4) > td:nth-child(1) > bignumber:nth-child(1)"));
        private IWebElement XPPerHourField => WebDriver.FindElement(By.CssSelector("#statsTable > tbody:nth-child(1) > tr:nth-child(5) > td:nth-child(1) > bignumber:nth-child(1)"));

        public PlayerPage(IWebDriver webDriver, string playerName, bool assertOnScreen = true) : base(webDriver, $"user/{Uri.EscapeDataString(playerName.ToLower())}", assertOnScreen)
        { }

        public bool AreStatsHidden()
        {
            return StatsTable == null;
        }

        public double HoursPlayed()
        {
            return double.Parse(HoursPlayedField.Text);
        }

        public int Kills()
        {
            return int.Parse(KillsField.Text);
        }

        public int Deaths()
        {
            return int.Parse(DeathsField.Text);
        }

        public int BulletsFired()
        {
            return int.Parse(BulletsFiredField.Text);
        }

        public double XPPerHour()
        {
            return double.Parse(XPPerHourField.Text);
        }
    }
}
