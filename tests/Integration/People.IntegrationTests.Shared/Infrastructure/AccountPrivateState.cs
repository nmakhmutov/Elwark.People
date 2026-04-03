using System.Reflection;
using People.Domain.Entities;
using People.Domain.ValueObjects;

namespace People.IntegrationTests.Shared.Infrastructure;

public static class AccountPrivateState
{
    private static object? GetField(Account account, string name) =>
        typeof(Account).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(account);

    public static DateTime LastLogIn(Account account) =>
        (DateTime)GetField(account, "_lastLogIn")!;

    public static DateTime UpdatedAt(Account account) =>
        (DateTime)GetField(account, "_updatedAt")!;

    public static DateTime CreatedAt(Account account) =>
        (DateTime)GetField(account, "_createdAt")!;

    public static string[] Roles(Account account) =>
        (string[])GetField(account, "_roles")!;

    public static Ban? Ban(Account account) =>
        (Ban?)GetField(account, "_ban");
}
