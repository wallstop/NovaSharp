# NovaSharp Dev Container Dockerfile
# Based on the official .NET devcontainer image with additional tooling
# Optimized for Unity compatibility (netstandard2.0/2.1) and VS Code experience

FROM mcr.microsoft.com/devcontainers/dotnet:1-9.0-bookworm

# Architecture for downloading binaries (supports amd64 and arm64)
ARG TARGETARCH=amd64

# Set non-interactive frontend for apt to avoid prompts
ENV DEBIAN_FRONTEND=noninteractive

# ============================================================================
# LAYER 1: System packages (consolidated for better caching)
# ============================================================================
# Core utilities:
# - ripgrep: Fast regex search (10x faster than grep)
# - jq: JSON processor
# - htop: Interactive process viewer
# - tree: Directory listing
# - ncdu: Disk usage analyzer with ncurses interface
# - tldr: Simplified man pages with practical examples
# - silversearcher-ag: Fast code search (ag command)
# - moreutils: Additional Unix utilities (sponge, parallel, etc.)
RUN apt-get update && apt-get install -y --no-install-recommends \
    # Build essentials for Lua 5.5 compilation
    build-essential \
    libreadline-dev \
    # PowerShell dependencies
    wget \
    apt-transport-https \
    software-properties-common \
    ca-certificates \
    unzip \
    # Development utilities (modern CLI tools)
    ripgrep \
    jq \
    tree \
    htop \
    ncdu \
    tldr \
    silversearcher-ag \
    moreutils \
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
# LAYER 1.5: Modern CLI tools from GitHub releases
# These are faster, more ergonomic replacements for standard Unix tools
# ============================================================================

# Install fd-find (faster alternative to find) from GitHub releases
# fd respects .gitignore by default and has intuitive syntax
RUN FD_VERSION="10.2.0" \
    && case "${TARGETARCH}" in \
        amd64) FD_ARCH="x86_64-unknown-linux-gnu" ;; \
        arm64) FD_ARCH="aarch64-unknown-linux-gnu" ;; \
        *) echo "Unsupported TARGETARCH: ${TARGETARCH}" >&2; exit 1 ;; \
    esac \
    && curl -fsSL "https://github.com/sharkdp/fd/releases/download/v${FD_VERSION}/fd-v${FD_VERSION}-${FD_ARCH}.tar.gz" \
    | tar -xz --strip-components=1 -C /usr/local/bin "fd-v${FD_VERSION}-${FD_ARCH}/fd" \
    && chmod +x /usr/local/bin/fd

# Install bat (better cat with syntax highlighting) from GitHub releases
RUN BAT_VERSION="0.24.0" \
    && case "${TARGETARCH}" in \
        amd64) BAT_ARCH="x86_64-unknown-linux-gnu" ;; \
        arm64) BAT_ARCH="aarch64-unknown-linux-gnu" ;; \
        *) echo "Unsupported TARGETARCH: ${TARGETARCH}" >&2; exit 1 ;; \
    esac \
    && curl -fsSL "https://github.com/sharkdp/bat/releases/download/v${BAT_VERSION}/bat-v${BAT_VERSION}-${BAT_ARCH}.tar.gz" \
    | tar -xz --strip-components=1 -C /usr/local/bin "bat-v${BAT_VERSION}-${BAT_ARCH}/bat" \
    && chmod +x /usr/local/bin/bat

# Install fzf (fuzzy finder) from GitHub releases
RUN FZF_VERSION="0.56.3" \
    && case "${TARGETARCH}" in \
        amd64) FZF_ARCH="linux_amd64" ;; \
        arm64) FZF_ARCH="linux_arm64" ;; \
        *) echo "Unsupported TARGETARCH: ${TARGETARCH}" >&2; exit 1 ;; \
    esac \
    && curl -fsSL "https://github.com/junegunn/fzf/releases/download/v${FZF_VERSION}/fzf-${FZF_VERSION}-${FZF_ARCH}.tar.gz" \
    | tar -xz -C /usr/local/bin fzf \
    && chmod +x /usr/local/bin/fzf

