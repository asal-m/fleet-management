#!/bin/sh
set -eu
cd /app

dotnet ComposeBootstrap.dll
test "$(stat -c %a /run/fleet/secrets.json)" = 600
test "$(stat -c %a /run/fleet/appsettings.local.json)" = 600
cp /run/fleet/secrets.json /run/fleet/original.json
cp /run/fleet/appsettings.local.json /run/fleet/original-config.json

# Retry after an interruption repairs derived files without rotating credentials.
rm /run/fleet/postgres-password /run/fleet/appsettings.local.json
dotnet ComposeBootstrap.dll
cmp -s /run/fleet/original.json /run/fleet/secrets.json
cmp -s /run/fleet/original-config.json /run/fleet/appsettings.local.json
test -s /run/fleet/postgres-password
echo 'PASS: Private files and retry/restart preserve credentials and repair configuration.'

# Existing configuration wins over a changed environment.
FLEET_POSTGRES_USER=ignored dotnet ComposeBootstrap.dll
cmp -s /run/fleet/original.json /run/fleet/secrets.json
echo 'PASS: Existing credentials are not rotated by environment changes.'

# Corrupt state must fail closed, never generate replacement database credentials.
printf 'invalid saved configuration' > /run/fleet/secrets.json
cp /run/fleet/secrets.json /run/fleet/invalid.json
if dotnet ComposeBootstrap.dll >/dev/null 2>&1; then
    echo 'FAIL: Corrupt saved state was accepted.'
    exit 1
fi
cmp -s /run/fleet/invalid.json /run/fleet/secrets.json
echo 'PASS: Corrupt saved state fails without overwriting it.'

# An incomplete import must not mix old database credentials with new ones.
rm /run/fleet/secrets.json
if FLEET_POSTGRES_USER=incomplete dotnet ComposeBootstrap.dll >/dev/null 2>&1; then
    echo 'FAIL: Partial legacy import was accepted.'
    exit 1
fi
test ! -e /run/fleet/secrets.json
echo 'PASS: Partial legacy import fails before creating saved state.'
