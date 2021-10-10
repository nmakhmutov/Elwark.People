using System;

namespace Gateway.Api.Features.AccountManagement.Requests;

public sealed record BanRequest(string Reason, DateTime? ExpiredAt);