# Install delta (better git diff viewer) from GitHub releases
RUN DELTA_VERSION="0.18.2" \
    && case "${TARGETARCH}" in \
        amd64) DELTA_ARCH="x86_64-unknown-linux-gnu" ;; \
        arm64) DELTA_ARCH="aarch64-unknown-linux-gnu" ;; \
        *) echo "Unsupported TARGETARCH: ${TARGETARCH}" >&2; exit 1 ;; \
    esac \
    && curl -fsSL "https://github.com/dandavison/delta/releases/download/${DELTA_VERSION}/delta-${DELTA_VERSION}-${DELTA_ARCH}.tar.gz" \
    | tar -xz --strip-components=1 -C /usr/local/bin "delta-${DELTA_VERSION}-${DELTA_ARCH}/delta" \
    && chmod +x /usr/local/bin/delta

# Install eza (modern replacement for ls, successor to exa) from GitHub releases
RUN EZA_VERSION="0.20.14" \
    && case "${TARGETARCH}" in \
        amd64) EZA_ARCH="x86_64-unknown-linux-gnu" ;; \
        arm64) EZA_ARCH="aarch64-unknown-linux-gnu" ;; \
        *) echo "Unsupported TARGETARCH: ${TARGETARCH}" >&2; exit 1 ;; \
    esac \
    && curl -fsSL "https://github.com/eza-community/eza/releases/download/v${EZA_VERSION}/eza_${EZA_ARCH}.tar.gz" \
    | tar -xz -C /usr/local/bin \
    && chmod +x /usr/local/bin/eza

# Install zoxide (smarter cd command) from GitHub releases
RUN ZOXIDE_VERSION="0.9.6" \
    && case "${TARGETARCH}" in \
        amd64) ZOXIDE_ARCH="x86_64-unknown-linux-musl" ;; \
        arm64) ZOXIDE_ARCH="aarch64-unknown-linux-musl" ;; \
        *) echo "Unsupported TARGETARCH: ${TARGETARCH}" >&2; exit 1 ;; \
    esac \
    && curl -fsSL "https://github.com/ajeetdsouza/zoxide/releases/download/v${ZOXIDE_VERSION}/zoxide-${ZOXIDE_VERSION}-${ZOXIDE_ARCH}.tar.gz" \
    | tar -xz -C /usr/local/bin zoxide \
    && chmod +x /usr/local/bin/zoxide

# Install yq (YAML processor, like jq for YAML) from GitHub releases
RUN YQ_VERSION="4.44.6" \
    && case "${TARGETARCH}" in \
        amd64) YQ_ARCH="linux_amd64" ;; \
        arm64) YQ_ARCH="linux_arm64" ;; \
        *) echo "Unsupported TARGETARCH: ${TARGETARCH}" >&2; exit 1 ;; \
    esac \
    && curl -fsSL "https://github.com/mikefarah/yq/releases/download/v${YQ_VERSION}/yq_${YQ_ARCH}" -o /usr/local/bin/yq \
    && chmod +x /usr/local/bin/yq

# Install duf (better df) from GitHub releases
RUN DUF_VERSION="0.8.1" \
    && case "${TARGETARCH}" in \
        amd64) DUF_ARCH="linux_x86_64" ;; \
        arm64) DUF_ARCH="linux_arm64" ;; \
        *) echo "Unsupported TARGETARCH: ${TARGETARCH}" >&2; exit 1 ;; \
    esac \
    && curl -fsSL "https://github.com/muesli/duf/releases/download/v${DUF_VERSION}/duf_${DUF_VERSION}_${DUF_ARCH}.tar.gz" \
    | tar -xz -C /usr/local/bin duf \
    && chmod +x /usr/local/bin/duf

