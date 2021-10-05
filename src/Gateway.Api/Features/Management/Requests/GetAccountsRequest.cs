namespace Gateway.Api.Features.Management.Requests;

public sealed class GetAccountsRequest
{
    public int Limit { get; init; }
    public int Page { get; init; }
}
