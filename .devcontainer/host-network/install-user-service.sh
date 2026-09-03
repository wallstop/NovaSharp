#!/usr/bin/env bash

set -euo pipefail

script_dir=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)
config_home=${XDG_CONFIG_HOME:-$HOME/.config}
libexec_dir=$HOME/.local/libexec
service_dir=$config_home/systemd/user
policy_dir=$config_home/prefer-wired-network

install -d -m 0755 "$libexec_dir" "$service_dir" "$policy_dir"
install -m 0755 "$script_dir/prefer-wired-network.sh" \
  "$libexec_dir/prefer-wired-network"
install -m 0644 "$script_dir/prefer-wired-network.service" \
  "$service_dir/prefer-wired-network.service"
if [[ ! -e "$policy_dir/config" ]]; then
  install -m 0644 "$script_dir/config.example" "$policy_dir/config"
fi

systemctl --user daemon-reload
systemctl --user enable prefer-wired-network.service
systemctl --user restart prefer-wired-network.service
systemctl --user --no-pager --full status prefer-wired-network.service
