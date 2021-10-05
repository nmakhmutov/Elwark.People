using System;
using System.Threading;
using System.Threading.Tasks;
using Common.Mongo;
using MediatR;
using MongoDB.Driver;
using People.Api.Application.Models;
using People.Domain;
using People.Domain.Aggregates.AccountAggregate;
using People.Infrastructure;

namespace People.Api.Application.Queries.GetAccounts;

internal sealed record GetAccountsQuery(int Page, int Limit) : IRequest<PaginatedCollection<AccountModel>>;

internal sealed class GetAccountsQueryHandler : IRequestHandler<GetAccountsQuery, PaginatedCollection<AccountModel>>
{
    private readonly PeopleDbContext _dbContext;

    public GetAccountsQueryHandler(PeopleDbContext dbContext) =>
        _dbContext = dbContext;

    public async Task<PaginatedCollection<AccountModel>> Handle(GetAccountsQuery request, CancellationToken ct)
    {
        var filter = Builders<Account>.Filter.Empty;
        var sort = Builders<Account>.Sort.Ascending(x => x.Id);

        var (items, pages, count) = await _dbContext.Accounts
            .PagingAsync(
                filter,
                sort,
                x => new AccountModel(x.Id, x.Name, x.CountryCode, x.TimeZone, x.Language, x.Picture, x.CreatedAt),
                request.Page,
                request.Limit,
                ct
            );

        return new PaginatedCollection<AccountModel>(items, pages, count);
    }
}

internal sealed record AccountModel(
    AccountId Id,
    Name Name,
    CountryCode CountryCode,
    string TimeZone,
    Language Language,
    Uri Picture,
    DateTime CreatedAt
);
