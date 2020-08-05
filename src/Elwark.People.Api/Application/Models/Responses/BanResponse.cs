using System.Diagnostics;

namespace Elwark.People.Api.Application.Models.Responses
{
    public class BanResponse
    {
        [DebuggerStepThrough]
        public BanResponse(bool isBanned, BanDetailsResponse? details)
        {
            IsBanned = isBanned;
            Details = details;
        }

        public bool IsBanned { get; }

        public BanDetailsResponse? Details { get; }
    }
}