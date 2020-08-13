using System;
using System.Globalization;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Bogus;
using Elwark.Extensions;
using Elwark.People.Abstractions;
using Elwark.People.Api.Application.Models;
using Elwark.People.Api.Infrastructure.Services.Facebook;
using Elwark.People.Api.Infrastructure.Services.Google;
using Elwark.People.Api.Infrastructure.Services.Microsoft;
using Elwark.People.Api.Requests;
using Elwark.People.Shared.Primitives;
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;

namespace Elwark.People.Api.FunctionalTests
{
    public class SignUpBySocialTest : ScenarioBase
    {
        private readonly ITestOutputHelper _testOutputHelper;
        private static readonly Faker Faker = new Faker();

        private static string GetControllerName(IdentificationType type) =>
            $"signup/{type}";

        private static UrlTemplate UrlTemplate =>
            new UrlTemplate("http://localhost/{m}", "{m}");

        public SignUpBySocialTest(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        [Fact]
        public async Task Create_account_by_google_ok_response()
        {
            var google = new Identification.Google(Faker.Random.ULong(long.MaxValue).ToString());
            var email = new Identification.Email(Faker.Internet.Email());
            var isConfirmed = Faker.Random.Bool();

            var token = JsonConvert.SerializeObject(
                new GoogleAccount(
                    google,
                    email,
                    isConfirmed,
                    Faker.Person.FirstName,
                    Faker.Person.LastName,
                    Faker.Image.PicsumUrl().ToUri(),
                    new CultureInfo("fr")
                )
            );
            var data = new SignUpByGoogleRequest(google, token, email, UrlTemplate);

            using var server = CreateServer();
            var httpResponse = await server.CreateClient()
                .PostAsync(GetControllerName(google.Type),
                    new StringContent(
                        JsonConvert.SerializeObject(data),
                        Encoding.UTF8,
                        "application/json"
                    ),
                    CancellationToken.None
                );

            var json = await httpResponse.Content.ReadAsStringAsync();
            _testOutputHelper.WriteLine(json);

            httpResponse.EnsureSuccessStatusCode();

            var result = JsonConvert.DeserializeObject<SignUpModel>(json);

            Assert.NotNull(result);
            Assert.True(result.AccountId.Value > 0);
            Assert.Equal(result.Name, email.GetUser());
            Assert.NotEmpty(result.Identities);
            Assert.Collection(result.Identities,
                response =>
                {
                    Assert.Equal(isConfirmed, response.ConfirmedAt.HasValue);
                    Assert.Equal(response.Identification.Value, email.Value);
                    Assert.NotEqual(Guid.Empty, response.IdentityId.Value);
                },
                response =>
                {
                    Assert.True(response.ConfirmedAt.HasValue);
                    Assert.Equal(response.Identification, google);
                    Assert.NotEqual(Guid.Empty, response.IdentityId.Value);
                });
        }

        [Fact]
        public async Task Create_account_by_facebook_ok_response()
        {
            var facebook = new Identification.Facebook(Faker.Random.ULong(long.MaxValue).ToString());
            var email = new Identification.Email(Faker.Internet.Email());

            var token = JsonConvert.SerializeObject(
                new FacebookAccount(
                    facebook,
                    email,
                    Gender.Male,
                    Faker.Date.Past(),
                    Faker.Person.FirstName,
                    Faker.Person.LastName,
                    new Uri(Faker.Image.PicsumUrl()),
                    Faker.Image.PicsumUrl().ToUri()
                )
            );

            var data = new SignUpByFacebookRequest(facebook, token, email, UrlTemplate);

            using var server = CreateServer();
            var httpResponse = await server.CreateClient()
                .PostAsync(GetControllerName(facebook.Type),
                    new StringContent(
                        JsonConvert.SerializeObject(data),
                        Encoding.UTF8,
                        "application/json"
                    ),
                    CancellationToken.None
                );

            var json = await httpResponse.Content.ReadAsStringAsync();
            _testOutputHelper.WriteLine(json);

            httpResponse.EnsureSuccessStatusCode();

            var result = JsonConvert.DeserializeObject<SignUpModel>(json);

            Assert.NotNull(result);
            Assert.True(result.AccountId.Value > 0);
            Assert.Equal(result.Name, email.GetUser());
            Assert.NotEmpty(result.Identities);
            Assert.Collection(result.Identities,
                response =>
                {
                    Assert.False(response.ConfirmedAt.HasValue);
                    Assert.Equal(response.Identification.Value, email.Value);
                    Assert.NotEqual(Guid.Empty, response.IdentityId.Value);
                },
                response =>
                {
                    Assert.True(response.ConfirmedAt.HasValue);
                    Assert.Equal(response.Identification, facebook);
                    Assert.NotEqual(Guid.Empty, response.IdentityId.Value);
                });
        }

        [Fact]
        public async Task Create_account_by_microsoft_ok_response()
        {
            var microsoft = new Identification.Microsoft(Faker.Random.ULong(long.MaxValue).ToString());
            var email = new Identification.Email(Faker.Internet.Email());

            var token = JsonConvert.SerializeObject(
                new MicrosoftAccount(
                    microsoft,
                    email,
                    Faker.Person.FirstName,
                    Faker.Person.LastName
                )
            );
            var data = new SignUpByMicrosoftRequest(microsoft, token, email, UrlTemplate);

            using var server = CreateServer();
            var httpResponse = await server.CreateClient()
                .PostAsync(GetControllerName(microsoft.Type),
                    new StringContent(
                        JsonConvert.SerializeObject(data),
                        Encoding.UTF8,
                        "application/json"
                    ),
                    CancellationToken.None
                );

            var json = await httpResponse.Content.ReadAsStringAsync();
            _testOutputHelper.WriteLine(json);

            httpResponse.EnsureSuccessStatusCode();

            var result = JsonConvert.DeserializeObject<SignUpModel>(json);

            Assert.NotNull(result);
            Assert.True(result.AccountId.Value > 0);
            Assert.Equal(result.Name, email.GetUser());
            Assert.NotEmpty(result.Identities);
            Assert.Collection(result.Identities,
                response =>
                {
                    Assert.True(response.ConfirmedAt.HasValue);
                    Assert.Equal(response.Identification.Value, email.Value);
                    Assert.NotEqual(Guid.Empty, response.IdentityId.Value);
                }, 
                response =>
                {
                    Assert.True(response.ConfirmedAt.HasValue);
                    Assert.Equal(response.Identification, microsoft);
                    Assert.NotEqual(Guid.Empty, response.IdentityId.Value);
                });
        }
    }
}