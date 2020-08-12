using System;
using System.Threading.Tasks;
using Elwark.People.Abstractions;
using Elwark.People.Background.Services.Gravatar;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Elwark.People.Background.FunctionalTests.Services
{
    public class GravatarServiceClientTests : ScenarioBase
    {
        [Fact]
        public async Task Get_available_profile_success()
        {
            using var server = CreateServer();
            var gravatar = server.Services.GetService<IGravatarService>();

            var profile = await gravatar.GetAsync(new Notification.PrimaryEmail("zywyqoehwshadevdjd@ttirv.org"));
            
            Assert.NotNull(profile);
            Assert.NotNull(profile.Name);
        }

        [Fact]
        public async Task Get_non_available_profile_error()
        {
            using var server = CreateServer();
            var gravatar = server.Services.GetService<IGravatarService>();

            var profile = await gravatar.GetAsync(new Notification.PrimaryEmail($"{Guid.NewGuid()}@test.com"));
            
            Assert.Null(profile);
        }
    }
}