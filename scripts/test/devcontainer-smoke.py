"""Exercise a committed NovaSharp checkout using the Dev Containers CLI and Docker."""

import argparse
from contextlib import ExitStack
import hashlib
import json
import os
from pathlib import Path
import shutil
import subprocess
import tempfile
import uuid


ROOT = Path(__file__).resolve().parents[2]
GITHUB = "GITHUB_PERSONAL_ACCESS_TOKEN"
ZAI = "ZAI_API_KEY"
UNRELATED = "NOVASHARP_SMOKE_UNRELATED"


def require(condition, message):
    if not condition:
        raise RuntimeError(message)


def cli_command():
    executable = shutil.which("devcontainer")
    require(executable, "Install @devcontainers/cli and put devcontainer on PATH.")
    # npm's Windows batch wrapper reparses command-line metacharacters.
    if os.name == "nt" and Path(executable).suffix.lower() in {".cmd", ".bat"}:
        for root in (Path(executable).parent, Path(executable).parent.parent):
            for modules in (root / "node_modules", root):
                entry = modules / "@devcontainers" / "cli" / "devcontainer.js"
                if entry.is_file():
                    return ["node", str(entry)]
        raise RuntimeError("Cannot locate devcontainer.js beside the npm launcher.")
    return [executable]


def sha(value):
    return hashlib.sha256(value.encode()).hexdigest()


# Run as the devcontainer user. The installed wrappers launch these local stdio
# MCP doubles. Neither the test command line nor the MCP replies contain keys.
MCP_PROBE = r'''
import hashlib, json, os, pathlib, subprocess, sys, tempfile

def sha(value):
    return hashlib.sha256(value.encode()).hexdigest()

github, zai = sys.argv[1:3]
assert sha(os.environ["GITHUB_PERSONAL_ACCESS_TOKEN"]) == github
assert sha(os.environ["ZAI_API_KEY"]) == zai
assert "NOVASHARP_SMOKE_UNRELATED" not in os.environ
request = {"jsonrpc": "2.0", "id": 1, "method": "initialize", "params": {
    "protocolVersion": "2025-03-26", "capabilities": {}, "clientInfo": {"name": "smoke", "version": "1"}}}
server = r"""#!/usr/bin/env python3
import hashlib, json, os, pathlib, sys
request = json.loads(input())
assert request['method'] == 'initialize'
for key in ('GITHUB_PERSONAL_ACCESS_TOKEN', 'ZAI_API_KEY', 'NOVASHARP_SMOKE_UNRELATED'):
    assert key not in os.environ
if pathlib.Path(sys.argv[0]).name == 'zai-mcp-server':
    key = os.environ['Z_AI_API_KEY']
else:
    assert sys.argv[1] == 'https://api.z.ai/api/mcp/web_search_prime/mcp'
    assert 'Z_AI_API_KEY' not in os.environ and sys.argv[2] == '--header-file'
    header = pathlib.Path(sys.argv[3]).read_bytes()
    assert header.startswith(b'Authorization: Bearer ') and header.endswith(b'\n') and header.count(b'\n') == 1
    key = header.removeprefix(b'Authorization: Bearer ').removesuffix(b'\n').decode()
assert hashlib.sha256(key.encode()).hexdigest() == os.environ['EXPECTED_DIGEST']
print(json.dumps({'jsonrpc':'2.0','id':request['id'],'result':{
    'protocolVersion':request['params']['protocolVersion'],'capabilities':{},'serverInfo':{'name':'smoke','version':'1'}}}))
"""
with tempfile.TemporaryDirectory() as folder:
    for binary in ("mcp-remote", "zai-mcp-server"):
        path = pathlib.Path(folder, binary)
        path.write_text(server)
        path.chmod(0o700)
    env = dict(os.environ, PATH=folder + ":" + os.environ["PATH"], EXPECTED_DIGEST=zai)
    env.pop("CLAUDE_PROJECT_DIR", None)
    for claude in (False, True):
        if claude:
            env.pop("ZAI_API_KEY")
            env["CLAUDE_PROJECT_DIR"] = os.getcwd()
        for command in (["zai-mcp-vision"], ["zai-mcp-remote", "web-search"]):
            result = subprocess.run(command, env=env, input=json.dumps(request) + "\n",
                                    text=True, stdout=subprocess.PIPE, check=True)
            reply = json.loads(result.stdout)
            assert reply["id"] == 1 and reply["result"]["serverInfo"]["name"] == "smoke"
'''


