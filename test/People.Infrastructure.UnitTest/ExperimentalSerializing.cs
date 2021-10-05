using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Xunit;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace People.Infrastructure.UnitTest;

public class ExperimentalSerializing
{
    internal record A(TimeSpan TimeSpan);

    internal record B(TimeSpan TimeSpan, Dictionary<string, int> Dictionary) : A(TimeSpan);

    [Fact]
    public void DateOnly_SerializationTest()
    {
        var date = DateOnly.FromDateTime(DateTime.UtcNow);

        Assert.Throws<NotSupportedException>(() => JsonSerializer.Serialize(date));
        // var systemTextJson = System.Text.Json.JsonSerializer.Serialize(date);

        // _output.WriteLine(date.ToString("O") + " - " + systemTextJson);
        // Assert.NotEqual(date.ToString("O"), systemTextJson);
    }

    [Fact]
    public void Dictionary_SerializationTest()
    {
        var dictionary = new Dictionary<string, int> { ["key"] = 1, ["Value"] = 42 };

        var newtonsoftJson = JsonConvert.SerializeObject(dictionary);
        var systemTextJson = JsonSerializer.Serialize(dictionary);

        Assert.Equal(newtonsoftJson, systemTextJson);
    }

    [Fact]
    public void Inheritance_SerializationTest()
    {
        A b = new B(TimeSpan.FromHours(1), new Dictionary<string, int> { ["key"] = 1 });

        var newtonsoftJson = JsonConvert.SerializeObject(b);
        var systemTextJson = JsonSerializer.Serialize(b);

        Assert.NotEqual(newtonsoftJson, systemTextJson);
    }

    [Fact]
    public void TimeOnly_SerializationTest()
    {
        var time = TimeOnly.FromDateTime(DateTime.UtcNow);

        Assert.Throws<NotSupportedException>(() => JsonSerializer.Serialize(time));
        // var systemTextJson = System.Text.Json.JsonSerializer.Serialize(time);

        // _output.WriteLine(time.ToString("O") + " - " + systemTextJson);
        // Assert.NotEqual(time.ToString("O"), systemTextJson);
    }
    // private readonly ITestOutputHelper _output;

    // public ExperimentalSerializing(ITestOutputHelper output) =>
    // _output = output;

    [Fact]
    public void Timespan_SerializationTest()
    {
        var a = new A(TimeSpan.FromDays(1));

        var newtonsoftJson = JsonConvert.SerializeObject(a);
        var systemTextJson = JsonSerializer.Serialize(a);

        Assert.Equal(newtonsoftJson, systemTextJson);
    }
}
