# NovaSharp Dev Container Dockerfile
# Based on the official .NET devcontainer image with additional tooling.
# Prioritizes deterministic cold-cache reliability and cross-platform behavior.

FROM mcr.microsoft.com/devcontainers/dotnet@sha256:7572080d1ce09283b8bc2504e3e88cab79dede5eb7e66973d544eea86c3fa216

# Use bash with pipefail so failed downloads in pipelines are surfaced.
SHELL ["/bin/bash", "-o", "pipefail", "-c"]

# Architecture for downloading binaries (supports amd64 and arm64).
ARG TARGETARCH=amd64
# Pinned Codex CLI version for reproducible builds.
ARG CODEX_CLI_VERSION=0.142.3
# Pinned checksum for Microsoft package repository bootstrap package.
ARG MS_PACKAGES_DEB_SHA256=8434dcb8c346dc95fbd63dbece056c343704590b58b6a5c323d39acf52bf0b48
# Pinned Lua source version and checksum.
ARG LUA55_VERSION=5.5.0
ARG LUA55_SHA256=57ccc32bbbd005cab75bcc52444052535af691789dba2b9016d5c50640d68b3d
ARG ACTIONLINT_VERSION=1.7.12
ARG ACTIONLINT_LINUX_AMD64_SHA256=8aca8db96f1b94770f1b0d72b6dddcb1ebb8123cb3712530b08cc387b349a3d8
ARG ACTIONLINT_LINUX_ARM64_SHA256=325e971b6ba9bfa504672e29be93c24981eeb1c07576d730e9f7c8805afff0c6

ENV DEBIAN_FRONTEND=noninteractive

