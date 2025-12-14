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

# Install Lua 5.1-5.4 and build dependencies for Lua 5.5
RUN apt-get update && apt-get install -y \
    lua5.1 \
    lua5.2 \
    lua5.3 \
    lua5.4 \
    build-essential \
    libreadline-dev \
    curl \
    && apt-get clean \
    && rm -rf /var/lib/apt/lists/*

# Build and install Lua 5.5 from source
ARG LUA55_TARBALL_VERSION="5.5.0-rc2"
ARG LUA55_EXTRACT_DIR="lua-5.5.0"
ARG LUA55_URL="https://www.lua.org/work/lua-${LUA55_TARBALL_VERSION}.tar.gz"
ARG LUA55_INSTALL_PREFIX="/usr/local/lua55"

RUN cd /tmp \
    && curl -L -o "lua-${LUA55_TARBALL_VERSION}.tar.gz" "${LUA55_URL}" \
    && tar xzf "lua-${LUA55_TARBALL_VERSION}.tar.gz" \
    && cd "${LUA55_EXTRACT_DIR}" \
    && make linux \
    && make install INSTALL_TOP="${LUA55_INSTALL_PREFIX}" \
    && ln -sf "${LUA55_INSTALL_PREFIX}/bin/lua" /usr/local/bin/lua5.5 \
    && ln -sf "${LUA55_INSTALL_PREFIX}/bin/luac" /usr/local/bin/luac5.5 \
    && cd / \
    && rm -rf /tmp/lua-*

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
