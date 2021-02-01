using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json;
using People.Infrastructure;
using People.Infrastructure.Countries;
using People.Infrastructure.Forbidden;
using People.Infrastructure.Timezones;

namespace People.Api.Infrastructure
{
    internal sealed class InfrastructureContextSeed
    {
        private readonly InfrastructureDbContext _dbContext;
        private readonly IWebHostEnvironment _env;

        public InfrastructureContextSeed(InfrastructureDbContext dbContext, IWebHostEnvironment env)
        {
            _dbContext = dbContext;
            _env = env;
        }

        public async Task SeedAsync()
        {
            await SeedCountriesAsync();
            await SeedTimezonesAsync();
            await SeedPasswordsAsync();
            await SeedEmailHostsAsync();
        }

        private async Task SeedCountriesAsync()
        {
            var path = Path.Combine(_env.ContentRootPath, "Setup", "countries.json");
            
            if (File.Exists(path))
            {
                var json = await File.ReadAllTextAsync(path, Encoding.UTF8);
                var countries = JsonConvert.DeserializeObject<Country[]>(json);

                await _dbContext.Countries.InsertManyAsync(countries, new InsertManyOptions {IsOrdered = false});
            }
        }

        private async Task SeedTimezonesAsync()
        {
            var path = Path.Combine(_env.ContentRootPath, "Setup", "timezones.csv");

            if (File.Exists(path))
            {
                var timezones = (await File.ReadAllLinesAsync(path, Encoding.UTF8))
                    .Select(x =>
                    {
                        var data = x.Split(',');
                        return new Timezone(ObjectId.Empty, data[0], TimeSpan.Parse(data[1]));
                    })
                    .ToArray();

                await _dbContext.Timezones.InsertManyAsync(timezones, new InsertManyOptions {IsOrdered = false});
            }
        }

        private async Task SeedPasswordsAsync()
        {
            var path = Path.Combine(_env.ContentRootPath, "Setup", "forbidden_passwords.csv");

            if (File.Exists(path))
            {
                var passwords = (await File.ReadAllLinesAsync(path, Encoding.UTF8))
                    .Select(x => new ForbiddenItem(ForbiddenType.Password, x))
                    .ToArray();

                await _dbContext.ForbiddenItems.InsertManyAsync(passwords, new InsertManyOptions {IsOrdered = false});
            }
        }

        private async Task SeedEmailHostsAsync()
        {
            var path = Path.Combine(_env.ContentRootPath, "Setup", "forbidden_email_hosts.csv");

            if (File.Exists(path))
            {
                var emails = (await File.ReadAllLinesAsync(path, Encoding.UTF8))
                    .Select(x => new ForbiddenItem(ForbiddenType.EmailHost, x))
                    .ToArray();

                await _dbContext.ForbiddenItems.InsertManyAsync(emails, new InsertManyOptions {IsOrdered = false});
            }
        }
    }
}