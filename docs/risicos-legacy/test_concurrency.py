"""Concurrency-tests op de legacy CargoHUB API (draai de API op een KOPIE van legacy/).

Gebruik: python test_concurrency.py <pad-naar-kopie>/data/user.json
"""
import http.client
import json
import socket
import sys
import threading
import time

HOST, PORT = "127.0.0.1", 3000
KEY = next(u["api_key"] for u in json.load(open(sys.argv[1], encoding="utf-8")) if u["app"] == "analytics_dashboard")


def get(path, timeout=400):
    start = time.perf_counter()
    try:
        c = http.client.HTTPConnection(HOST, PORT, timeout=timeout)
        c.request("GET", path, headers={"API_KEY": KEY})
        status = c.getresponse().status
    except Exception as e:
        status = f"geen antwoord ({type(e).__name__})"
    return status, time.perf_counter() - start


def parallel(slow_path, fast_path, delay=0.5):
    out = {}
    t = threading.Thread(target=lambda: out.setdefault("slow", get(slow_path)))
    t.start()
    time.sleep(delay)
    out["fast"] = get(fast_path)
    t.join()
    return out


print("== Test 1: referentie")
s, d = get("/api/v1/warehouses")
print(f"GET /warehouses alleen: {s} in {d*1000:.0f} ms")

print("\n== Test 2: snelle request terwijl een trage request loopt")
for slow in ["/api/v1/clients/1/orders", "/api/v1/orders"]:
    r = parallel(slow, "/api/v1/warehouses")
    print(f"GET {slow}: {r['slow'][0]} in {r['slow'][1]:.1f} s")
    print(f"  GET /warehouses 0,5 s later gestart: {r['fast'][0]} in {r['fast'][1]:.1f} s")

print("\n== Test 3: één client stuurt een onvolledige request (geen API-key nodig)")
sock = socket.create_connection((HOST, PORT))
sock.sendall(b"GET /api/v1/ware")  # geen regeleinde: server blijft wachten
time.sleep(0.5)
s, d = get("/api/v1/warehouses", timeout=15)
print(f"GET /warehouses terwijl de onvolledige request openstaat: {s} na {d:.1f} s")
sock.close()
s, d = get("/api/v1/warehouses")
print(f"GET /warehouses nadat die client de verbinding sluit: {s} in {d*1000:.0f} ms")
