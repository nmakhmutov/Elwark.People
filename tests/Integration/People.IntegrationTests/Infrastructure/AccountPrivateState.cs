using System.Reflection;
using People.Domain.Entities;
using People.Domain.ValueObjects;

namespace People.IntegrationTests.Infrastructure;

internal static class AccountPrivateState
{
    private static object? GetField(Account account, string name) =>
        typeof(Account).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(account);

    internal static DateTime LastLogIn(Account account) =>
        (DateTime)GetField(account, "_lastLogIn")!;

    internal static DateTime UpdatedAt(Account account) =>
        (DateTime)GetField(account, "_updatedAt")!;

    internal static DateTime CreatedAt(Account account) =>
        (DateTime)GetField(account, "_createdAt")!;

    internal static string[] Roles(Account account) =>
        (string[])GetField(account, "_roles")!;

    internal static Ban? Ban(Account account) =>
        (Ban?)GetField(account, "_ban");
}
