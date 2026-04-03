#!/usr/bin/env bash
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
SCRIPT_PATH="$REPO_ROOT/src/People.Infrastructure/add_migration.sh"

TMP_DIR="$(mktemp -d)"
cleanup() {
  rm -rf "$TMP_DIR"
}
trap cleanup EXIT

FAKE_BIN="$TMP_DIR/bin"
mkdir -p "$FAKE_BIN"
DOTNET_LOG="$TMP_DIR/dotnet.log"

cat > "$FAKE_BIN/dotnet" <<EOF
#!/usr/bin/env bash
set -euo pipefail
printf '%s\n' "\$*" >> "$DOTNET_LOG"

if [ "\$1" = "ef" ] && [ "\$2" = "migrations" ] && [ "\$3" = "has-pending-model-changes" ]; then
  exit 0
fi

exit 0
EOF
chmod +x "$FAKE_BIN/dotnet"

OUTPUT_FILE="$TMP_DIR/output.log"
if ! PATH="$FAKE_BIN:$PATH" bash "$SCRIPT_PATH" InitTest >"$OUTPUT_FILE" 2>&1; then
  echo "Script failed unexpectedly"
  cat "$OUTPUT_FILE"
  exit 1
fi

grep -F "Creating migration 'InitTest' for PeopleDbContext" "$OUTPUT_FILE" >/dev/null
grep -F "Creating migration 'InitTest' for WebhookDbContext" "$OUTPUT_FILE" >/dev/null

grep -F "ef migrations has-pending-model-changes --context PeopleDbContext --project $REPO_ROOT/src/People.Infrastructure/People.Infrastructure.csproj --startup-project $REPO_ROOT/src/People.Api/People.Api.csproj --no-build -q" "$DOTNET_LOG" >/dev/null
grep -F "ef migrations add InitTest --context PeopleDbContext --output-dir $REPO_ROOT/src/People.Infrastructure/Migrations/People --project $REPO_ROOT/src/People.Infrastructure/People.Infrastructure.csproj --startup-project $REPO_ROOT/src/People.Api/People.Api.csproj" "$DOTNET_LOG" >/dev/null
grep -F "ef migrations has-pending-model-changes --context WebhookDbContext --project $REPO_ROOT/src/People.Infrastructure/People.Infrastructure.csproj --startup-project $REPO_ROOT/src/People.Api/People.Api.csproj --no-build -q" "$DOTNET_LOG" >/dev/null
grep -F "ef migrations add InitTest --context WebhookDbContext --output-dir $REPO_ROOT/src/People.Infrastructure/Migrations/Webhooks --project $REPO_ROOT/src/People.Infrastructure/People.Infrastructure.csproj --startup-project $REPO_ROOT/src/People.Api/People.Api.csproj" "$DOTNET_LOG" >/dev/null
