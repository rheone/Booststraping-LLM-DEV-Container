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
&& apt-get clean \
&& rm -rf /var/lib/apt/lists/*

# Create a symbolic link for fd-find to be accessible as 'fd'.
RUN ln -s $(which fdfind) /usr/local/bin/fd

# Install zoxide for enhanced directory navigation.
RUN curl -sSfL https://raw.githubusercontent.com/ajeetdsouza/zoxide/main/install.sh | sh

# Enable Git LFS support.
RUN git lfs install

# Configure PowerShell profile directory.
RUN mkdir -p /home/vscode/.config/powershell

# Ensure vscode user owns their home directory.
RUN chown -R vscode:vscode /home/vscode