# =============================================================================
# LAYER 1: Core system packages and language runtimes from signed apt repos
# =============================================================================
RUN set -eux; \
    # Base image may include a stale Yarn repo key; remove Yarn apt source since Yarn is not required.
    rm -f /etc/apt/sources.list.d/yarn.list /etc/apt/sources.list.d/yarn.list.save; \
    rm -f /etc/apt/trusted.gpg.d/yarn*.gpg /usr/share/keyrings/yarnkey.gpg; \
    printf 'Acquire::Retries "5";\n' > /etc/apt/apt.conf.d/99novasharp-retries; \
    printf 'Acquire::http::Timeout "30";\n' >> /etc/apt/apt.conf.d/99novasharp-retries; \
    printf 'Acquire::https::Timeout "30";\n' >> /etc/apt/apt.conf.d/99novasharp-retries; \
    apt-get update; \
    apt-get install -y --no-install-recommends \
        # Build essentials for Lua compilation
        build-essential \
        libreadline-dev \
        # Download/runtime dependencies
        wget \
        curl \
        ca-certificates \
        unzip \
        apt-transport-https \
        software-properties-common \
        gnupg \
        # Development utilities
        ripgrep \
        jq \
        tree \
        htop \
        ncdu \
        tldr \
        silversearcher-ag \
        moreutils \
        fd-find \
        bat \
        fzf \
        zoxide \
        # Node.js + npm for Codex CLI
        nodejs \
        npm \
        # Python for tooling
        python3 \
        python3-venv \
        python3-pip \
        # YAML and GitHub Actions linting
        yamllint \
        # Lua interpreters (5.1-5.4 from apt)
        lua5.1 \
        lua5.2 \
        lua5.3 \
        lua5.4; \
    # Debian package names differ from expected command names.
    ln -sf /usr/bin/fdfind /usr/local/bin/fd; \
    ln -sf /usr/bin/batcat /usr/local/bin/bat; \
    apt-get clean; \
    rm -rf /var/lib/apt/lists/*

# =============================================================================
# LAYER 2: Add Microsoft signed apt feed and install PowerShell + .NET 8 SDK
# =============================================================================
RUN set -eux; \
    wget -q --tries=5 --waitretry=2 --timeout=30 "https://packages.microsoft.com/config/debian/12/packages-microsoft-prod.deb" -O /tmp/packages-microsoft-prod.deb; \
    echo "${MS_PACKAGES_DEB_SHA256}  /tmp/packages-microsoft-prod.deb" | sha256sum -c -; \
    dpkg -i /tmp/packages-microsoft-prod.deb; \
    rm /tmp/packages-microsoft-prod.deb; \
    apt-get update; \
    apt-get install -y --no-install-recommends powershell dotnet-sdk-8.0; \
    apt-get clean; \
    rm -rf /var/lib/apt/lists/*

# =============================================================================
# LAYER 3: Lua 5.5 from source (checksum-verified)
# =============================================================================
RUN set -eux; \
    wget -q --tries=5 --waitretry=2 --timeout=30 "https://www.lua.org/ftp/lua-${LUA55_VERSION}.tar.gz" -O "/tmp/lua-${LUA55_VERSION}.tar.gz"; \
    echo "${LUA55_SHA256}  /tmp/lua-${LUA55_VERSION}.tar.gz" | sha256sum -c -; \
    tar -xzf "/tmp/lua-${LUA55_VERSION}.tar.gz" -C /tmp; \
    cd "/tmp/lua-${LUA55_VERSION}"; \
    make linux; \
    make install INSTALL_TOP=/usr/local; \
    ln -sf /usr/local/bin/lua /usr/local/bin/lua5.5; \
    rm -rf "/tmp/lua-${LUA55_VERSION}" "/tmp/lua-${LUA55_VERSION}.tar.gz"

# =============================================================================
# LAYER 4: actionlint for GitHub Actions workflow validation
# =============================================================================
RUN set -eux; \
    case "${TARGETARCH}" in \
        amd64) actionlint_arch="amd64"; actionlint_sha="${ACTIONLINT_LINUX_AMD64_SHA256}" ;; \
        arm64) actionlint_arch="arm64"; actionlint_sha="${ACTIONLINT_LINUX_ARM64_SHA256}" ;; \
        *) echo "Unsupported TARGETARCH for actionlint: ${TARGETARCH}" >&2; exit 1 ;; \
    esac; \
    actionlint_archive="actionlint_${ACTIONLINT_VERSION}_linux_${actionlint_arch}.tar.gz"; \
    wget -q --tries=5 --waitretry=2 --timeout=30 "https://github.com/rhysd/actionlint/releases/download/v${ACTIONLINT_VERSION}/${actionlint_archive}" -O "/tmp/${actionlint_archive}"; \
    echo "${actionlint_sha}  /tmp/${actionlint_archive}" | sha256sum -c -; \
    tar -xzf "/tmp/${actionlint_archive}" -C /tmp actionlint; \
    install -m 0755 /tmp/actionlint /usr/local/bin/actionlint; \
    rm -f "/tmp/${actionlint_archive}" /tmp/actionlint

# =============================================================================
# LAYER 5: Global .NET tools (as vscode user for proper permissions)
# =============================================================================
USER vscode

RUN set -eux; \
    retry() { \
        local attempts=0; \
        local max=5; \
        until "$@"; do \
            attempts=$((attempts + 1)); \
            if [ "$attempts" -ge "$max" ]; then \
                return 1; \
            fi; \
            sleep $((attempts * 3)); \
        done; \
    }; \
    retry dotnet tool install --global csharpier || retry dotnet tool update --global csharpier; \
    retry dotnet tool install --global dotnet-reportgenerator-globaltool \
    || retry dotnet tool update --global dotnet-reportgenerator-globaltool

USER root

# Install OpenAI Codex CLI from npm with conservative retry settings.
RUN set -eux; \
    npm config set fetch-retries 5; \
    npm config set fetch-retry-mintimeout 20000; \
    npm config set fetch-retry-maxtimeout 120000; \
    npm config set fetch-timeout 300000; \
    for attempt in 1 2 3 4 5; do \
        npm install --global "@openai/codex@${CODEX_CLI_VERSION}" && break; \
        if [ "$attempt" -eq 5 ]; then exit 1; fi; \
        sleep $((attempt * 4)); \
    done

# =============================================================================
# ENVIRONMENT CONFIGURATION
# =============================================================================

# Ensure dotnet tools are on PATH for both root and vscode users.
ENV PATH="${PATH}:/root/.dotnet/tools:/home/vscode/.dotnet/tools"

# Optimize .NET for container environment.
ENV DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_USE_POLLING_FILE_WATCHER=true \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false \
    DOTNET_ROLL_FORWARD=Major \
    DOTNET_CLI_TELEMETRY_OPTOUT=true \
    DOTNET_NOLOGO=true \
    DOTNET_BUILD_SERVER_SHUTDOWN_TIMEOUT=7200 \
    ROSLYNCOMMANDLINELOGFILE="" \
    MSBUILDDISABLENODEREUSE=0 \
    MSBuildEnableWorkloadResolver=true

# Configure Git defaults (useful for devcontainer users).
RUN git config --system init.defaultBranch main \
    && git config --system core.autocrlf input \
    && git config --system core.eol lf

# Initialize zoxide for bash (add to bashrc for interactive shells).
RUN echo 'eval "$(zoxide init bash)"' >> /etc/bash.bashrc

# Add helpful aliases for installed CLI tools.
RUN { \
    echo ''; \
    echo '# NovaSharp CLI aliases'; \
    echo 'if command -v fd >/dev/null 2>&1; then alias find="fd"; fi'; \
    echo 'if command -v bat >/dev/null 2>&1; then alias cat="bat --paging=never"; fi'; \
    echo 'if command -v rg >/dev/null 2>&1; then alias grep="rg"; fi'; \
    echo 'if command -v zoxide >/dev/null 2>&1; then alias cd="z"; alias cdi="zi"; fi'; \
    echo 'alias ll="ls -lah --color=auto"'; \
    echo 'alias lt="tree -a -I .git"'; \
    } >> /etc/bash.bashrc

# Enable VS Code terminal shell integration.
RUN echo '' >> /etc/bash.bashrc \
    && echo '# VS Code Shell Integration' >> /etc/bash.bashrc \
    && echo 'if [[ "$TERM_PROGRAM" == "vscode" ]]; then' >> /etc/bash.bashrc \
    && echo '  . "$(code --locate-shell-integration-path bash 2>/dev/null)" 2>/dev/null || true' >> /etc/bash.bashrc \
    && echo 'fi' >> /etc/bash.bashrc

# =============================================================================
# VERIFICATION
# =============================================================================
RUN echo "=== Environment Verification ===" \
    && echo "--- .NET SDKs ---" && dotnet --list-sdks \
    && echo "--- Node.js Tooling ---" && node --version && npm --version && codex --version \
    && echo "--- PowerShell ---" && pwsh --version \
    && echo "--- Lua Versions ---" \
    && lua5.1 -v && lua5.2 -v && lua5.3 -v && lua5.4 -v && lua5.5 -v \
    && echo "--- Global Tools ---" && ls -la /home/vscode/.dotnet/tools/ \
    && echo "--- Core CLI Tools ---" \
    && fd --version && bat --version && fzf --version && zoxide --version \
    && rg --version && jq --version && tree --version \
    && echo "--- Workflow/YAML Lint Tools ---" \
    && actionlint -version && yamllint --version \
    && echo "=== Verification Complete ==="

USER vscode
