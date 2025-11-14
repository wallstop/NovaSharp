#!/usr/bin/env bash

set -euo pipefail

base_ref="${NOVASHARP_BASE_REF:-}"
if [[ -z "$base_ref" ]]; then
    if git rev-parse --verify HEAD^ >/dev/null 2>&1; then
        base_ref="HEAD^"
    else
        base_ref="$(git rev-parse HEAD)"
    fi
else
    if ! git rev-parse --verify "$base_ref" >/dev/null 2>&1; then
        echo "Base reference '$base_ref' is not available; falling back to HEAD^/HEAD." >&2
        if git rev-parse --verify HEAD^ >/dev/null 2>&1; then
            base_ref="HEAD^"
        else
            base_ref="$(git rev-parse HEAD)"
        fi
    fi
fi

tmpfile="$(mktemp)"
cleanup() {
    rm -f "$tmpfile"
}
trap cleanup EXIT

if ! git diff --name-status -z "$base_ref"...HEAD >"$tmpfile" 2>/dev/null; then
    git diff --name-status -z "$base_ref" HEAD >"$tmpfile"
fi

declare -A folder_requires_readme
declare -A folder_readme_changed
declare -A folder_file_map
declare -a new_script_files=()
declare -a new_doc_files=()
root_scripts_readme_changed=0
docs_index_changed=0

process_diff() {
    local status path old new folder
    while IFS= read -r -d '' status; do
        if [[ "$status" == R* ]]; then
            IFS= read -r -d '' old || true
            IFS= read -r -d '' new || true
            path="$new"
        else
            IFS= read -r -d '' path || true
        fi

        [[ -z "$path" ]] && continue

        if [[ "$path" == scripts/README.md ]]; then
            root_scripts_readme_changed=1
            continue
        fi

        if [[ "$path" == scripts/*/README.md ]]; then
            folder="${path#scripts/}"
            folder="${folder%%/*}"
            folder_readme_changed["$folder"]=1
            continue
        fi

        if [[ "$path" == scripts/* ]]; then
            folder="${path#scripts/}"
            folder="${folder%%/*}"
            if [[ "$status" == A* || "$status" == R* ]]; then
                new_script_files+=("$path")
                folder_requires_readme["$folder"]=1
                if [[ -n "${folder_file_map[$folder]:-}" ]]; then
                    folder_file_map["$folder"]+=", $path"
                else
                    folder_file_map["$folder"]="$path"
                fi
            fi
            continue
        fi

        if [[ "$path" == docs/README.md ]]; then
            docs_index_changed=1
            continue
        fi

        if [[ "$path" == docs/* && "$path" == *.md ]]; then
            if [[ "$status" == A* || "$status" == R* ]]; then
                new_doc_files+=("$path")
            fi
        fi
    done <"$tmpfile"
}

process_diff

errors=()

if ((${#new_script_files[@]})); then
    if ((root_scripts_readme_changed == 0)); then
        errors+=("scripts/README.md was not updated after adding new helper scripts: ${new_script_files[*]}")
    fi

    for folder in "${!folder_requires_readme[@]}"; do
        if [[ -z "${folder_readme_changed[$folder]:-}" ]]; then
            errors+=("scripts/${folder}/README.md must be updated when adding new files under scripts/${folder}/ (found: ${folder_file_map[$folder]})")
        fi
    done
fi

if ((${#new_doc_files[@]})) && ((docs_index_changed == 0)); then
    errors+=("docs/README.md must link new documentation files: ${new_doc_files[*]}")
fi

if ((${#errors[@]})); then
    printf '%s\n' "${errors[@]}" >&2
    exit 1
fi

echo "Documentation guard passed."
