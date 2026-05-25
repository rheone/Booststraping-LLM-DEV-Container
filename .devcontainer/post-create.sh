#!/usr/bin/env bash

set -e

echo "Running post-create setup..."

# Trust workspace.
git config --global --add safe.directory "$(pwd)"

# Restore all solutions.
find . -name "*.sln" -print0 | while IFS= read -r -d '' sln
do
	echo "Restoring: $sln"
	dotnet restore "$sln"
done

echo "Post-create complete."