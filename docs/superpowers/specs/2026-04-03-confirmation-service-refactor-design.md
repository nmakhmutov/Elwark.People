# Confirmation Service Refactor Design

## Goal

Refactor the current confirmation flow so that:

- naming reflects actual behavior
- challenge issuance and token parsing have separate responsibilities
- hot paths allocate less and rely less on string-based branching
- concurrency behavior is more explicit and defensible

This refactor allows interface and signature changes.

## Current Problems

The current `ConfirmationService` mixes several concerns:

- issuing sign-in and sign-up confirmations
- verifying sign-in and sign-up confirmations
- throttling email verification sends
- encrypting and decrypting email verification payloads
- deleting confirmation rows

That leads to overloaded method names such as `SignInAsync` and `SignUpAsync`, where the same method name means both "issue a challenge" and "verify a challenge" depending on parameter shape. It also keeps string literals like `"SignIn"` and `"EmailVerify"` in the hot path.

## Proposed Design

Split the current service into two services with explicit responsibilities.

### `IConfirmationChallengeService`

This service owns confirmation challenge lifecycle for sign-in, sign-up, and email verification.

Public API:

```csharp
public interface IConfirmationChallengeService
{
    Task<ConfirmationChallenge> IssueAsync(AccountId accountId, ConfirmationKind kind, CancellationToken ct = default);
    Task<AccountId> VerifyAsync(string token, string code, ConfirmationKind kind, CancellationToken ct = default);
    Task<int> DeleteByAccountAsync(AccountId accountId, CancellationToken ct = default);
    Task ThrottleEmailVerificationAsync(AccountId accountId, CancellationToken ct = default);
}
```

Behavior:

- `IssueAsync` creates or returns an existing active confirmation challenge for the given account and kind
- `VerifyAsync` parses the token, validates the challenge kind and code, and returns the associated `AccountId`
- `DeleteByAccountAsync` deletes all confirmation rows for the account
- `ThrottleEmailVerificationAsync` keeps the current lock behavior for send throttling

### `IEmailVerificationTokenService`

This service owns encrypted token creation and parsing for email verification only.

Public API:

```csharp
public interface IEmailVerificationTokenService
{
    string CreateToken(Guid confirmationId, MailAddress email);
    EmailVerificationTokenPayload ParseToken(string token);
}
```

Behavior:

- `CreateToken` serializes confirmation id and email into a protected token
- `ParseToken` decrypts and validates the token payload
- this service does not talk to the database

## New Types

### `ConfirmationKind`

Replace string literals with an enum:

```csharp
public enum ConfirmationKind
{
    EmailSignIn = 1,
    EmailSignUp = 2,
    EmailConfirmation = 3,
}
```

Persistence can continue using the existing string column for now, but the infrastructure layer should map enum values to canonical string names in one place. The rest of the code should use the enum only.

### `ConfirmationChallenge`

Represents an issued challenge returned to callers.

```csharp
public sealed record ConfirmationChallenge(
    Guid Id,
    string Token,
    string Code,
    ConfirmationKind Kind);
```

### `EmailVerificationTokenPayload`

Represents the decrypted email token contents.

```csharp
public sealed record EmailVerificationTokenPayload(
    Guid ConfirmationId,
    MailAddress Email);
```

## Naming Changes

Rename existing concepts as follows:

- `ConfirmationService` -> `ConfirmationChallengeService`
- `IConfirmationService` -> `IConfirmationChallengeService`
- `EncodeAsync` -> `IssueChallengeAsync`
- `DecodeAsync` -> `VerifyChallengeAsync`
- `ConventToGuid` -> `ParseChallengeId`
- `VerifyEmailAsync` -> `VerifyEmailChallengeAsync`
- `CreateEmailVerificationToken` -> `CreateToken`
- `DeleteAsync` -> `DeleteByAccountAsync`

At the call sites:

- `SignInAsync(accountId)` becomes `IssueAsync(accountId, ConfirmationKind.EmailSignIn)`
- `SignInAsync(token, code)` becomes `VerifyAsync(token, code, ConfirmationKind.EmailSignIn)`
- `SignUpAsync(accountId)` becomes `IssueAsync(accountId, ConfirmationKind.EmailSignUp)`
- `SignUpAsync(token, code)` becomes `VerifyAsync(token, code, ConfirmationKind.EmailSignUp)`

This removes overload ambiguity and makes intent visible in each caller.

## Performance Improvements

### Remove repeated cache options allocations

The service currently constructs `HybridCacheEntryOptions` on each throttle and issue call. Replace these with static readonly instances:

- one for challenge lock TTL
- one for email verification throttle TTL if it remains the same or a separate one if it diverges later

### Remove string comparisons from the verification path

