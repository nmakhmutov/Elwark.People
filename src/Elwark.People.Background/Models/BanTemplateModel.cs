using Elwark.People.Background.Services;

namespace Elwark.People.Background.Models
{
    public class BanTemplateModel : ITemplateModel
    {
        public BanTemplateModel(string reason, string type)
        {
            Reason = reason;
            Type = type;
        }

        public string Reason { get; }

        public string Type { get; }
    }
}