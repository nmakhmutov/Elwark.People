using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using People.Gateway.Infrastructure;

namespace People.Gateway.Endpoints.Timezone;

[ApiController, Route("timezones"), Authorize(Policy = Policy.RequireProfileAccess)]
public class TimeZonesController : ControllerBase
{
    private static readonly Lazy<TimeZone[]> Timezone = new(Factory, true);

    private static TimeZone[] Factory() =>
        TimeZoneInfo.GetSystemTimeZones()
            .Where(x => x.HasIanaId)
            .OrderBy(x => x.BaseUtcOffset)
            .ThenBy(x => x.Id)
            .Select(x => new TimeZone(x.Id, x.BaseUtcOffset))
            .ToArray();

    public IActionResult Get() =>
        Ok(Timezone.Value);
    
    internal sealed record TimeZone(string Name, TimeSpan Offset);
}
