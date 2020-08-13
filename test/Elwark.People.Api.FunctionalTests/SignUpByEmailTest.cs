using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Bogus;
using Elwark.People.Abstractions;
using Elwark.People.Api.Application.Models;
using Elwark.People.Api.Application.ProblemDetails;
using Elwark.People.Api.Requests;
using Elwark.People.Domain.ErrorCodes;
using Elwark.People.Shared.Primitives;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;

namespace Elwark.People.Api.FunctionalTests
{
    public class SignUpByEmailTest : ScenarioBase
    {
        private readonly ITestOutputHelper _testOutputHelper;
        private const string ControllerName = "signup/email";
        private static readonly Faker Faker = new Faker();

        public SignUpByEmailTest(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        [Fact]
        public async Task Create_account_by_email_ok_response()
        {
            var data = new SignUpByEmailRequest(
                new Identification.Email(Faker.Internet.Email()),
                Faker.Internet.Password(),
                new UrlTemplate("http://localhost/{marker}", "{marker}")
            );

            using var server = CreateServer();
            var httpResponse = await server.CreateClient()
                .PostAsync(ControllerName,
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
            Assert.NotNull(result.Name);
            Assert.Equal(result.Name, data.Email.GetUser());
            Assert.True(result.AccountId.Value > 0);
            Assert.NotEmpty(result.Identities);
            Assert.Collection(result.Identities, response =>
            {
                Assert.False(response.ConfirmedAt.HasValue);
                Assert.NotNull(response.Identification);
                Assert.Equal(response.Identification, data.Email);
                Assert.NotEqual(Guid.Empty, response.IdentityId.Value);
            });
        }

        [Fact]
        public async Task Create_account_with_null_email_error_response()
        {
            var data = new
            {
                Email = (string?) null,
                Password = Faker.Internet.Password(),
                ConfirmationUrl = new UrlTemplate("http://localhost/{marker}", "{marker}")
            };

            using var server = CreateServer();
            var httpResponse = await server.CreateClient()
                .PostAsync(ControllerName,
                    new StringContent(
                        JsonConvert.SerializeObject(data),
                        Encoding.UTF8,
                        "application/json"
                    ),
                    CancellationToken.None
                );

            var json = await httpResponse.Content.ReadAsStringAsync();
            _testOutputHelper.WriteLine(json);

            Assert.Throws<HttpRequestException>(() => httpResponse.EnsureSuccessStatusCode());

            var result = JsonConvert.DeserializeObject<ValidationProblemDetails>(json);

            Assert.NotNull(result);
            Assert.Equal(StatusCodes.Status400BadRequest, result.Status);
            Assert.Equal(nameof(CommonError), result.Title);
            Assert.Equal(CommonError.InvalidModelState.ToString(), result.Type);
            Assert.True(result.Errors.Count == 1);
            Assert.Collection(result.Errors, pair => Assert.Equal("email", pair.Key));
        }

        [Fact]
        public async Task Create_account_with_empty_email_error_response()
        {
            var data = new
            {
                Email = string.Empty,
                Password = Faker.Internet.Password(),
                ConfirmationUrl = new UrlTemplate("http://localhost/{marker}", "{marker}")
            };

            using var server = CreateServer();
            var httpResponse = await server.CreateClient()
                .PostAsync(ControllerName,
                    new StringContent(
                        JsonConvert.SerializeObject(data),
                        Encoding.UTF8,
                        "application/json"
                    ),
                    CancellationToken.None
                );

            var json = await httpResponse.Content.ReadAsStringAsync();
            _testOutputHelper.WriteLine(json);

            Assert.Throws<HttpRequestException>(() => httpResponse.EnsureSuccessStatusCode());

            var result = JsonConvert.DeserializeObject<ValidationProblemDetails>(json);

            Assert.NotNull(result);
            Assert.Equal(StatusCodes.Status400BadRequest, result.Status);
            Assert.Equal(nameof(CommonError), result.Title);
            Assert.Equal(CommonError.InvalidModelState.ToString(), result.Type);
            Assert.True(result.Errors.Count == 1);
            Assert.Collection(result.Errors, pair => Assert.Equal("identification", pair.Key));
        }

        [Fact]
        public async Task Create_account_with_null_password_error_response()
        {
            var data = new
            {
                Email = new Identification.Email(Faker.Internet.Email()),
                Password = (string?) null,
                ConfirmationUrl = new UrlTemplate("http://localhost/{marker}", "{marker}")
            };

            using var server = CreateServer();
            var httpResponse = await server.CreateClient()
                .PostAsync(ControllerName,
                    new StringContent(
                        JsonConvert.SerializeObject(data),
                        Encoding.UTF8,
                        "application/json"
                    ),
                    CancellationToken.None
                );

            var json = await httpResponse.Content.ReadAsStringAsync();
            _testOutputHelper.WriteLine(json);

            Assert.Throws<HttpRequestException>(() => httpResponse.EnsureSuccessStatusCode());

            var result = JsonConvert.DeserializeObject<ValidationProblemDetails>(json);

            Assert.NotNull(result);
            Assert.Equal(StatusCodes.Status400BadRequest, result.Status);
            Assert.Equal(nameof(CommonError), result.Title);
            Assert.Equal(CommonError.InvalidModelState.ToString(), result.Type);
            Assert.True(result.Errors.Count == 1);
            Assert.Collection(result.Errors, pair => Assert.Equal("password", pair.Key));
        }

        [Fact]
        public async Task Create_account_with_empty_password_error_response()
        {
            var data = new SignUpByEmailRequest(
                new Identification.Email(Faker.Internet.Email()),
                string.Empty,
                new UrlTemplate("http://localhost/{marker}", "{marker}")
            );

            using var server = CreateServer();
            var httpResponse = await server.CreateClient()
                .PostAsync(ControllerName,
                    new StringContent(
                        JsonConvert.SerializeObject(data),
                        Encoding.UTF8,
                        "application/json"
                    ),
                    CancellationToken.None
                );

            var json = await httpResponse.Content.ReadAsStringAsync();
            _testOutputHelper.WriteLine(json);

            Assert.Throws<HttpRequestException>(() => httpResponse.EnsureSuccessStatusCode());

            var result = JsonConvert.DeserializeObject<ElwarkProblemDetails>(json);

            Assert.NotNull(result);
            Assert.Equal(StatusCodes.Status400BadRequest, result.Status);
            Assert.Equal(nameof(CommonError), result.Title);
            Assert.Equal(CommonError.InvalidModelState.ToString(), result.Type);
        }

        [Fact]
        public async Task Create_account_with_empty_identities_error_response()
        {
            var data = new
            {
                Email = string.Empty,
                Password = string.Empty,
                ConfirmationUrl = new UrlTemplate("http://localhost/{marker}", "{marker}")
            };

            using var server = CreateServer();
            var httpResponse = await server.CreateClient()
                .PostAsync(ControllerName,
                    new StringContent(
                        JsonConvert.SerializeObject(data),
                        Encoding.UTF8,
                        "application/json"
                    ),
                    CancellationToken.None
                );

            var json = await httpResponse.Content.ReadAsStringAsync();
            _testOutputHelper.WriteLine(json);

            Assert.Throws<HttpRequestException>(() => httpResponse.EnsureSuccessStatusCode());

            var result = JsonConvert.DeserializeObject<ValidationProblemDetails>(json);

            Assert.NotNull(result);
            Assert.Equal(StatusCodes.Status400BadRequest, result.Status);
            Assert.Equal(nameof(CommonError), result.Title);
            Assert.Equal(CommonError.InvalidModelState.ToString(), result.Type);
            Assert.True(result.Errors.Count == 1);
            Assert.Collection(result.Errors, pair => Assert.Equal("identification", pair.Key));
        }

        [Fact]
        public async Task Create_account_with_null_identities_error_response()
        {
            var data = new
            {
                Email = (string?) null,
                Password = (string?) null,
                ConfirmationUrl = new UrlTemplate("http://localhost/{marker}", "{marker}")
            };

            using var server = CreateServer();
            var httpResponse = await server.CreateClient()
                .PostAsync(ControllerName,
                    new StringContent(
                        JsonConvert.SerializeObject(data),
                        Encoding.UTF8,
                        "application/json"
                    ),
                    CancellationToken.None
                );

            var json = await httpResponse.Content.ReadAsStringAsync();
            _testOutputHelper.WriteLine(json);

            Assert.Throws<HttpRequestException>(() => httpResponse.EnsureSuccessStatusCode());

            var result = JsonConvert.DeserializeObject<ValidationProblemDetails>(json);

            Assert.NotNull(result);
            Assert.Equal(StatusCodes.Status400BadRequest, result.Status);
            Assert.Equal(nameof(CommonError), result.Title);
            Assert.Equal(CommonError.InvalidModelState.ToString(), result.Type);
            Assert.True(result.Errors.Count == 2);

            var errors = new[] {"email", "password"};
            Assert.Contains(result.Errors.Keys, s => errors.Contains(s));
        }
    }
}