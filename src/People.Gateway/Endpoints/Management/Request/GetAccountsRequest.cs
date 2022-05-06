namespace People.Gateway.Endpoints.Management.Request;

public sealed record GetAccountsRequest
{
    public int Page { get; init; } = 1;

    public int Count { get; init; } = 10;

    public int Limit => Count switch
    {
        0 => 10,
        > 0 and < 100 => Count,
        _ => 100
    };
}