# Install sd (intuitive find-and-replace, better sed) from GitHub releases
RUN SD_VERSION="1.0.0" \
    && case "${TARGETARCH}" in \
        amd64) SD_ARCH="x86_64-unknown-linux-gnu" ;; \
        arm64) SD_ARCH="aarch64-unknown-linux-gnu" ;; \
        *) echo "Unsupported TARGETARCH: ${TARGETARCH}" >&2; exit 1 ;; \
    esac \
    && curl -fsSL "https://github.com/chmln/sd/releases/download/v${SD_VERSION}/sd-v${SD_VERSION}-${SD_ARCH}.tar.gz" \
    | tar -xz --strip-components=1 -C /usr/local/bin "sd-v${SD_VERSION}-${SD_ARCH}/sd" \
    && chmod +x /usr/local/bin/sd

# Install dust (intuitive disk usage, better du) from GitHub releases
RUN DUST_VERSION="1.1.1" \
    && case "${TARGETARCH}" in \
        amd64) DUST_ARCH="x86_64-unknown-linux-gnu" ;; \
        arm64) DUST_ARCH="aarch64-unknown-linux-gnu" ;; \
        *) echo "Unsupported TARGETARCH: ${TARGETARCH}" >&2; exit 1 ;; \
    esac \
    && curl -fsSL "https://github.com/bootandy/dust/releases/download/v${DUST_VERSION}/dust-v${DUST_VERSION}-${DUST_ARCH}.tar.gz" \
    | tar -xz --strip-components=1 -C /usr/local/bin "dust-v${DUST_VERSION}-${DUST_ARCH}/dust" \
    && chmod +x /usr/local/bin/dust

# Install procs (modern replacement for ps) from GitHub releases
RUN PROCS_VERSION="0.14.8" \
    && case "${TARGETARCH}" in \
        amd64) PROCS_ARCH="x86_64-linux" ;; \
        arm64) PROCS_ARCH="aarch64-linux" ;; \
        *) echo "Unsupported TARGETARCH: ${TARGETARCH}" >&2; exit 1 ;; \
    esac \
    && curl -fsSL "https://github.com/dalance/procs/releases/download/v${PROCS_VERSION}/procs-v${PROCS_VERSION}-${PROCS_ARCH}.zip" -o /tmp/procs.zip \
    && unzip -q /tmp/procs.zip -d /usr/local/bin \
    && chmod +x /usr/local/bin/procs \
    && rm /tmp/procs.zip

# Install tokei (code statistics) from GitHub releases
RUN TOKEI_VERSION="12.1.2" \
    && case "${TARGETARCH}" in \
        amd64) TOKEI_ARCH="x86_64-unknown-linux-gnu" ;; \
        arm64) TOKEI_ARCH="aarch64-unknown-linux-gnu" ;; \
        *) echo "Unsupported TARGETARCH: ${TARGETARCH}" >&2; exit 1 ;; \
    esac \
    && curl -fsSL "https://github.com/XAMPPRocky/tokei/releases/download/v${TOKEI_VERSION}/tokei-${TOKEI_ARCH}.tar.gz" \
    | tar -xz -C /usr/local/bin tokei \
    && chmod +x /usr/local/bin/tokei

# Install lychee (fast link checker) from GitHub releases
RUN LYCHEE_VERSION="0.21.0" \
    && case "${TARGETARCH}" in \
        amd64) LYCHEE_ARCH="x86_64-unknown-linux-gnu" ;; \
        arm64) LYCHEE_ARCH="aarch64-unknown-linux-gnu" ;; \
        *) echo "Unsupported TARGETARCH: ${TARGETARCH}" >&2; exit 1 ;; \
    esac \
    && curl -fsSL "https://github.com/lycheeverse/lychee/releases/download/lychee-v${LYCHEE_VERSION}/lychee-${LYCHEE_ARCH}.tar.gz" \
    | tar -xz -C /usr/local/bin \
    && chmod +x /usr/local/bin/lychee

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
    DOTNET_NOLOGO=true \
    # Build server / Roslyn VBCSCompiler optimizations
    # Keep build server alive for 2 hours (default 20 min)
    DOTNET_BUILD_SERVER_SHUTDOWN_TIMEOUT=7200 \
    # Roslyn compiler server keep-alive (2 hours in seconds)
    ROSLYNCOMMANDLINELOGFILE="" \
    # MSBuild node reuse - keeps worker processes alive between builds
    MSBUILDDISABLENODEREUSE=0 \
    # Ensure MSBuild uses out-of-proc nodes for parallelism
    MSBuildEnableWorkloadResolver=true

