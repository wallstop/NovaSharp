#!/usr/bin/env bash

set -euo pipefail

script_dir=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)
test_root=$(mktemp -d)
trap 'rm -rf -- "$test_root"' EXIT

mkdir -p "$test_root/bin" "$test_root/config" "$test_root/state"

cat >"$test_root/bin/nmcli" <<'EOF'
#!/usr/bin/env bash
set -euo pipefail
if [[ "$*" == "--terse --escape no --fields DEVICE,TYPE,STATE,CONNECTION device status" ]]; then
  [[ ${MOCK_NMCLI_DEVICE_ERROR:-no} == no ]] || exit 69
  if [[ -n ${MOCK_NMCLI_DEVICE_FAIL_AFTER:-} ]]; then
    count=0
    [[ ! -e "$MOCK_NMCLI_DEVICE_COUNT" ]] || count=$(<"$MOCK_NMCLI_DEVICE_COUNT")
    count=$((count + 1))
    printf '%d\n' "$count" >"$MOCK_NMCLI_DEVICE_COUNT"
    ((count <= MOCK_NMCLI_DEVICE_FAIL_AFTER)) || exit 69
  fi
  cat "$MOCK_NMCLI_DEVICES"
elif [[ "$*" == "--terse --fields WIFI general" ]]; then
  cat "$MOCK_NMCLI_RADIO"
elif [[ "$*" == "radio wifi off" ]]; then
  printf 'disabled\n' >"$MOCK_NMCLI_RADIO"
  printf 'off\n' >>"$MOCK_NMCLI_ACTIONS"
elif [[ "$*" == "radio wifi on" ]]; then
  printf 'enabled\n' >"$MOCK_NMCLI_RADIO"
  printf 'on\n' >>"$MOCK_NMCLI_ACTIONS"
elif [[ "$*" == monitor ]]; then
  printf 'NetworkManager event\n'
else
  printf 'unexpected nmcli arguments: %s\n' "$*" >&2
  exit 64
fi
EOF
chmod +x "$test_root/bin/nmcli"

export PATH="$test_root/bin:$PATH"
export MOCK_NMCLI_DEVICES="$test_root/devices"
export MOCK_NMCLI_RADIO="$test_root/radio"
export MOCK_NMCLI_ACTIONS="$test_root/actions"
export MOCK_NMCLI_DEVICE_COUNT="$test_root/device-count"
export PREFER_WIRED_CONFIG_FILE="$test_root/config/policy"
export PREFER_WIRED_STATE_DIR="$test_root/state"

write_config() {
  local enabled=$1 interfaces=$2 restore=$3
  cat >"$PREFER_WIRED_CONFIG_FILE" <<EOF
PREFER_WIRED_ENABLED=$enabled
PREFER_WIRED_INTERFACES="$interfaces"
PREFER_WIRED_RESTORE_WIFI=$restore
PREFER_WIRED_RECONCILE_SECONDS=30
PREFER_WIRED_MONITOR_RESTART_SECONDS=2
EOF
}

run_once() {
  "$script_dir/prefer-wired-network.sh" --once
}

assert_radio() {
  local expected=$1
  [[ $(<"$MOCK_NMCLI_RADIO") == "$expected" ]]
}

# A connected allowed Ethernet interface disables Wi-Fi and records ownership.
printf 'enp5s0:ethernet:connected:Wired connection 1\nwlan0:wifi:disconnected:\n' \
  >"$MOCK_NMCLI_DEVICES"
printf 'enabled\n' >"$MOCK_NMCLI_RADIO"
: >"$MOCK_NMCLI_ACTIONS"
write_config yes enp5s0 yes
run_once
assert_radio disabled
[[ -e "$PREFER_WIRED_STATE_DIR/wifi-disabled-by-service" ]]
[[ $(<"$MOCK_NMCLI_ACTIONS") == off ]]

# Reconciliation is idempotent while Ethernet remains connected.
run_once
[[ $(wc -l <"$MOCK_NMCLI_ACTIONS") -eq 1 ]]

# Losing Ethernet restores Wi-Fi when this service disabled it.
printf 'enp5s0:ethernet:disconnected:\nwlan0:wifi:disconnected:\n' \
  >"$MOCK_NMCLI_DEVICES"
run_once
assert_radio enabled
[[ ! -e "$PREFER_WIRED_STATE_DIR/wifi-disabled-by-service" ]]
[[ $(tail -1 "$MOCK_NMCLI_ACTIONS") == on ]]

