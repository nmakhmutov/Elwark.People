using System;

namespace People.Gateway.Endpoints.Management.Request;

public sealed record BanRequest(string Reason, DateTime? ExpiredAt);
