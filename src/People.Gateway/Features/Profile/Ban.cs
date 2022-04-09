using System;

namespace People.Gateway.Features.Profile;

internal sealed record Ban(string Reason, DateTime ExpiredAt);
