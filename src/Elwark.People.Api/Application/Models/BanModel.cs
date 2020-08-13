using System.Diagnostics;

namespace Elwark.People.Api.Application.Models
{
    public class BanModel
    {
        [DebuggerStepThrough]
        public BanModel(bool isBanned, BanDetail? details)
        {
            IsBanned = isBanned;
            Details = details;
        }

        public bool IsBanned { get; }

        public BanDetail? Details { get; }
    }
}