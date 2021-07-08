using System.Globalization;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace People.Gateway.Infrastructure
{
    public class LocalizationMessageHandler : DelegatingHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            request.Headers.Add("Language", CultureInfo.CurrentCulture.TwoLetterISOLanguageName);
            return await base.SendAsync(request, ct);
        }
    }
}
