# Pi deployment handoff

This repository runs its runtime images only on the Raspberry Pi. No Docker image is pushed to a registry.

## What the stack does

- `finalversion.dev` is the persistent `main` deployment.
- `dev.finalversion.dev` is the persistent `dev` deployment.
- `pr-<number>.finalversion.dev` is an on-demand preview. It sleeps after 30 minutes without a request. nginx calls a loopback-only host controller before forwarding each preview request; that controller starts a known stopped container and waits for its Docker health check.
- Every deployment has an `--internal` Docker network using isolated gateway mode, its own SQLite volume, a read-only non-root application container, no published application port, no capabilities, no Docker socket, and CPU/RAM/PID limits.
- The only published service is hardened nginx at `127.0.0.1:8080`. `cloudflared` on the host reaches it. nginx is the only container joined to the edge and application networks.

The lifecycle controller has the Docker socket because it runs on the trusted host. nginx and all application containers do not have it. It serializes deploy, wake, sleep, and removal through `/var/lib/finalversion/lifecycle.lock` and persists deployments in `/var/lib/finalversion/deployments.json`.

## One-time Pi setup

1. Install Docker Engine and the Compose plugin if they are not present. Do **not** expose Docker's API over TCP.
2. Clone this repository at a stable host path and run the installation script as root, naming the account that runs the self-hosted GitHub runner:

   ```bash
   git clone git@github.com:bartek-hr/SCP_Team3.git /srv/SCP_Team3
   cd /srv/SCP_Team3
   sudo ./infra/install.sh <runner-user>
   ```

   This creates the `finalversion-deploy` group, installs the host controller and services, and starts nginx. The named runner account is added to Docker's group, which is host-equivalent privilege: only a tightly restricted, trusted runner may use it. Log out and back in after this command.

3. Install a GitHub self-hosted runner with the `finalversion` label. Restrict the runner group so only this repository's protected branch workflows can use it. The normal test workflow remains GitHub-hosted.
4. Protect `main` and `dev`: only trusted maintainers may push or edit their workflows. Do not run `pull_request` jobs from forks on this runner. Preview deployment is intentionally a maintainer-only manual dispatch, because building a PR Dockerfile on a Docker-privileged Pi executes that code with host-equivalent power.

## Cloudflare Tunnel setup

Perform these account actions from a PC authenticated to Cloudflare:

1. Create a tunnel for the Pi and install `cloudflared` on the Pi using Cloudflare's generated token/service command.
2. Add public hostnames pointing at the local service:

   | Hostname | Service |
   | --- | --- |
   | `finalversion.dev` | `http://127.0.0.1:8080` |
   | `dev.finalversion.dev` | `http://127.0.0.1:8080` |
   | `*.finalversion.dev` | `http://127.0.0.1:8080` |

3. Disable Cloudflare caching for `pr-*.finalversion.dev/*` (or add a Cache Rule that bypasses cache). Preview responses also send `Cache-Control: no-store`; the cache rule ensures requests always hit the wake path.

The source hostnames are defaults. Change `FV_MAIN_HOST`, `FV_DEV_HOST`, and `FV_PREVIEW_DOMAIN` in `/etc/finalversion/finalversion.env`, then run `sudo systemctl restart finalversion-controller finalversion-proxy` if your domain differs.

## Operations

All commands run on the Pi from a checkout, or from `/usr/local/bin/finalversion` after installation:

```bash
finalversion deploy main <full-commit-sha>
finalversion deploy dev <full-commit-sha>
finalversion deploy-pr 42 <full-commit-sha>
finalversion remove-pr 42
finalversion sleep-idle
```

`deploy` builds locally. Its Dockerfile's final runtime stage depends on `dotnet test`, so a failing build or test never changes the currently proxied container. A candidate must also become healthy before nginx is reloaded. Closed-preview cleanup removes its container, isolated network, SQLite volume, nginx config, state record, and local image.

For a stopped preview, the first `GET` request waits for readiness and is forwarded normally. If startup exceeds 45 seconds, nginx returns a temporary `503` with `Retry-After: 5`, while the container continues to start. Since nginx wakes before proxying, no request that may have reached the application is automatically replayed.

## Acceptance checks

```bash
# Docker build includes restore, build, tests, and publish.
docker build --network=host -t cargo-hub-check .

# Start nginx and a reviewed preview.
finalversion bootstrap
finalversion deploy-pr 42 <full-commit-sha>
curl -i -H 'Host: pr-42.finalversion.dev' http://127.0.0.1:8080/health/ready

# App has no host port and is isolated; only nginx joins its network.
docker inspect fv-pr-42-<short-sha>
docker network inspect fv-pr-42

# Confirm wake after the idle task stops it.
finalversion sleep-idle
docker ps -a --filter name=fv-pr-42
curl -i -H 'Host: pr-42.finalversion.dev' http://127.0.0.1:8080/health/ready
```

Do not run `docker system prune` as part of this stack: its scope is broader than this project and could remove unrelated Pi workloads.
