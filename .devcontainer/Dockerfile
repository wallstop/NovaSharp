# NovaSharp Dev Container Dockerfile
# Based on the official .NET devcontainer image with additional tooling
# Optimized for Unity compatibility (netstandard2.0/2.1) and VS Code experience

FROM mcr.microsoft.com/devcontainers/dotnet:1-9.0-bookworm

# ============================================================================
# LAYER 1: System packages (consolidated for better caching)
# ============================================================================
RUN apt-get update && apt-get install -y --no-install-recommends \
    # Build essentials for Lua 5.5 compilation
    build-essential \
    libreadline-dev \
    # PowerShell dependencies
    wget \
    apt-transport-https \
    software-properties-common \
    # Development utilities
    ripgrep \
    jq \
    tree \
    htop \
    # Python for tooling
    python3 \
    python3-venv \
    python3-pip \
    # Lua interpreters (5.1-5.4 from apt)
    lua5.1 \
    lua5.2 \
    lua5.3 \
    lua5.4 \
    && apt-get clean \
    && rm -rf /var/lib/apt/lists/*

# ============================================================================
# LAYER 2: PowerShell (separate layer - changes less frequently)
# ============================================================================
RUN wget -q "https://packages.microsoft.com/config/debian/12/packages-microsoft-prod.deb" \
    && dpkg -i packages-microsoft-prod.deb \
    && rm packages-microsoft-prod.deb \
    && apt-get update \
    && apt-get install -y --no-install-recommends powershell \
    && apt-get clean \
    && rm -rf /var/lib/apt/lists/*

# ============================================================================
# LAYER 3: .NET 8.0 SDK (Unity 6+ uses .NET 8, older Unity uses netstandard2.x)
# ============================================================================
RUN wget -q https://dot.net/v1/dotnet-install.sh -O /tmp/dotnet-install.sh \
    && chmod +x /tmp/dotnet-install.sh \
    && /tmp/dotnet-install.sh --channel 8.0 --install-dir /usr/share/dotnet \
    && rm /tmp/dotnet-install.sh

# ============================================================================
# LAYER 4: Lua 5.5 (built from source - changes rarely)
# ============================================================================
# Note: Lua 5.5 is not yet in Debian repos, so we always build from source
RUN curl -fsSL "https://www.lua.org/ftp/lua-5.5.0.tar.gz" | tar xz \
    && cd lua-5.5.0 \
    && make linux \
    && make install INSTALL_TOP=/usr/local \
    && ln -sf /usr/local/bin/lua /usr/local/bin/lua5.5 \
    && cd .. \
    && rm -rf lua-5.5.0

# ============================================================================
# LAYER 5: Global .NET tools (as vscode user for proper permissions)
# ============================================================================
USER vscode

# Install global dotnet tools
RUN dotnet tool install --global csharpier \
    && dotnet tool install --global dotnet-reportgenerator-globaltool

# Pre-warm the NuGet package cache with commonly used packages for faster first build
# These are packages used across the solution - caching them speeds up initial restore
RUN dotnet new classlib -o /tmp/warmup --no-restore \
    && cd /tmp/warmup \
    && dotnet add package TUnit --version 0.* --no-restore || true \
    && dotnet restore --verbosity minimal || true \
    && rm -rf /tmp/warmup

USER root

# ============================================================================
# ENVIRONMENT CONFIGURATION
# ============================================================================

# Ensure dotnet tools are on PATH
ENV PATH="${PATH}:/home/vscode/.dotnet/tools"

# Optimize .NET for container environment
ENV DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_USE_POLLING_FILE_WATCHER=true \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false \
    # Improve NuGet restore performance
    DOTNET_NUGET_SIGNATURE_VERIFICATION=false \
    # Enable roll-forward for SDK version flexibility
    DOTNET_ROLL_FORWARD=Major \
    # Disable telemetry in container
    DOTNET_CLI_TELEMETRY_OPTOUT=true \
    # Roslyn language server memory optimization
    DOTNET_GCHeapHardLimit=0x40000000

# Configure Git defaults (useful for devcontainer users)
RUN git config --system init.defaultBranch main \
    && git config --system core.autocrlf input \
    && git config --system core.eol lf

# ============================================================================
# VERIFICATION
# ============================================================================
RUN echo "=== Environment Verification ===" \
    && echo "--- .NET SDKs ---" && dotnet --list-sdks \
    && echo "--- Lua Versions ---" \
    && lua5.1 -v && lua5.2 -v && lua5.3 -v && lua5.4 -v && lua5.5 -v \
    && echo "--- Global Tools ---" && ls -la /home/vscode/.dotnet/tools/ \
    && echo "=== Verification Complete ==="

