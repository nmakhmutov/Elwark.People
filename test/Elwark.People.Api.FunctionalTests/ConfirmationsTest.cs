using System;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Bogus;
using Elwark.People.Abstractions;
using Elwark.People.Api.Application.Models.Requests;
using Elwark.People.Api.Application.Models.Responses;
using Elwark.People.Api.Application.ProblemDetails;
using Elwark.People.Api.Infrastructure.Security;
using Elwark.People.Api.Infrastructure.Services.Confirmation;
using Elwark.People.Domain.AggregatesModel.AccountAggregate;
using Elwark.People.Domain.ErrorCodes;
using Elwark.People.Infrastructure;
using Elwark.People.Infrastructure.Cache;
using Elwark.People.Infrastructure.Confirmation;
using Elwark.People.Shared.Primitives;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;

namespace Elwark.People.Api.FunctionalTests
{
    public class ConfirmationsTest : ScenarioBase
    {
        private readonly ITestOutputHelper _testOutputHelper;
        private readonly Faker _faker = new Faker();

        public ConfirmationsTest(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        private const string ControllerName = "confirmations";

        [Fact]
        public async Task Get_by_token_success()
        {
            using var server = CreateServer();

            var email = new Identification.Email(_faker.Internet.Email());
            var account = new Account(
                new Name(_faker.Internet.UserName()),
                new CultureInfo("en"),
                new Uri(_faker.Image.LoremFlickrUrl())
            );
            var validator = server.Services.GetService<IIdentificationValidator>();
            await account.AddIdentificationAsync(email, true, validator);
            var identity = account.Identities.First();

            await using var context = server.Services.GetService<OAuthContext>();
            await context.Accounts.AddAsync(account);
            await context.SaveChangesAsync();

            var dbSet = context.Set<ConfirmationModel>();
            var model = new ConfirmationModel(
                identity.Id,
                ConfirmationType.ConfirmIdentity,
                _faker.Random.Long(),
                _faker.Date.Future()
            );
            await dbSet.AddAsync(model);
            await context.SaveChangesAsync();

            var service = server.Services.GetService<IConfirmationService>();
            var token = service.WriteToken(model.Id, model.IdentityId, model.Type, model.Code);

            var httpResponse = await server.CreateClient()
                .GetAsync($"{ControllerName}?token={WebUtility.UrlEncode(token)}");

            var json = await httpResponse.Content.ReadAsStringAsync();
            _testOutputHelper.WriteLine(json);

            httpResponse.EnsureSuccessStatusCode();

            var result = JsonConvert.DeserializeObject<ConfirmationResponse>(json);
            Assert.NotNull(result);
            Assert.Equal(email, result.Identification);
        }

        [Fact]
        public async Task Get_confirmation_by_null_token_fail()
        {
            using var server = CreateServer();
            var httpResponse = await server.CreateClient()
                .GetAsync(ControllerName);

            var json = await httpResponse.Content.ReadAsStringAsync();
            _testOutputHelper.WriteLine(json);

            Assert.Throws<HttpRequestException>(() => httpResponse.EnsureSuccessStatusCode());

            var result = JsonConvert.DeserializeObject<ValidationProblemDetails>(json);
            Assert.NotNull(result);
            Assert.Equal(nameof(CommonError), result.Title);
            Assert.Equal(CommonError.InvalidModelState.ToString(), result.Type);
            Assert.True(result.Errors.Count == 1);
        }

        [Fact]
        public async Task Get_confirmation_by_empty_token_fail()
        {
            using var server = CreateServer();
            var httpResponse = await server.CreateClient()
                .GetAsync($"{ControllerName}?token=");

            var json = await httpResponse.Content.ReadAsStringAsync();
            _testOutputHelper.WriteLine(json);

            Assert.Throws<HttpRequestException>(() => httpResponse.EnsureSuccessStatusCode());

            var result = JsonConvert.DeserializeObject<ValidationProblemDetails>(json);
            Assert.NotNull(result);
            Assert.Equal(nameof(CommonError), result.Title);
            Assert.Equal(CommonError.InvalidModelState.ToString(), result.Type);
            Assert.True(result.Errors.Count == 1);
        }

        [Fact]
        public async Task Get_confirmation_by_wrong_token_fail()
        {
            using var server = CreateServer();
            var httpResponse = await server.CreateClient()
                .GetAsync($"{ControllerName}?token={_faker.Random.Hash()}");

            var json = await httpResponse.Content.ReadAsStringAsync();
            _testOutputHelper.WriteLine(json);

            Assert.Throws<HttpRequestException>(() => httpResponse.EnsureSuccessStatusCode());

            var result = JsonConvert.DeserializeObject<ElwarkProblemDetails>(json);
            Assert.NotNull(result);
            Assert.Equal(nameof(CryptographyError), result.Title);
            Assert.Equal(CryptographyError.DecoderError.ToString(), result.Type);
        }

        [Fact]
        public async Task Get_confirmation_by_non_existent_token_fail()
        {
            using var server = CreateServer();
            var service = server.Services.GetService<IConfirmationService>();
            var token = service.WriteToken(
                _faker.Random.Guid(),
                new IdentityId(_faker.Random.Guid()),
                ConfirmationType.ConfirmIdentity,
                _faker.Random.Long()
            );

            var httpResponse = await server.CreateClient()
                .GetAsync($"{ControllerName}?token={WebUtility.UrlEncode(token)}");

            var json = await httpResponse.Content.ReadAsStringAsync();
            _testOutputHelper.WriteLine(json);

            Assert.Throws<HttpRequestException>(() => httpResponse.EnsureSuccessStatusCode());

            var result = JsonConvert.DeserializeObject<ConfirmationProblemDetails>(json);
            Assert.NotNull(result);
            Assert.Equal(nameof(ConfirmationError), result.Title);
        }

        [Fact]
        public async Task Get_confirmation_by_non_crypt_value_token_fail()
        {
            using var server = CreateServer();
            var token = Convert.ToBase64String(Encoding.UTF8.GetBytes(_faker.Random.Hash()));

            var httpResponse = await server.CreateClient()
                .GetAsync($"{ControllerName}?token={WebUtility.UrlEncode(token)}");

            var json = await httpResponse.Content.ReadAsStringAsync();
            _testOutputHelper.WriteLine(json);

            Assert.Throws<HttpRequestException>(() => httpResponse.EnsureSuccessStatusCode());

            var result = JsonConvert.DeserializeObject<ConfirmationProblemDetails>(json);
            Assert.NotNull(result);
            Assert.Equal(nameof(CryptographyError), result.Title);
            Assert.Equal(CryptographyError.DecoderError.ToString(), result.Type);
        }

        [Fact]
        public async Task Get_confirmation_by_wrong_crypt_value_token_fail()
        {
            using var server = CreateServer();
            var encryption = server.Services.GetService<IDataEncryption>();
            var token = encryption.EncryptToString(new {data = _faker.Address.City()});

            var httpResponse = await server.CreateClient()
                .GetAsync($"{ControllerName}?token={WebUtility.UrlEncode(token)}");

            var json = await httpResponse.Content.ReadAsStringAsync();
            _testOutputHelper.WriteLine(json);

            Assert.Throws<HttpRequestException>(() => httpResponse.EnsureSuccessStatusCode());

            var result = JsonConvert.DeserializeObject<ConfirmationProblemDetails>(json);
            Assert.NotNull(result);
            Assert.Equal(nameof(ConfirmationError), result.Title);
            Assert.Equal(ConfirmationError.NotFound.ToString(), result.Type);
        }

        [Fact]
        public async Task Create_confirmation_success()
        {
            using var server = CreateServer();
            var cache = server.Services.GetService<ICacheStorage>();
            var validator = server.Services.GetService<IIdentificationValidator>();
            
            var email = new Identification.Email(_faker.Internet.Email());
            var account = new Account(
                new Name(_faker.Internet.UserName()),
                new CultureInfo("en"),
                new Uri(_faker.Image.LoremFlickrUrl())
            );
            await account.AddIdentificationAsync(email, validator);
            var identity = account.Identities.First();

            await using var context = server.Services.GetService<OAuthContext>();
            await context.Accounts.AddAsync(account);
            await context.SaveChangesAsync();

            var request = new SendConfirmationRequest(
                email,
                new UrlTemplate("http://localhost/{marker}", "{marker}")
            );

            var httpResponse = await server.CreateClient()
                .PostAsync(ControllerName,
                    new StringContent(
                        JsonConvert.SerializeObject(request),
                        Encoding.UTF8,
                        "application/json"
                    )
                );

            _testOutputHelper.WriteLine(await httpResponse.Content.ReadAsStringAsync());

            httpResponse.EnsureSuccessStatusCode();

            var c = await cache.ReadAsync<DateTimeOffset?>(
                $"Confirmation_Url_{identity.Id}_{account.GetPrimaryEmail()}_{ConfirmationType.ConfirmIdentity}"
            );

            Assert.True(c.HasValue);

            var confirmationModel = await context.Set<ConfirmationModel>()
                .FirstOrDefaultAsync(x => x.IdentityId == identity.Id);

            Assert.NotNull(confirmationModel);
            Assert.Equal(ConfirmationType.ConfirmIdentity, confirmationModel.Type);
        }

        [Fact]
        public async Task Create_confirmation_for_non_existent_identity_fail()
        {
            using var server = CreateServer();

            var email = new Identification.Email(_faker.Internet.Email());

            var request = new SendConfirmationRequest(
                email,
                new UrlTemplate("http://localhost/{marker}", "{marker}")
            );

            var httpResponse = await server.CreateClient()
                .PostAsync(ControllerName,
                    new StringContent(
                        JsonConvert.SerializeObject(request),
                        Encoding.UTF8,
                        "application/json"
                    )
                );

            var json = await httpResponse.Content.ReadAsStringAsync();
            _testOutputHelper.WriteLine(json);

            Assert.Throws<HttpRequestException>(() => httpResponse.EnsureSuccessStatusCode());

            var error = JsonConvert.DeserializeObject<IdentificationProblemDetails>(json);
            Assert.Equal(HttpStatusCode.NotFound, httpResponse.StatusCode);
            Assert.Equal(email, error.Identifier);
            Assert.Equal(nameof(IdentificationError), error.Title);
            Assert.Equal("NotFound", error.Type);
        }

        [Fact]
        public async Task Create_confirmation_for_null_identity_fail()
        {
            using var server = CreateServer();

            var httpResponse = await server.CreateClient()
                .PostAsync(ControllerName,
                    new StringContent(
                        JsonConvert.SerializeObject(
                            new
                            {
                                Email = (string?) null,
                                ConfirmationUrl = new UrlTemplate("http://localhost/{marker}", "{marker}")
                            }),
                        Encoding.UTF8,
                        "application/json"
                    )
                );

            var json = await httpResponse.Content.ReadAsStringAsync();
            _testOutputHelper.WriteLine(json);

            Assert.Throws<HttpRequestException>(() => httpResponse.EnsureSuccessStatusCode());

            var error = JsonConvert.DeserializeObject<ValidationProblemDetails>(json);
            Assert.Equal(HttpStatusCode.BadRequest, httpResponse.StatusCode);
            Assert.Equal(nameof(CommonError), error.Title);
            Assert.Equal(CommonError.InvalidModelState.ToString(), error.Type);
            Assert.True(error.Errors.Count == 1);
        }
    }
}