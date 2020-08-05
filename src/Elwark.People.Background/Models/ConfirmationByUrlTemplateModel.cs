using Elwark.People.Background.Services;

namespace Elwark.People.Background.Models
{
    public class ConfirmationByUrlTemplateModel : ITemplateModel
    {
        public ConfirmationByUrlTemplateModel(string url) =>
            Url = url;

        public string Url { get; }
    }
}