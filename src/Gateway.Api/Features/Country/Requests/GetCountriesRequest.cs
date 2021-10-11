namespace Gateway.Api.Features.Country.Requests;

public sealed record GetCountriesRequest
{
    public int Page { get; init; } = 1;

    public int Count { get; init; } = 10;

    public string? Code { get; init; }

    public int Limit => Count switch
    {
        0 => 10,
        > 0 and < 100 => Count,
        _ => 100
    };
}
