#!/usr/bin/env bash
set -euo pipefail

DEFAULT_MIGRATION_NAME="Init"
NAME="${1:-$DEFAULT_MIGRATION_NAME}"

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"
PROJECT="$SCRIPT_DIR/People.Infrastructure.csproj"
STARTUP_PROJECT="$REPO_ROOT/src/People.Api/People.Api.csproj"

for CONTEXT in PeopleDbContext WebhookDbContext; do
  if [ "$CONTEXT" = "PeopleDbContext" ]; then
    OUTPUT_DIR="$SCRIPT_DIR/Migrations/People"
  else
    OUTPUT_DIR="$SCRIPT_DIR/Migrations/Webhooks"
  fi

  set +e
  CHECK_OUTPUT="$(
    dotnet ef migrations has-pending-model-changes \
      --context "$CONTEXT" \
      --project "$PROJECT" \
      --startup-project "$STARTUP_PROJECT" 2>&1
  )"
  CHECK_STATUS=$?
  set -e

  if [ "$CHECK_STATUS" -eq 0 ]; then
    echo "No model changes in $CONTEXT, skipping"
  elif printf '%s' "$CHECK_OUTPUT" | grep -F "Changes have been made to the model since the last migration." >/dev/null; then
    echo "Creating migration '$NAME' for $CONTEXT"
    dotnet ef migrations add "$NAME" \
      --context "$CONTEXT" \
      --output-dir "$OUTPUT_DIR" \
      --project "$PROJECT" \
      --startup-project "$STARTUP_PROJECT"
  else
    printf '%s\n' "$CHECK_OUTPUT" >&2
    exit "$CHECK_STATUS"
  fi
done
