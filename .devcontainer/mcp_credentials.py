"""Stage and read the project-specific Z.ai credential used by Claude MCP."""

import hashlib
import os
from pathlib import Path
import stat
import sys
import tempfile


def validate_key(key):
    if not key or any(byte in b" \t\r\n\v\f\"'\0" for byte in key):
        raise ValueError("Filtered MCP credentials contain an invalid ZAI_API_KEY.")
    return key


def private_file(workspace, *, create):
    project = workspace.resolve(strict=True)
    if not project.is_dir():
        raise ValueError("The Claude workspace must be a directory.")
    home = Path.home()
    home_info = home.stat()
    if home_info.st_uid != os.geteuid() or home_info.st_mode & 0o022:
        raise ValueError("The runtime user's home directory must be owned and writable only by its user.")
    directory = home / ".novasharp-mcp"
    if create:
        directory.mkdir(mode=0o700, exist_ok=True)
    directory_info = directory.lstat()
    if (
        not stat.S_ISDIR(directory_info.st_mode)
        or directory_info.st_uid != os.geteuid()
        or stat.S_IMODE(directory_info.st_mode) != 0o700
    ):
        raise ValueError("The private MCP directory must be user-owned with mode 700.")
    return directory / hashlib.sha256(os.fsencode(project)).hexdigest()


def stage(workspace):
    destination = private_file(workspace, create=True)
    key = os.environb.get(b"ZAI_API_KEY", b"")
    if key:
        validate_key(key)
    # Publish an empty file on removal as well. Readers see a complete snapshot.
    staging = tempfile.NamedTemporaryFile(dir=destination.parent, delete=False)
    try:
        with staging:
            os.fchmod(staging.fileno(), 0o600)
            if key:
                staging.write(b"ZAI_API_KEY=" + key + b"\n")
        os.replace(staging.name, destination)
    finally:
        Path(staging.name).unlink(missing_ok=True)


def read_key(path, *, standalone):
    # Check the open file itself, including refusals for symlinks and pipes.
    fd = os.open(path, os.O_RDONLY | os.O_NOFOLLOW | os.O_NONBLOCK)
    with os.fdopen(fd, "rb") as file:
        info = os.fstat(file.fileno())
        # An atomic replacement may already have unlinked the open snapshot.
        if not stat.S_ISREG(info.st_mode) or info.st_uid != os.geteuid() or info.st_nlink > 1:
            raise ValueError("Filtered MCP credentials must be a user-owned regular file.")
        if stat.S_IMODE(info.st_mode) != 0o600:
            raise ValueError("Filtered MCP credentials must have mode 600; rebuild the devcontainer.")
        prefix = b"ZAI_API_KEY="
        keys = []
        for line in file:
            if line.startswith(prefix):
                keys.append(line[len(prefix):].removesuffix(b"\n").removesuffix(b"\r"))
            elif not standalone:
                raise ValueError("Unexpected assignment in the private Claude credential file.")
        if len(keys) != 1:
            raise ValueError("Filtered MCP credentials contain an invalid ZAI_API_KEY.")
        return validate_key(keys[0])


def main(*, standalone=False):
    if len(sys.argv) != 3 or sys.argv[1] not in ("stage", "read"):
        sys.exit("Expected a stage or read operation and a workspace directory.")
    workspace = Path(sys.argv[2])
    try:
        if sys.argv[1] == "stage":
            stage(workspace)
        else:
            if standalone:
                parent = workspace / ".devcontainer"
                if parent.is_symlink():
                    raise ValueError("Filtered MCP credentials must have a real parent directory.")
                path = parent / ".mcp.env"
            else:
                path = private_file(workspace, create=False)
            sys.stdout.buffer.write(read_key(path, standalone=standalone))
    except FileNotFoundError:
        sys.exit("Filtered MCP credentials are missing; rebuild the devcontainer.")
    except (OSError, ValueError) as error:
        sys.exit(f"Cannot {sys.argv[1]} the Claude MCP credential: {error}")


if __name__ == "__main__":
    main()
