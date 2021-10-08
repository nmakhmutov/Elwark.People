using System;

namespace Gateway.Api.Features.Management.Models;

internal sealed record Ban(string Reason, DateTime? ExpiredAt);
