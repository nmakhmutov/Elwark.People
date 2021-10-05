using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using MongoDB.Driver;
using Newtonsoft.Json;
using People.Infrastructure;
using People.Infrastructure.Countries;
using People.Infrastructure.Forbidden;

namespace People.Api.Infrastructure;

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

            await _dbContext.Countries.InsertManyAsync(countries, new InsertManyOptions { IsOrdered = false });
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

            await _dbContext.ForbiddenItems.InsertManyAsync(passwords, new InsertManyOptions { IsOrdered = false });
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

            await _dbContext.ForbiddenItems.InsertManyAsync(emails, new InsertManyOptions { IsOrdered = false });
        }
    }
}
