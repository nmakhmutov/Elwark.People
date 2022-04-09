using System;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using People.Gateway.Infrastructure;

namespace People.Gateway.Features.Timezone;

[ApiController, Route("timezones"), Authorize(Policy = Policy.RequireProfileAccess)]
public class TimeZonesController : ControllerBase
{
    public IActionResult Get() =>
        Ok(TimeZoneInfo.GetSystemTimeZones().Select(x => new TimeZone(x.Id, x.BaseUtcOffset)));
}

internal sealed record TimeZone(string Name, TimeSpan Offset);