Use `ConfirmationKind` internally and map it once at the database boundary. This removes repeated ordinal-ignore-case comparisons and lowers typo risk.

### Avoid `Concat(...).ToArray()` for email token creation

When composing the protected payload:

- get the 16 confirmation id bytes
- get the UTF-8 email bytes
- allocate a buffer of exact size
- copy both segments into the buffer directly

This avoids iterator overhead and an extra intermediate array composition step.

### Project only required columns during verification

The current verification path materializes the entire `Confirmation` entity when only the following values are needed:

- `AccountId`
- `Code`
- `Type`

Use an EF projection for the verification query so the database returns only required fields.

### Make issuance safe under concurrency

The current cache-based lock reduces duplicate sends but is not a full correctness guarantee across concurrent callers. Add or verify a unique constraint on `(AccountId, Type)` and handle conflict on insert. This gives a stable database-level guarantee for active challenge uniqueness.

The implementation should:

- attempt to fetch an existing challenge for the account and kind
- if absent, insert a new one
- if a unique conflict occurs, re-read and return the existing row

If the confirmation model is intended to allow historical rows, the unique constraint should be scoped to active rows only. If partial unique indexes are not already modeled in the project, the simpler fallback is to continue deleting consumed rows and enforcing one row per `(AccountId, Type)`.

## Data and Persistence Notes

- The existing `confirmations` table can remain in place.
- The `Type` column may remain a string during this refactor to minimize migration scope.
- Mapping between `ConfirmationKind` and persistence values should live in one internal helper, not in scattered string literals.
- If a uniqueness constraint is added, create or update the EF configuration and migration accordingly.

Canonical persistence values:

- `EmailSignIn`
- `EmailSignUp`
- `EmailConfirmation`

The existing `EmailVerify` string should be migrated to `EmailConfirmation` to align terminology.

## Call Site Updates

Affected consumers will move from flow-specific methods to explicit issue and verify calls.

Examples:

- sign-in command handlers issue and verify `ConfirmationKind.EmailSignIn`
- sign-up command handlers issue and verify `ConfirmationKind.EmailSignUp`
- email confirmation flow uses:
  - `IConfirmationChallengeService.ThrottleEmailVerificationAsync`
  - `IConfirmationChallengeService.IssueAsync(accountId, ConfirmationKind.EmailConfirmation, ct)`
  - `IEmailVerificationTokenService.CreateToken(confirmation.Id, email)`
  - `IEmailVerificationTokenService.ParseToken(token)`
  - `IConfirmationChallengeService.VerifyAsync(confirmationToken, code, ConfirmationKind.EmailConfirmation, ct)`

`VerifyEmailChallengeAsync` may remain as a temporary application-facing convenience method during migration if it reduces churn, but the target design is to keep email token parsing separate from challenge verification.

## Error Handling

Existing confirmation error semantics should remain unchanged:

- malformed token -> `ConfirmationException.Mismatch()`
- missing row -> `ConfirmationException.NotFound()`
- wrong code -> `ConfirmationException.Mismatch()`
- wrong confirmation kind -> `ConfirmationException.Mismatch()`
- reissue during lock window -> `ConfirmationException.AlreadySent()`

The split services should preserve these behaviors so the API surface changes do not alter user-visible failure semantics.

## Testing Strategy

Update tests to match the new boundaries.

### Unit tests

- challenge issuance for each `ConfirmationKind`
- challenge verification for correct and incorrect codes
- malformed token handling
- kind mismatch handling
- deletion by account
- email token round-trip parsing
- email token malformed payload handling
- cache throttling behavior

### Integration tests

- email sign-up issue + verify round trip
- email sign-in issue + verify round trip
- email verification token creation + parse + confirmation verification
- duplicate issuance under lock window returns `AlreadySent`
- persistence uniqueness for `(AccountId, Type)` if a unique constraint is introduced

## Migration Plan

Implementation should proceed in these stages:

1. Introduce `ConfirmationKind`, new records, and mapping helpers.
2. Add `IEmailVerificationTokenService` and move token encryption/parsing into it.
3. Rename and reshape `IConfirmationService` into `IConfirmationChallengeService`.
4. Update infrastructure implementation and registration.
5. Update all command handlers and tests.
6. Add or update persistence constraint and migration if needed.
7. Remove obsolete names and compatibility shims.

## Scope Boundaries

This refactor does not change:

- confirmation code length
- TTL values
- account domain behavior
- notification dispatch behavior
- outward API semantics beyond renamed internal service boundaries

## Decisions

- Split challenge management from email token protection.
- Replace flow-specific overloads with explicit `IssueAsync` and `VerifyAsync` methods.
- Use `ConfirmationKind` instead of raw strings in application and infrastructure logic.
- Keep current error semantics.
- Include the listed allocation and query improvements in the implementation.
