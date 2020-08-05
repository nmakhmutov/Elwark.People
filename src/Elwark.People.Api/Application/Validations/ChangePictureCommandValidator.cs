using System.Net.Http;
using Elwark.People.Api.Application.Commands;
using FluentValidation;

namespace Elwark.People.Api.Application.Validations
{
    public class ChangePictureCommandValidator : AbstractValidator<ChangePictureCommand>
    {
        public ChangePictureCommandValidator(IHttpClientFactory factory) =>
            RuleFor(x => x.Picture)
                .MustAsync(async (uri, token) =>
                {
                    if (uri is null)
                        return true;

                    var client = factory.CreateClient();

                    var result = await client.GetAsync(uri);

                    return result.IsSuccessStatusCode &&
                           result.Content.Headers.ContentType.MediaType.StartsWith("image/");
                })
                .WithMessage("Incorrect image url");
    }
}