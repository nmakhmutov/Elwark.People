using System;
using System.Collections.Generic;
using System.Linq;

namespace People.Domain.Aggregates.AccountAggregate;

public readonly struct TimeFormat : IEquatable<TimeFormat>
{
    public static readonly TimeFormat Default = new("HH:mm");

    public static readonly IReadOnlyCollection<string> List = new[]
    {
        "H:mm",
        "h:mm tt",
        "HH:mm",
        "hh:mm tt"
    };

    private readonly string _value;

    private TimeFormat(string value) =>
        _value = value;

    public static TimeFormat Parse(string? value)
    {
        if (string.IsNullOrEmpty(value))
            throw new ArgumentException("Time format cannot be null or empty.", nameof(value));

        if (!List.Contains(value))
            throw new ArgumentException("Time format have incorrect format.", nameof(value));

        return new TimeFormat(value);
    }

    public static bool TryParse(string? value, out TimeFormat timeFormat)
    {
        timeFormat = Default;

        if (string.IsNullOrEmpty(value))
            return false;

        if (!List.Contains(value))
            return false;

        timeFormat = new TimeFormat(value);
        return true;
    }

    public override string ToString() =>
        _value;

    public bool Equals(TimeFormat other) =>
        _value == other._value;

    public override bool Equals(object? obj) =>
        obj is TimeFormat other && Equals(other);

    public override int GetHashCode() =>
        _value.GetHashCode();

    public static bool operator ==(TimeFormat left, TimeFormat right) =>
        left.Equals(right);

    public static bool operator !=(TimeFormat left, TimeFormat right) =>
        !left.Equals(right);
}
