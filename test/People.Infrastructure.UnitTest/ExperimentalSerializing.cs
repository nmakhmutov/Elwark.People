using System;
using System.Collections.Generic;
using Xunit;
using Xunit.Abstractions;

namespace People.Infrastructure.UnitTest;

public class ExperimentalSerializing
{
    private readonly ITestOutputHelper _output;

    public ExperimentalSerializing(ITestOutputHelper output) =>
        _output = output;

    [Fact]
    public void Timespan_SerializationTest()
    {
        var a = new A(TimeSpan.FromDays(1));

        var newtonsoftJson = Newtonsoft.Json.JsonConvert.SerializeObject(a);
        var systemTextJson = System.Text.Json.JsonSerializer.Serialize(a);

        Assert.Equal(newtonsoftJson, systemTextJson);
    }

    [Fact]
    public void Inheritance_SerializationTest()
    {
        A b = new B(TimeSpan.FromHours(1), new Dictionary<string, int> { ["key"] = 1 });

        var newtonsoftJson = Newtonsoft.Json.JsonConvert.SerializeObject(b);
        var systemTextJson = System.Text.Json.JsonSerializer.Serialize(b);

        Assert.NotEqual(newtonsoftJson, systemTextJson);
    }

    [Fact]
    public void Dictionary_SerializationTest()
    {
        var dictionary = new Dictionary<string, int> { ["key"] = 1, ["Value"] = 42 };

        var newtonsoftJson = Newtonsoft.Json.JsonConvert.SerializeObject(dictionary);
        var systemTextJson = System.Text.Json.JsonSerializer.Serialize(dictionary);

        Assert.Equal(newtonsoftJson, systemTextJson);
    }

    [Fact]
    public void DateOnly_SerializationTest()
    {
        var date = DateOnly.FromDateTime(DateTime.UtcNow);
        
        var newtonsoftJson = Newtonsoft.Json.JsonConvert.SerializeObject(date);
        var systemTextJson = System.Text.Json.JsonSerializer.Serialize(date);

        _output.WriteLine(date.ToString("O") + " - " +newtonsoftJson + " - " + systemTextJson);
        Assert.Equal(newtonsoftJson, systemTextJson);
    }

    [Fact]
    public void TimeOnly_SerializationTest()
    {
        var date = TimeOnly.FromDateTime(DateTime.UtcNow);
        
        var newtonsoftJson = Newtonsoft.Json.JsonConvert.SerializeObject(date);
        var systemTextJson = System.Text.Json.JsonSerializer.Serialize(date);

        _output.WriteLine(newtonsoftJson + " - " + systemTextJson);
        Assert.Equal(newtonsoftJson, systemTextJson);
    }
    
    internal record A(TimeSpan TimeSpan);

    internal record B(TimeSpan TimeSpan, Dictionary<string, int> Dictionary) : A(TimeSpan);
}
