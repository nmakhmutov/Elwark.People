using Elwark.People.Background.Services;

namespace Elwark.People.Background.Models
{
    public class ConfirmationByCodeTemplateModel : ITemplateModel
    {
        public ConfirmationByCodeTemplateModel(long code) =>
            Code = code;

        public long Code { get; }
    }
}