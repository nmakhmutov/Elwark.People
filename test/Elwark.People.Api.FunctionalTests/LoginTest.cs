using System;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Bogus;
using Elwark.People.Abstractions;
using Elwark.People.Api.Application.Models;
using Elwark.People.Api.Application.ProblemDetails;
using Elwark.People.Api.Requests;
using Elwark.People.Domain.AggregatesModel.AccountAggregate;
using Elwark.People.Domain.ErrorCodes;
using Elwark.People.Infrastructure;
using Elwark.People.Shared.Primitives;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Elwark.People.Api.FunctionalTests
{
    public class LoginTest : ScenarioBase
    {
        private readonly ITestOutputHelper _testOutputHelper;
        private readonly Faker _faker = new Faker();

        public LoginTest(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        private const string ControllerName = "signin";

        [Fact]
        public async Task Identify_by_email_ok_response()
        {
            using var server = CreateServer();
            var hasher = server.Services.GetRequiredService<IPasswordHasher>();
            var validator = server.Services.GetRequiredService<IPasswordValidator>();
            
            await using var context = server.Services.GetRequiredService<OAuthContext>();
            
            var email = new Identification.Email(_faker.Internet.Email());
            var password = _faker.Internet.Password();
            
            var account = new Account(
                new Name(email.GetUser()), 
                new CultureInfo("en"), 
                new Uri(_faker.Image.LoremPixelUrl())
            );
            
            await account.SetPasswordAsync(password, validator, hasher);
            
            var notificationValidator = server.Services.GetRequiredService<IIdentificationValidator>();
            await account.AddIdentificationAsync(email, true, notificationValidator);

            context.Accounts.Update(account);
            await context.SaveChangesAsync();

            var httpResponse = await server.CreateClient()
                .PostAsync(ControllerName,
                    new StringContent(
                        JsonConvert.SerializeObject(new SignInRequest(email, password)),
                        Encoding.UTF8,
                        "application/json"
                    )
                );

            var json = await httpResponse.Content.ReadAsStringAsync();
            _testOutputHelper.WriteLine(json);

            httpResponse.EnsureSuccessStatusCode();

            var response = JsonConvert.DeserializeObject<SignInModel>(json);
            Assert.NotNull(response);
            Assert.NotEqual(response.IdentityId, Guid.Empty);
            Assert.True(response.AccountId > 0);
        }

        [Fact]
        public async Task Identify_by_email_without_password_error_response()
        {
            using var server = CreateServer();

            var httpResponse = await server.CreateClient()
                .PostAsync(ControllerName,
                    new StringContent(
                        JsonConvert.SerializeObject(
                            new
                            {
                                Identification = new Identification.Email(_faker.Internet.Email()),
                                Verifier = (string?) null
                            }
                        ),
                        Encoding.UTF8,
                        "application/json"
                    )
                );

            var json = await httpResponse.Content.ReadAsStringAsync();
            _testOutputHelper.WriteLine(json);

            var error = JsonConvert.DeserializeObject<ValidationProblemDetails>(json);
            Assert.Equal(HttpStatusCode.BadRequest, httpResponse.StatusCode);
            Assert.Equal(nameof(CommonError), error.Title);
            Assert.Equal(CommonError.InvalidModelState.ToString(), error.Type);
            Assert.True(error.Errors.Count == 1);
        }

        [Fact]
        public async Task Identify_by_email_by_empty_password_error_response()
        {
            using var server = CreateServer();

            var httpResponse = await server.CreateClient()
                .PostAsync(ControllerName,
                    new StringContent(
                        JsonConvert.SerializeObject(
                            new SignInRequest(new Identification.Email(_faker.Internet.Email()), string.Empty)
                        ),
                        Encoding.UTF8,
                        "application/json"
                    )
                );

            var json = await httpResponse.Content.ReadAsStringAsync();
            _testOutputHelper.WriteLine(json);

            var error = JsonConvert.DeserializeObject<ValidationProblemDetails>(json);
            Assert.Equal(HttpStatusCode.BadRequest, httpResponse.StatusCode);
            Assert.Equal(nameof(CommonError), error.Title);
            Assert.Equal(CommonError.InvalidModelState.ToString(), error.Type);
            Assert.True(error.Errors.Count == 1);
        }

        [Fact]
        public async Task Identify_by_absent_email_error_response()
        {
            using var server = CreateServer();

            var httpResponse = await server.CreateClient()
                .PostAsync(ControllerName,
                    new StringContent(
                        JsonConvert.SerializeObject(new SignInRequest(new Identification.Email("notfoundemail@test.com"),
                            "incorectpassword")),
                        Encoding.UTF8,
                        "application/json"
                    )
                );

            var json = await httpResponse.Content.ReadAsStringAsync();
            _testOutputHelper.WriteLine(json);

            var error = JsonConvert.DeserializeObject<ValidationProblemDetails>(json);
            Assert.Equal(HttpStatusCode.NotFound, httpResponse.StatusCode);
            Assert.Equal(nameof(IdentificationError), error.Title);
        }

        [Fact]
        public async Task Identify_by_incorrect_email_error_response()
        {
            using var server = CreateServer();

            var httpResponse = await server.CreateClient()
                .PostAsync(ControllerName,
                    new StringContent(
                        new JObject
                            {
                                {nameof(SignInRequest.Identification), "Email:incorrectemail"},
                                {nameof(SignInRequest.Verifier), "incorectpassword"}
                            }
                            .ToString(),
                        Encoding.UTF8,
                        "application/json"
                    )
                );

            var json = await httpResponse.Content.ReadAsStringAsync();
            _testOutputHelper.WriteLine(json);

            var error = JsonConvert.DeserializeObject<ValidationProblemDetails>(json);
            Assert.Equal(HttpStatusCode.BadRequest, httpResponse.StatusCode);
            Assert.Equal(nameof(CommonError), error.Title);
            Assert.Equal(CommonError.InvalidModelState.ToString(), error.Type);
            Assert.True(error.Errors.Count == 1);
        }

        [Fact]
        public async Task Identify_by_correct_email_and_incorrect_password_error_response()
        {
            using var server = CreateServer();
            var hasher = server.Services.GetRequiredService<IPasswordHasher>();
            var validator = server.Services.GetRequiredService<IPasswordValidator>();
            
            await using var context = server.Services.GetRequiredService<OAuthContext>();

            var email = new Identification.Email(_faker.Internet.Email());
            var password = _faker.Internet.Password();
            
            var account = new Account(
                new Name(email.GetUser()), 
                new CultureInfo("en"), 
                new Uri(_faker.Image.LoremPixelUrl())
            );
            
            await account.SetPasswordAsync(password, validator, hasher);
            var notificationValidator = server.Services.GetRequiredService<IIdentificationValidator>();
            await account.AddIdentificationAsync(email, true, notificationValidator);

            context.Accounts.Update(account);
            await context.SaveChangesAsync();

            var httpResponse = await server.CreateClient()
                .PostAsync(ControllerName,
                    new StringContent(
                        JsonConvert.SerializeObject(new SignInRequest(email, _faker.Internet.Password())),
                        Encoding.UTF8,
                        "application/json"
                    )
                );

            var json = await httpResponse.Content.ReadAsStringAsync();
            _testOutputHelper.WriteLine(json);

            Assert.Throws<HttpRequestException>(() => httpResponse.EnsureSuccessStatusCode());

            var result = JsonConvert.DeserializeObject<ElwarkProblemDetails>(json);

            Assert.NotNull(result);
            Assert.Equal(nameof(PasswordError), result.Title);
            Assert.Equal(nameof(PasswordError.Mismatch), result.Type);
        }

        [Fact]
        public async Task Identify_banned_account_error_response()
        {
            var ban = new Ban(BanType.Temporarily, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(1),
                "Test reason");
            using var server = CreateServer();

            await using var context = server.Services.GetRequiredService<OAuthContext>();

            var email = new Identification.Email(_faker.Internet.Email());
            var account = new Account(
                new Name(email.GetUser()), 
                new CultureInfo("en"), 
                new Uri(_faker.Image.LoremPixelUrl())
            );
            var validator = server.Services.GetRequiredService<IIdentificationValidator>();
            await account.AddIdentificationAsync(email, true, validator);
            account.SetBan(ban);

            context.Accounts.Update(account);
            await context.SaveChangesAsync();

            var httpResponse = await server.CreateClient()
                .PostAsync(ControllerName,
                    new StringContent(
                        JsonConvert.SerializeObject(new SignInRequest(email, "incorectpassword")),
                        Encoding.UTF8,
                        "application/json"
                    )
                );

            var json = await httpResponse.Content.ReadAsStringAsync();
            _testOutputHelper.WriteLine(json);

            Assert.Throws<HttpRequestException>(() => httpResponse.EnsureSuccessStatusCode());

            var result = JsonConvert.DeserializeObject<AccountBlockedProblemDetails>(json);

            Assert.NotNull(result);
            Assert.Equal(account.Id, result.AccountId);
            Assert.Equal(ban.Type, result.BanType);
            Assert.Equal(ban.ExpiredAt, result.ExpiredAt);
            Assert.Equal(ban.Reason, result.Reason);
        }
    }
}