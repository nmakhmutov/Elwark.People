using System;

namespace Gateway.Api.Features.Account.Models;

internal sealed record Ban(string Reason, DateTime? ExpiredAt);
