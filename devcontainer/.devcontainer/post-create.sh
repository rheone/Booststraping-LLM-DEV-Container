#!/usr/bin/env bash

set -e

echo "Running post-create setup..."

USER_HOME="/home/vscode"

export GIT_CONFIG_GLOBAL=/home/vscode/.config/git/config
mkdir -p "$(dirname $GIT_CONFIG_GLOBAL)"

# Install Oh My Zsh (unattended)
if [ ! -d "$USER_HOME/.oh-my-zsh" ]; then
    sudo -u vscode sh -c \
        "$(curl -fsSL https://raw.githubusercontent.com/ohmyzsh/ohmyzsh/master/tools/install.sh)" "" --unattended
fi

# Set zsh as default shell
sudo chsh -s /usr/bin/zsh vscode

# Configure Oh My Posh
cat <<'EOF' > "$USER_HOME/.oh-my-posh.zsh"
eval "$(oh-my-posh init zsh)"
EOF

# Configure zshrc
if ! grep -q "oh-my-posh.zsh" "$USER_HOME/.zshrc"; then
cat <<'EOF' >> "$USER_HOME/.zshrc"

source ~/.oh-my-posh.zsh

# Aliases
alias ll="ls -alF"
alias gs="git status"

EOF
fi

# Install dotnet global tools
dotnet tool install -g dotnet-ef
dotnet tool install -g dotnet-outdated-tool
dotnet tool install -g dotnet-script
dotnet tool install -g dotnet-trace
dotnet tool install -g dotnet-counters
dotnet tool install -g dotnet-dump

# Restore all solutions.
find . -name "*.sln" -print0 | while IFS= read -r -d '' sln
do
    echo "Restoring: $sln"
    dotnet restore "$sln"
done

# Configure Oh My Posh for PowerShell
PWSH_PROFILE_DIR="/home/vscode/.config/powershell"
PWSH_PROFILE="$PWSH_PROFILE_DIR/Microsoft.PowerShell_profile.ps1"

mkdir -p "$PWSH_PROFILE_DIR"

if ! grep -q "oh-my-posh" "$PWSH_PROFILE" 2>/dev/null; then
    cat <<'EOF' >> "$PWSH_PROFILE"
oh-my-posh init pwsh | Invoke-Expression
EOF
fi

echo "Post-create complete."