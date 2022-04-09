using System;

namespace People.Gateway.Features.Account.Requests;

public sealed record BanRequest(string Reason, DateTime? ExpiredAt);