# Configure Git defaults (useful for devcontainer users)
RUN git config --system init.defaultBranch main \
    && git config --system core.autocrlf input \
    && git config --system core.eol lf

# Configure delta as the default git pager for better diffs
RUN git config --system core.pager delta \
    && git config --system interactive.diffFilter "delta --color-only" \
    && git config --system delta.navigate true \
    && git config --system delta.light false \
    && git config --system delta.line-numbers true \
    && git config --system delta.side-by-side false \
    && git config --system merge.conflictstyle diff3 \
    && git config --system diff.colorMoved default

# Initialize zoxide for bash (add to bashrc for interactive shells)
RUN echo 'eval "$(zoxide init bash)"' >> /etc/bash.bashrc

# Add helpful aliases for modern CLI tools to bashrc
RUN { \
    echo ''; \
    echo '# Modern CLI tool aliases (from unity-helpers devcontainer)'; \
    echo 'alias ls="eza --icons --group-directories-first"'; \
    echo 'alias ll="eza -la --icons --group-directories-first"'; \
    echo 'alias lt="eza --tree --icons --group-directories-first"'; \
    echo 'alias cat="bat --paging=never"'; \
    echo 'alias less="bat"'; \
    echo 'alias find="fd"'; \
    echo 'alias grep="rg"'; \
    echo 'alias df="duf"'; \
    echo 'alias sed="sd"'; \
    echo 'alias du="dust"'; \
    echo 'alias ps="procs"'; \
    echo 'alias cd="z"'; \
    echo 'alias cdi="zi"'; \
    } >> /etc/bash.bashrc

# ============================================================================
# VS Code Shell Integration
# ============================================================================
# Enable VS Code terminal shell integration for better terminal experience
# This prevents the "enable shell integration?" prompt and enables features like:
# - Command decorations (success/failure indicators)
# - Command navigation (Ctrl+Up/Down to jump between commands)
# - Run recent command (Ctrl+Alt+R)
# See: https://code.visualstudio.com/docs/terminal/shell-integration
RUN echo '' >> /etc/bash.bashrc \
    && echo '# VS Code Shell Integration' >> /etc/bash.bashrc \
    && echo 'if [[ "$TERM_PROGRAM" == "vscode" ]]; then' >> /etc/bash.bashrc \
    && echo '  . "$(code --locate-shell-integration-path bash 2>/dev/null)" 2>/dev/null || true' >> /etc/bash.bashrc \
    && echo 'fi' >> /etc/bash.bashrc

# ============================================================================
# VERIFICATION
# ============================================================================
RUN echo "=== Environment Verification ===" \
    && echo "--- .NET SDKs ---" && dotnet --list-sdks \
    && echo "--- Lua Versions ---" \
    && lua5.1 -v && lua5.2 -v && lua5.3 -v && lua5.4 -v && lua5.5 -v \
    && echo "--- Global Tools ---" && ls -la /home/vscode/.dotnet/tools/ \
    && echo "--- Modern CLI Tools ---" \
    && fd --version && bat --version && fzf --version \
    && delta --version && eza --version && zoxide --version \
    && yq --version && duf --version && sd --version \
    && dust --version && procs --version && tokei --version \
    && lychee --version \
    && echo "=== Verification Complete ==="

# Ensure dotnet tools are on the PATH for both root and vscode users
ENV PATH="${PATH}:/root/.dotnet/tools:/home/vscode/.dotnet/tools"

# Reset to non-root user
USER vscode

