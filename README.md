# BlockTanks Clanalytics

Clananalytics is designed to enable BlockTanks clan owners to make data-driven decisions while managing their clan. The system is able to track statistics about players. You can specify a list of players to track or specify a list of clans, of which the members will be tracked (no stats on the clans themselves).
Three different kinds of Excel dashboards are generated. One for each clan, one for all individually specified players (which are treated as being in the same clan), and a dashboard that tracks the Clan leaderboard (several per-clan stats are kept track of). These are then uploaded straight to a channel in your Discord server.

By default, the dashboards show data of the last 14 days.

Clanalytics was created to support the RIOT clan. Currently, it will only upload dashboards in the RIOT Discord server. There are no plans to extend this to enable access for clans.

How it works:
The system polls the BlockTanks API to fetch player data, persists it in a database on your own computer, generate Excel dashboards from it, and post them to Discord. It does this every day.

## Used technologies
Both services are written in C# and are designed to run locally on your own system in Docker Linux containers with Docker Compose.

One service runs every day to fetch player statistic from the BlockTanks API and saves them in a MongoDB database. It does this with a combination of scraping the API and scraping the community website using Selenium.

The other service also runs every day and creates dashboards from this data as Excel sheets. Then it creates images from these excel sheet and posts both the images and the dashboards to a channel in the RIOT discord server. The Excel dashboards are defined and generated using the MVVM pattern. The used Excel library allows for the design of Excel templates with databinding.

The services are separated to allow them to be scheduled at different times/frequencies. Scheduling of the services is done by creating a task for each in the Windows task scheduler.

## Disclaimer
This is an unofficial, fan-made system and has no ties to the Blocktanks game.
