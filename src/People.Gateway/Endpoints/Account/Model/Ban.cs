using System;

namespace People.Gateway.Endpoints.Account.Model;

internal sealed record Ban(string Reason, DateTime ExpiredAt);
