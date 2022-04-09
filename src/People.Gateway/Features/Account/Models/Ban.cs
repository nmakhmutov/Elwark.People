using System;

namespace People.Gateway.Features.Account.Models;

internal sealed record Ban(string Reason, DateTime? ExpiredAt);
