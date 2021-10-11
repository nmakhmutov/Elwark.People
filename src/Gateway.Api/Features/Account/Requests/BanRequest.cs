using System;

namespace Gateway.Api.Features.Account.Requests;

public sealed record BanRequest(string Reason, DateTime? ExpiredAt);
