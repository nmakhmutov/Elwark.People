using System;

namespace People.Gateway.Endpoints.Management.Model;

internal sealed record BanModel(string Reason, DateTime? ExpiredAt);
