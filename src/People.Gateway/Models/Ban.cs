using System;

namespace People.Gateway.Models
{
    internal sealed record Ban(string Reason, DateTime? ExpiredAt);
}