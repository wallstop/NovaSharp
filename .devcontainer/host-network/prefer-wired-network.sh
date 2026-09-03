#!/usr/bin/env bash

# Keep Wi-Fi disabled while an eligible wired Ethernet interface is connected.
# This runs as a systemd user service and reacts to NetworkManager events.

set -uo pipefail

export LC_ALL=C

readonly program_name="prefer-wired-network"
config_file=${PREFER_WIRED_CONFIG_FILE:-${XDG_CONFIG_HOME:-$HOME/.config}/prefer-wired-network/config}
state_dir=${PREFER_WIRED_STATE_DIR:-${XDG_STATE_HOME:-$HOME/.local/state}/prefer-wired-network}
disabled_marker="$state_dir/wifi-disabled-by-service"

log() {
  printf '%s: %s\n' "$program_name" "$*" >&2
}

load_config() {
  PREFER_WIRED_ENABLED=yes
  PREFER_WIRED_INTERFACES=
  PREFER_WIRED_RESTORE_WIFI=yes
  PREFER_WIRED_RECONCILE_SECONDS=30
  PREFER_WIRED_MONITOR_RESTART_SECONDS=2

  if [[ -r "$config_file" ]]; then
    # This is a user-owned configuration file, equivalent to an EnvironmentFile.
    # shellcheck source=/dev/null
    source "$config_file"
  fi

  case "$PREFER_WIRED_ENABLED" in
    yes | no) ;;
    *)
      log "PREFER_WIRED_ENABLED must be 'yes' or 'no'"
      return 2
      ;;
  esac
  case "$PREFER_WIRED_RESTORE_WIFI" in
    yes | no) ;;
    *)
      log "PREFER_WIRED_RESTORE_WIFI must be 'yes' or 'no'"
      return 2
      ;;
  esac
  if [[ ! "$PREFER_WIRED_MONITOR_RESTART_SECONDS" =~ ^[1-9][0-9]*$ ]]; then
    log "PREFER_WIRED_MONITOR_RESTART_SECONDS must be a positive integer"
    return 2
  fi
  if [[ ! "$PREFER_WIRED_RECONCILE_SECONDS" =~ ^[1-9][0-9]*$ ]]; then
    log "PREFER_WIRED_RECONCILE_SECONDS must be a positive integer"
    return 2
  fi
}

interface_is_allowed() {
  local candidate=$1
  local allowed
  local -a allowed_interfaces=()

  [[ -z "$PREFER_WIRED_INTERFACES" ]] && return 0
  read -r -a allowed_interfaces <<<"$PREFER_WIRED_INTERFACES"
  for allowed in "${allowed_interfaces[@]}"; do
    [[ "$candidate" == "$allowed" ]] && return 0
  done
  return 1
}

wired_is_connected() {
  local device type state _connection
  local device_status

  if ! device_status=$(nmcli --terse --escape no \
    --fields DEVICE,TYPE,STATE,CONNECTION device status); then
    log "unable to read NetworkManager device state"
    return 2
  fi
  while IFS=: read -r device type state _connection; do
    if [[ "$type" == ethernet && "$state" == connected ]] &&
      interface_is_allowed "$device"; then
      return 0
    fi
  done <<<"$device_status"
  return 1
}

wifi_radio_state() {
  nmcli --terse --fields WIFI general
}

restore_wifi_if_owned() {
  [[ -e "$disabled_marker" ]] || return 0

  local radio_state
  if ! radio_state=$(wifi_radio_state); then
    log "unable to read Wi-Fi radio state"
    return 1
  fi
  if [[ "$radio_state" != enabled ]]; then
    if ! nmcli radio wifi on; then
      log "unable to enable the Wi-Fi radio"
      return 1
    fi
    log "enabled Wi-Fi because no eligible wired connection is active"
  fi
  rm -f -- "$disabled_marker"
}

reconcile() {
  local config_status
  if load_config; then
    :
  else
    config_status=$?
    return "$config_status"
  fi

  if [[ "$PREFER_WIRED_ENABLED" == no ]]; then
    restore_wifi_if_owned
    return $?
  fi

  local radio_state
  if ! radio_state=$(wifi_radio_state); then
    log "unable to read Wi-Fi radio state"
    return 1
  fi

  local wired_status
  if wired_is_connected; then
    if [[ "$radio_state" == enabled ]]; then
      if ! mkdir -p -- "$state_dir"; then
        log "unable to create policy state directory; leaving Wi-Fi enabled"
        return 1
      fi
      if ! : >"$disabled_marker"; then
        log "unable to persist Wi-Fi ownership marker; leaving Wi-Fi enabled"
        return 1
      fi
      if ! nmcli radio wifi off; then
        log "unable to disable the Wi-Fi radio"
        return 1
      fi
      log "disabled Wi-Fi because an eligible wired connection is active"
    fi
    return 0
  else
    wired_status=$?
    if [[ $wired_status -ne 1 ]]; then
      return "$wired_status"
    fi
  fi

  if [[ "$PREFER_WIRED_RESTORE_WIFI" == yes ]]; then
    restore_wifi_if_owned
  fi
}

monitor() {
  local monitor_status

  while true; do
    reconcile || return $?
    if timeout --foreground "${PREFER_WIRED_RECONCILE_SECONDS}s" nmcli monitor |
      while IFS= read -r _event; do
        reconcile || exit $?
      done; then
      monitor_status=0
    else
      monitor_status=$?
    fi

    case "$monitor_status" in
      124)
        # Expected heartbeat: resnapshot immediately to cover a missed event.
        ;;
      0)
        log "NetworkManager monitor stopped; restarting in ${PREFER_WIRED_MONITOR_RESTART_SECONDS}s"
        sleep "$PREFER_WIRED_MONITOR_RESTART_SECONDS"
        ;;
      *) return "$monitor_status" ;;
    esac
  done
}

usage() {
  printf 'Usage: %s {--once|--monitor|--restore}\n' "$program_name" >&2
}

case "${1:---monitor}" in
  --once) reconcile ;;
  --monitor) monitor ;;
  --restore) restore_wifi_if_owned ;;
  *)
    usage
    exit 2
    ;;
esac
