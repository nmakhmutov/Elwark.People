using System;

namespace Gateway.Api.Features.Profile;

internal sealed record Ban(string Reason, DateTime? ExpiredAt);
