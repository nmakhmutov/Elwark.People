using System;

namespace Gateway.Api.Features.AccountManagement.Models;

internal sealed record Ban(string Reason, DateTime? ExpiredAt);
