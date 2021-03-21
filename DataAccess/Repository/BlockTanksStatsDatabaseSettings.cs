namespace DataAccess.Repository
{
    public record BlockTanksStatsDatabaseSettings(
        string ConnectionString,
        string DatabaseName,
        string PlayersCollectionName,
        string ClansCollectionName
    );
}