def smoke(image, temp_parent):
    name = "novasharp-smoke-" + uuid.uuid4().hex
    secrets = {key: uuid.uuid4().hex for key in (GITHUB, ZAI, UNRELATED)}
    clean_env = {key: value for key, value in os.environ.items() if key not in secrets}
    cli = cli_command()
    label = "novasharp.smoke=" + name

    def run(*args, input_text=None, cwd=None):
        result = subprocess.run(
            list(map(str, args)), cwd=cwd, env=clean_env,
            input=input_text.encode("utf-8") if input_text is not None else None,
            stdout=subprocess.PIPE, stderr=subprocess.PIPE,
        )
        # Text-mode stdin on Windows would translate LF into CRLF inside
        # Dockerfiles and Bash programs executed by the Linux container.
        result.stdout = result.stdout.decode("utf-8", errors="replace")
        result.stderr = result.stderr.decode("utf-8", errors="replace")
        log = result.stdout + result.stderr
        require(not any(value in log for value in secrets.values()),
                f"{args[0]} {args[1]} disclosed a synthetic credential in its output.")
        if result.returncode:
            raise RuntimeError(f"{args[0]} {args[1]} exited {result.returncode}\n{log}")
        return result

    print("Dev Containers CLI", run(*cli, "--version").stdout.strip(), flush=True)
    print("Docker client/server", run("docker", "version", "--format",
          "{{.Client.Version}}/{{.Server.Version}}").stdout.strip(), flush=True)
    with tempfile.TemporaryDirectory(prefix="novasharp-smoke-", dir=temp_parent) as tmp, ExitStack() as cleanup:
        scratch = Path(tmp).resolve()
        workspace = scratch / "NovaSharp smoke"
        print(f"Cloning committed HEAD into {workspace}", flush=True)
        run("git", "clone", "--quiet", "--no-hardlinks", "-c", "core.longpaths=true", "--", ROOT, workspace)
        config_file = workspace / ".devcontainer" / "devcontainer.json"
        config = json.loads("\n".join(
            line for line in config_file.read_text(encoding="utf-8").splitlines()
            if not line.lstrip().startswith("//")
        ))
        mount = "source=novasharp-nuget-cache,target=/home/vscode/.nuget,type=volume"
        require(mount in config["mounts"], "Cannot isolate the project's NuGet volume.")
        config["mounts"][config["mounts"].index(mount)] = mount.replace("novasharp-nuget-cache", name)
        config_file.write_text(json.dumps(config), encoding="utf-8")
        context = (config_file.parent / config["build"]["context"]).resolve()
        ws_args = ["--workspace-folder", str(workspace), "--id-label", label]

        runtime = name + ":runtime"
        if image:
            # Avoid tagging or deleting a caller-owned image when given an ID.
            image_info = json.loads(run("docker", "image", "inspect", image).stdout)[0]
            source = next(iter(image_info["RepoTags"] or image_info["RepoDigests"] or [image_info["Id"]]))
        else:
            source = name + ":project"
            print("Building the project image and configured features", flush=True)
            run(*cli, "build", "--workspace-folder", workspace, "--image-name", source)
            cleanup.callback(run, "docker", "image", "rm", "--no-prune", source)
        # Prebuilt images can carry the original workspace mount and lifecycle in
        # their metadata. Apply the isolated fixture's config exactly once.
        empty = scratch / "empty"
        empty.mkdir()
        run("docker", "build", "-t", runtime, "-f", "-", empty,
            input_text=f'FROM {source}\nLABEL devcontainer.metadata="[]"\n')
        cleanup.callback(run, "docker", "image", "rm", "--no-prune", runtime)
        del config["build"]
        config.pop("features", None)
        config["image"] = runtime
        config_file.write_text(json.dumps(config), encoding="utf-8")
        run("docker", "volume", "create", "--label", label, name)
        cleanup.callback(run, "docker", "volume", "rm", name)

        def remove_containers():
            ids = run("docker", "ps", "-aq", "--filter", f"label={label}").stdout.split()
            if ids:
                run("docker", "rm", "-fv", *ids)

        cleanup.callback(remove_containers)

        def up(stage):
            print(stage, flush=True)
            result = run(*cli, "up", *ws_args)
            info = json.loads(result.stdout.strip().splitlines()[-1])
            require(info["outcome"] == "success", f"{stage} failed.")
            container = info["containerId"]
            source_name = run("docker", "inspect", "--format",
                              '{{range .Mounts}}{{if eq .Destination "/home/vscode/.nuget"}}{{.Name}}{{end}}{{end}}',
                              container).stdout.strip()
            require(source_name == name, "Wrong NuGet volume mounted.")
            require(info["remoteUser"] == "vscode", "Unexpected devcontainer user.")
            run(*cli, "exec", *ws_args, "zsh", "-ilc",
                '[[ -o interactive && -o login && "$PWD" == "$1" && "$(id -un)" == vscode '
                '&& "$(git rev-parse --show-toplevel)" == "$PWD" ]]',
                "smoke", info["remoteWorkspaceFolder"])
            run("docker", "exec", "-w", info["remoteWorkspaceFolder"], "-u", info["remoteUser"],
                container, "bash", "-ec",
                'test -w "$HOME/.nuget"; test -s src/runtime/WallstopStudios.NovaSharp.Interpreter/obj/project.assets.json')
            return info, result.stdout + result.stderr

        def exec_script(info, *command, script):
            run("docker", "exec", "-i", "-u", info["remoteUser"], "-w", info["remoteWorkspaceFolder"],
                info["containerId"], *command, input_text=script)

        no_keys = r"""
test -z "${GITHUB_PERSONAL_ACCESS_TOKEN+x}${ZAI_API_KEY+x}${NOVASHARP_SMOKE_UNRELATED+x}"
for runtime_credential in "$HOME"/.novasharp-mcp/*; do
    test -f "$runtime_credential" && test ! -s "$runtime_credential" || exit 1
done
bin=$(mktemp -d)
trap 'rm -rf -- "$bin"' EXIT
ln -s /bin/true "$bin/zai-mcp-server"
ln -s /bin/true "$bin/mcp-remote"
export PATH="$bin:$PATH" CLAUDE_PROJECT_DIR="$PWD" ZAI_API_KEY=unexpected-ambient-fallback
assert_refuses() {
    if output=$("$@" 2>&1); then
        echo "MCP launcher accepted a removed credential: $1" >&2
        exit 1
    fi
    case "$output" in
        *ZAI_API_KEY*) ;;
        *) printf '%s\n' "$output" >&2; exit 1 ;;
    esac
}
assert_refuses zai-mcp-vision
assert_refuses zai-mcp-remote web-search
"""
        source_env = workspace / ".env.local"
        filtered = config_file.parent / ".mcp.env"
        require(not source_env.exists(), "Fresh checkout unexpectedly includes .env.local.")
        fresh, _ = up("Starting with a fresh cache and no credentials")
        require(filtered.read_bytes() == b"", "Expected an empty filtered environment.")
        exec_script(fresh, "bash", "-e", script=no_keys)
        run("docker", "exec", "-u", "0", fresh["containerId"], "bash", "-ec",
            'cd /home/vscode/.nuget; touch .ownership-probe; chown 0:0 . .ownership-probe; chmod 700 .')
        remove_containers()
        source_env.write_bytes("".join(f"{key}={value}\r\n" for key, value in secrets.items()).encode())
        repaired, log = up("Recreating with a root-owned cache and synthetic credentials")
        require("Repaired ownership" in log, "The cache repair did not run.")
        expected = {f"{key}={secrets[key]}" for key in (GITHUB, ZAI)}
        require(set(filtered.read_text().splitlines()) == expected, "Wrong Docker env-file contents.")
        exec_script(repaired, "python3", "-", sha(secrets[GITHUB]), sha(secrets[ZAI]), script=MCP_PROBE)

        # Docker itself applies .dockerignore. Inspect the result of COPY over
        # the whole configured build context, including dotfiles, in scratch.
        exported = scratch / "exported-context"
        (filtered.parent / ".mcp.env.interrupted").write_bytes(filtered.read_bytes())
        run("docker", "build", "-f", "-", "--output", f"type=local,dest={exported}", context,
            input_text="FROM scratch\nCOPY . /\n")
        require((exported / "check-mcp-env.sh").is_file(), "Wrong Docker build context.")
        for path in exported.rglob("*"):
            if path.is_file():
                content = path.read_bytes()
                require(not any(secret.encode() in content for secret in secrets.values()),
                        "Docker included a synthetic credential in a COPY layer.")
        run("docker", "history", "--no-trunc", runtime)
        run("docker", "logs", repaired["containerId"])

        def owner_stamp(info):
            return run("docker", "exec", info["containerId"], "stat", "-c", "%u:%g %z",
                       "/home/vscode/.nuget/.ownership-probe").stdout

        stamp = owner_stamp(repaired)
        require(not stamp.startswith("0:"), "Cache probe is still root-owned after restore.")
        run("docker", "stop", repaired["containerId"])
        restarted, log = up("Restarting the repaired container and checking cache ownership")
        require(restarted["containerId"] == repaired["containerId"], "Restart replaced the container.")
        require("Repaired ownership" not in log and owner_stamp(restarted) == stamp,
                "Restart changed existing cache ownership.")
        remove_containers()
        source_env.unlink()
        cleared, _ = up("Recreating after removing credentials")
        require(filtered.read_bytes() == b"", "Stale credentials survived removal.")
        exec_script(cleared, "bash", "-e", script=no_keys)
    print("Devcontainer startup smoke test passed.")


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--image", help="Reuse a local project image built from this commit with its features.")
    parser.add_argument("--temp-parent", type=Path, help="Create the disposable checkout on this filesystem.")
    args = parser.parse_args()
    smoke(args.image, args.temp_parent)