# A user-disabled radio is not restored because there is no ownership marker.
printf 'disabled\n' >"$MOCK_NMCLI_RADIO"
: >"$MOCK_NMCLI_ACTIONS"
run_once
assert_radio disabled
[[ ! -s "$MOCK_NMCLI_ACTIONS" ]]

# The allowlist prevents an unrelated Ethernet connection from changing Wi-Fi.
printf 'enp6s0:ethernet:connected:Dock\nwlan0:wifi:disconnected:\n' \
  >"$MOCK_NMCLI_DEVICES"
printf 'enabled\n' >"$MOCK_NMCLI_RADIO"
write_config yes enp5s0 yes
run_once
assert_radio enabled

# Disabling the policy restores a radio that the service previously disabled.
printf 'disabled\n' >"$MOCK_NMCLI_RADIO"
: >"$PREFER_WIRED_STATE_DIR/wifi-disabled-by-service"
write_config no enp5s0 yes
run_once
assert_radio enabled
[[ ! -e "$PREFER_WIRED_STATE_DIR/wifi-disabled-by-service" ]]

# Restore can be deliberately suppressed for appliances without Wi-Fi failover.
printf 'disabled\n' >"$MOCK_NMCLI_RADIO"
: >"$PREFER_WIRED_STATE_DIR/wifi-disabled-by-service"
write_config yes enp5s0 no
run_once
assert_radio disabled
[[ -e "$PREFER_WIRED_STATE_DIR/wifi-disabled-by-service" ]]

# Invalid configuration fails visibly instead of silently reporting success.
write_config maybe enp5s0 yes
if run_once; then
  printf 'invalid configuration unexpectedly succeeded\n' >&2
  exit 1
else
  rc=$?
fi
[[ $rc -eq 2 ]]

# Monitor mode propagates policy failures so systemd can report and restart a
# broken worker instead of presenting a false active state.
if "$script_dir/prefer-wired-network.sh" --monitor; then
  printf 'monitor mode masked invalid configuration\n' >&2
  exit 1
else
  rc=$?
fi
[[ $rc -eq 2 ]]

# Failure to observe device state is not misclassified as loss of Ethernet.
write_config yes enp5s0 yes
printf 'disabled\n' >"$MOCK_NMCLI_RADIO"
: >"$PREFER_WIRED_STATE_DIR/wifi-disabled-by-service"
export MOCK_NMCLI_DEVICE_ERROR=yes
if run_once; then
  printf 'device observation failure unexpectedly succeeded\n' >&2
  exit 1
else
  rc=$?
fi
unset MOCK_NMCLI_DEVICE_ERROR
[[ $rc -eq 2 ]]
assert_radio disabled
[[ -e "$PREFER_WIRED_STATE_DIR/wifi-disabled-by-service" ]]

# A reconciliation error triggered by a monitor event also reaches systemd.
printf 'enp5s0:ethernet:disconnected:\nwlan0:wifi:unavailable:\n' \
  >"$MOCK_NMCLI_DEVICES"
printf 'disabled\n' >"$MOCK_NMCLI_RADIO"
[[ ! -e "$PREFER_WIRED_STATE_DIR/wifi-disabled-by-service" ]] ||
  rm -f -- "$PREFER_WIRED_STATE_DIR/wifi-disabled-by-service"
: >"$MOCK_NMCLI_DEVICE_COUNT"
export MOCK_NMCLI_DEVICE_FAIL_AFTER=1
if "$script_dir/prefer-wired-network.sh" --monitor; then
  printf 'monitor event masked device observation failure\n' >&2
  exit 1
else
  rc=$?
fi
unset MOCK_NMCLI_DEVICE_FAIL_AFTER
[[ $rc -eq 2 ]]
assert_radio disabled

# Ownership must be durable before changing radio state. A state-path failure
# therefore leaves Wi-Fi enabled and returns an error.
printf 'enp5s0:ethernet:connected:Wired connection 1\n' >"$MOCK_NMCLI_DEVICES"
printf 'enabled\n' >"$MOCK_NMCLI_RADIO"
PREFER_WIRED_STATE_DIR="$test_root/state-is-a-file"
export PREFER_WIRED_STATE_DIR
: >"$PREFER_WIRED_STATE_DIR"
if run_once; then
  printf 'state persistence failure unexpectedly succeeded\n' >&2
  exit 1
else
  rc=$?
fi
[[ $rc -eq 1 ]]
assert_radio enabled
PREFER_WIRED_STATE_DIR="$test_root/state"
export PREFER_WIRED_STATE_DIR

printf 'prefer-wired-network tests passed\n'
