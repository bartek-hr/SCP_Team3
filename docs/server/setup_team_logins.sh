#!/usr/bin/env bash
# Maakt server-logins voor de teamleden uit github.com/bartek-hr/SCP_Team3.
# Elk teamlid krijgt een Linux-account zonder wachtwoord; inloggen kan alleen
# met de SSH-keys die op hun GitHub-profiel staan (github.com/<user>.keys).
#
# Gebruik op de server:
#   sudo bash setup_team_logins.sh          # accounts + keys
#   sudo bash setup_team_logins.sh --sudo   # idem, en geef ze ook sudo-rechten
#
# Het script is idempotent: nogmaals draaien ververst alleen de keys.
set -euo pipefail

# linux-gebruikersnaam => GitHub-gebruikersnaam
declare -A USERS=(
  [rmjtromp]=RMJTromp
  [jemostert]=JEMostert
)

GIVE_SUDO=0
[[ "${1:-}" == "--sudo" ]] && GIVE_SUDO=1

if [[ $EUID -ne 0 ]]; then
  echo "Dit script heeft root nodig. Draai: sudo bash $0 ${1:-}"
  exit 1
fi

fetch() {
  if command -v curl >/dev/null; then curl -fsSL "$1"
  elif command -v wget >/dev/null; then wget -qO- "$1"
  else echo "curl of wget is nodig" >&2; return 1
  fi
}

for linux_user in "${!USERS[@]}"; do
  gh_user="${USERS[$linux_user]}"
  echo "== $linux_user (github.com/$gh_user)"

  if ! keys="$(fetch "https://github.com/${gh_user}.keys")" || [[ -z "$keys" ]]; then
    echo "   !! geen SSH-keys gevonden op GitHub voor $gh_user, overgeslagen"
    continue
  fi

  if id "$linux_user" &>/dev/null; then
    echo "   account bestaat al, keys worden ververst"
  else
    useradd -m -s /bin/bash -c "$gh_user (GitHub)" "$linux_user"
    echo "   account aangemaakt"
  fi

  home="$(getent passwd "$linux_user" | cut -d: -f6)"
  install -d -m 700 -o "$linux_user" -g "$linux_user" "$home/.ssh"
  printf '%s\n' "$keys" > "$home/.ssh/authorized_keys"
  chown "$linux_user:$linux_user" "$home/.ssh/authorized_keys"
  chmod 600 "$home/.ssh/authorized_keys"
  echo "   $(printf '%s\n' "$keys" | wc -l) key(s) geïnstalleerd"

  if [[ $GIVE_SUDO -eq 1 ]]; then
    usermod -aG sudo "$linux_user"
    echo "   toegevoegd aan groep sudo"
  fi
done

echo
echo "Klaar. Teamleden loggen in met hun eigen GitHub-key:"
for linux_user in "${!USERS[@]}"; do
  echo "  ssh $linux_user@$(hostname -I | awk '{print $1}')"
done
