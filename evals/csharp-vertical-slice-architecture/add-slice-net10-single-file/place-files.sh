#!/usr/bin/env bash
set -eu
exec dotnet "$(dirname "$0")/../place-files.cs" -- "$(dirname "$0")"
