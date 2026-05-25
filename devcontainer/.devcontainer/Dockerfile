# Base .NET development image.
FROM mcr.microsoft.com/devcontainers/dotnet:10.0

# Install common developer utilities.
RUN apt-get update && apt-get install -y \
    bat \
    fd-find \
    fzf \
    git-lfs \
    jq \
    less \
    ripgrep \
    tree \
    unzip \
    curl \
    wget \
    vim \
    zip \
#   zsh \
    git \
    unzip \    
&& apt-get clean \
&& rm -rf /var/lib/apt/lists/*

# Create a symbolic link for fd-find to be accessible as 'fd'.
RUN ln -s $(which fdfind) /usr/local/bin/fd

# Enable Git LFS support.
RUN git lfs install

# Configure PowerShell profile directory.
RUN mkdir -p /home/vscode/.config/powershell

# Copy custom PowerShell profile into the container.
COPY .devcontainer/dotfiles/powershell-profile.ps1 /home/vscode/.config/powershell/Microsoft.PowerShell_profile.ps1

# Ensure NuGet config directory exists and is owned by vscode
RUN mkdir -p /home/vscode/.nuget/NuGet \
    && chown -R vscode:vscode /home/vscode/.nuget

# Ensure vscode user owns their home directory.
RUN chown -R vscode:vscode /home/vscode