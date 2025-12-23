# NovaSharp Dev Container Dockerfile
# Based on the official .NET devcontainer image with additional tooling

FROM mcr.microsoft.com/devcontainers/dotnet:1-9.0-bookworm

# Install .NET 8.0 SDK (in addition to 9.0 from base image)
RUN wget https://dot.net/v1/dotnet-install.sh -O /tmp/dotnet-install.sh \
    && chmod +x /tmp/dotnet-install.sh \
    && /tmp/dotnet-install.sh --channel 8.0 --install-dir /usr/share/dotnet \
    && rm /tmp/dotnet-install.sh

# Install PowerShell
RUN apt-get update && apt-get install -y wget apt-transport-https software-properties-common \
    && wget -q "https://packages.microsoft.com/config/debian/12/packages-microsoft-prod.deb" \
    && dpkg -i packages-microsoft-prod.deb \
    && rm packages-microsoft-prod.deb \
    && apt-get update \
    && apt-get install -y powershell \
    && apt-get clean \
    && rm -rf /var/lib/apt/lists/*

# Install Python 3.11 (default in Debian Bookworm) and venv support
RUN apt-get update && apt-get install -y \
    python3 \
    python3-venv \
    python3-pip \
    && apt-get clean \
    && rm -rf /var/lib/apt/lists/*

# Install ripgrep
RUN apt-get update && apt-get install -y ripgrep \
    && apt-get clean \
    && rm -rf /var/lib/apt/lists/*

# Install Lua 5.1-5.4 from apt (these are available in Debian repos)
RUN apt-get update && apt-get install -y \
    lua5.1 \
    lua5.2 \
    lua5.3 \
    lua5.4 \
    && apt-get clean \
    && rm -rf /var/lib/apt/lists/*

# Install Lua 5.5 - try apt first, fall back to building from source
# Lua 5.5 is not yet in apt repositories as of December 2025
RUN apt-get update \
    && if apt-cache show lua5.5 >/dev/null 2>&1; then \
        echo "Installing lua5.5 from apt..." \
        && apt-get install -y lua5.5; \
    else \
        echo "lua5.5 not in apt, building from source..." \
        && apt-get install -y build-essential libreadline-dev \
        && curl -fsSL "https://www.lua.org/ftp/lua-5.5.0.tar.gz" | tar xz \
        && cd lua-5.5.0 \
        && make linux \
        && make install INSTALL_TOP=/usr/local \
        && ln -sf /usr/local/bin/lua /usr/local/bin/lua5.5 \
        && cd .. \
        && rm -rf lua-5.5.0; \
    fi \
    && apt-get clean \
    && rm -rf /var/lib/apt/lists/*

# Install CSharpier globally
RUN dotnet tool install --global csharpier

# Ensure dotnet tools are on PATH for all users
ENV PATH="${PATH}:/root/.dotnet/tools:/home/vscode/.dotnet/tools"

# Verify Lua installations
RUN echo "=== Lua Version Verification ===" \
    && lua5.1 -v \
    && lua5.2 -v \
    && lua5.3 -v \
    && lua5.4 -v \
    && lua5.5 -v
