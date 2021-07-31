using DataAccess.Repository;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BlockTanksStats.ViewModels
{
    public abstract class ViewModel
    {
        public string TemplateFile { get; }

        protected IClanRepository ClanRepository { get; }
        protected IPlayerRepository PlayerRepository { get; }

        protected ViewModel(IClanRepository clanRepository, IPlayerRepository playerRepository, string templateFile)
        {
            ClanRepository = clanRepository;
            PlayerRepository = playerRepository;
            TemplateFile = templateFile;
        }

        public abstract Task OnGenerateAsync(DateTime now, int days, int periodLengthDays, CancellationToken cancellation);
    }
}