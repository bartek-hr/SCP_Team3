#!/usr/bin/env python3
"""Private, host-only wake controller for finalversion preview containers."""
from __future__ import annotations

import fcntl
import http.client
import json
import os
import socket
import tempfile
import threading
import time
from contextlib import contextmanager
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path
from urllib.parse import quote, urlparse

STATE_DIR = Path(os.environ.get("FV_STATE_DIR", "/var/lib/finalversion"))
STATE_FILE = STATE_DIR / "deployments.json"
LOCK_FILE = STATE_DIR / "lifecycle.lock"
DOCKER_SOCKET = os.environ.get("DOCKER_SOCKET", "/var/run/docker.sock")
START_TIMEOUT = int(os.environ.get("FV_START_TIMEOUT", "45"))


def docker_request(method: str, path: str) -> tuple[int, bytes]:
    connection = socket.socket(socket.AF_UNIX, socket.SOCK_STREAM)
    connection.connect(DOCKER_SOCKET)
    try:
        connection.sendall(
            f"{method} {path} HTTP/1.1\r\nHost: docker\r\nConnection: close\r\nContent-Length: 0\r\n\r\n".encode()
        )
        response = http.client.HTTPResponse(connection)
        response.begin()
        return response.status, response.read()
    finally:
        connection.close()


def inspect_container(name: str) -> dict | None:
    status, body = docker_request("GET", f"/v1.45/containers/{quote(name, safe='')}/json")
    return json.loads(body) if status == 200 else None


def healthy(name: str) -> bool:
    container = inspect_container(name)
    if not container:
        return False
    state = container.get("State", {})
    health = state.get("Health")
    return health.get("Status") == "healthy" if health else state.get("Status") == "running"


def read_state() -> dict:
    try:
        with STATE_FILE.open() as handle:
            value = json.load(handle)
            return value if isinstance(value, dict) else {"deployments": {}}
    except FileNotFoundError:
        return {"deployments": {}}


def write_state(state: dict) -> None:
    STATE_DIR.mkdir(mode=0o770, parents=True, exist_ok=True)
    descriptor, temporary = tempfile.mkstemp(prefix="deployments.", dir=STATE_DIR)
    try:
        with os.fdopen(descriptor, "w") as handle:
            json.dump(state, handle, sort_keys=True)
            handle.write("\n")
            handle.flush()
            os.fsync(handle.fileno())
        os.chmod(temporary, 0o660)
        os.replace(temporary, STATE_FILE)
    finally:
        if os.path.exists(temporary):
            os.unlink(temporary)


@contextmanager
def lifecycle_lock():
    STATE_DIR.mkdir(mode=0o770, parents=True, exist_ok=True)
    with LOCK_FILE.open("a+") as handle:
        fcntl.flock(handle, fcntl.LOCK_EX)
        try:
            yield
        finally:
            fcntl.flock(handle, fcntl.LOCK_UN)


class WakeHandler(BaseHTTPRequestHandler):
    server_version = "finalversion-wake/1"

    def log_message(self, format: str, *args: object) -> None:
        print(f"{self.client_address[0]} {format % args}", flush=True)

    def do_GET(self) -> None:
        if urlparse(self.path).path == "/healthz":
            self.respond(204)
            return
        if urlparse(self.path).path != "/wake":
            self.respond(404)
            return

        # Only nginx can reach this loopback listener. Never trust a user-supplied
        # Host header: nginx writes this private header from its parsed server name.
        host = self.headers.get("X-Finalversion-Host", "").lower().strip().rstrip(".")
        if not host:
            self.respond(404)
            return

        with lifecycle_lock():
            state = read_state()
            deployment = next(
                (item for item in state.get("deployments", {}).values()
                 if item.get("kind") == "preview" and item.get("hostname") == host),
                None,
            )
            if not deployment:
                self.respond(404)
                return

            name = deployment["container"]
            if not healthy(name):
                container = inspect_container(name)
                if not container:
                    self.respond(404)
                    return
                current = container.get("State", {}).get("Status")
                if current in {"created", "exited"}:
                    status, _ = docker_request("POST", f"/v1.45/containers/{quote(name, safe='')}/start")
                    if status not in {204, 304}:
                        self.respond(503, "Preview could not start.")
                        return
                elif current not in {"running", "restarting"}:
                    self.respond(503, "Preview is unavailable.")
                    return

                deadline = time.monotonic() + START_TIMEOUT
                while time.monotonic() < deadline:
                    if healthy(name):
                        break
                    time.sleep(0.5)
                else:
                    # Docker continues starting in the background. nginx maps this
                    # private error to a short, non-cacheable warming response.
                    self.respond(503, "Preview is warming.", retry_after=True)
                    return

            deployment["last_activity"] = int(time.time())
            write_state(state)
            self.respond(204)

    def respond(self, status: int, body: str = "", retry_after: bool = False) -> None:
        self.send_response(status)
        self.send_header("Cache-Control", "no-store")
        if retry_after:
            self.send_header("Retry-After", "5")
        self.send_header("Content-Length", str(len(body.encode())))
        self.end_headers()
        if body:
            self.wfile.write(body.encode())


def main() -> None:
    server = ThreadingHTTPServer((os.environ.get("FV_CONTROLLER_HOST", "127.0.0.1"),
                                  int(os.environ.get("FV_CONTROLLER_PORT", "9081"))), WakeHandler)
    server.daemon_threads = True
    server.serve_forever()


if __name__ == "__main__":
    main()
