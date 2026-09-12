#!/usr/bin/env bash
# One-time host setup. Run from a trusted SCP_Team3 checkout with sudo.
set -euo pipefail

[[ $EUID -eq 0 ]] || { echo 'Run with sudo.' >&2; exit 1; }
SOURCE_DIR=$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)
RUNNER_USER=${1:-${SUDO_USER:-}}
[[ -n $RUNNER_USER ]] || { echo 'Usage: sudo ./infra/install.sh <github-runner-user>' >&2; exit 2; }
id "$RUNNER_USER" >/dev/null
getent group docker >/dev/null || { echo 'Docker group is missing.' >&2; exit 1; }

getent group finalversion-deploy >/dev/null || groupadd --system finalversion-deploy
usermod -aG docker,finalversion-deploy "$RUNNER_USER"
# The deployment group writes generated vhost files; nginx runs as an unprivileged
# container user and needs read/traverse access to load them.
install -d -o root -g finalversion-deploy -m 2775 /var/lib/finalversion/nginx/conf.d
install -d -o root -g root -m 0755 /etc/finalversion /usr/local/lib/finalversion
install -m 0750 -o root -g finalversion-deploy "$SOURCE_DIR/infra/finalversion" /usr/local/bin/finalversion
install -m 0750 -o root -g finalversion-deploy "$SOURCE_DIR/infra/lifecycle-controller.py" /usr/local/lib/finalversion/lifecycle-controller.py

cat > /etc/finalversion/finalversion.env <<EOF
FV_SOURCE_DIR=$SOURCE_DIR
FV_STATE_DIR=/var/lib/finalversion
FV_NGINX_CONF_DIR=/var/lib/finalversion/nginx/conf.d
FV_MAIN_HOST=finalversion.dev
FV_DEV_HOST=dev.finalversion.dev
FV_PREVIEW_DOMAIN=finalversion.dev
FV_IDLE_SECONDS=1800
FV_START_TIMEOUT=45
EOF
chown root:finalversion-deploy /etc/finalversion/finalversion.env
chmod 0640 /etc/finalversion/finalversion.env

install -m 0644 "$SOURCE_DIR/infra/systemd/finalversion-controller.service" /etc/systemd/system/
install -m 0644 "$SOURCE_DIR/infra/systemd/finalversion-proxy.service" /etc/systemd/system/
install -m 0644 "$SOURCE_DIR/infra/systemd/finalversion-idle.service" /etc/systemd/system/
install -m 0644 "$SOURCE_DIR/infra/systemd/finalversion-idle.timer" /etc/systemd/system/
systemctl daemon-reload
systemctl enable --now finalversion-controller.service finalversion-proxy.service finalversion-idle.timer

echo "Installed. Log out and back in before using the runner account '$RUNNER_USER'."
