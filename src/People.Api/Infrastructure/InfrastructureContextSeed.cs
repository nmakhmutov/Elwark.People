using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using MongoDB.Driver;
using People.Infrastructure;
using People.Infrastructure.Blacklist;
using People.Infrastructure.Countries;

namespace People.Api.Infrastructure;

internal sealed class InfrastructureContextSeed
{
    private readonly InfrastructureDbContext _dbContext;
    private readonly IWebHostEnvironment _env;
    private readonly CountOptions _countOptions;

    public InfrastructureContextSeed(InfrastructureDbContext dbContext, IWebHostEnvironment env)
    {
        _dbContext = dbContext;
        _env = env;
        _countOptions = new CountOptions { Limit = 1,  };
    }

    public async Task SeedAsync()
    {
        await SeedCountriesAsync();
        await SeedBlacklistAsync();
    }

    private async Task SeedCountriesAsync()
    {
        var path = Path.Combine(_env.ContentRootPath, "Setup", "countries.json");

        if (!File.Exists(path))
            return;

        var count = await _dbContext.Countries
            .CountDocumentsAsync(FilterDefinition<Country>.Empty, _countOptions);

        if (count > 0)
            return;

        var json = await File.ReadAllTextAsync(path, Encoding.UTF8);
        var countries = JsonSerializer.Deserialize<Country[]>(json);

        await _dbContext.Countries.InsertManyAsync(countries, new InsertManyOptions { IsOrdered = false });
    }

    private async Task SeedBlacklistAsync()
    {
        var count = await _dbContext.Blacklist
            .CountDocumentsAsync(FilterDefinition<BlacklistItem>.Empty, _countOptions);

        if (count > 0)
            return;

        var list = new List<BlacklistItem>();

        var passwordsPath = Path.Combine(_env.ContentRootPath, "Setup", "forbidden_passwords.csv");
        if (File.Exists(passwordsPath))
            list.AddRange((await File.ReadAllLinesAsync(passwordsPath))
                .Select(x => new BlacklistItem(ForbiddenType.Password, x)));

        var emailsPath = Path.Combine(_env.ContentRootPath, "Setup", "forbidden_email_hosts.csv");
        if (File.Exists(emailsPath))
            list.AddRange((await File.ReadAllLinesAsync(emailsPath))
                .Select(x => new BlacklistItem(ForbiddenType.EmailHost, x)));

        if (list.Count == 0)
            return;

        await _dbContext.Blacklist.InsertManyAsync(list, new InsertManyOptions { IsOrdered = false });
    }
}
