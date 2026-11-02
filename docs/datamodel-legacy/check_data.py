"""Integriteitscontroles op legacy/data/*.json (draai vanuit legacy/data)."""
import json
from collections import Counter

L = lambda f: json.load(open(f + ".json", encoding="utf-8"))
T = {f: L(f) for f in ["client", "inventory", "item", "item_group", "item_line", "item_type", "location",
                       "order", "order_item", "shipment", "shipment_item", "supplier", "transfer",
                       "transfer_item", "warehouse"]}
ids = {k: {r["id"] for r in v if "id" in r} for k, v in T.items()}

def dups(t, k):
    return sum(1 for n in Counter(r[k] for r in T[t]).values() if n > 1)

print("== 1. Uniciteit")
for t, k in [("warehouse", "code"), ("location", "code"), ("item", "code"), ("item", "barcode"),
             ("supplier", "code"), ("order", "reference"), ("shipment", "reference"), ("transfer", "reference")]:
    print(f"{t}.{k}: {dups(t, k)} dubbele waarden")
print("inventory (item_id, location_id):", sum(1 for n in Counter((r["item_id"], r["location_id"]) for r in T["inventory"]).values() if n > 1), "dubbel")

print("\n== 2. Verwijzingen (wezen)")
fks = [("location", "warehouse_id", "warehouse"), ("inventory", "item_id", "item"), ("inventory", "location_id", "location"),
       ("item", "supplier_id", "supplier"), ("order", "client_id", "client"), ("order", "warehouse_id", "warehouse"),
       ("order_item", "order_id", "order"), ("shipment", "order_id", "order"), ("shipment_item", "shipment_id", "shipment"),
       ("transfer", "from_location_id", "location"), ("transfer", "to_location_id", "location"), ("transfer_item", "transfer_id", "transfer")]
for t, k, ref in fks:
    print(f"{t}.{k} -> {ref}: {sum(1 for r in T[t] if r.get(k) not in ids[ref])} wezen")

print("\n== 3. Bedrijfsregels")
orders = {r["id"]: r for r in T["order"]}
by_order = {}
for s in T["shipment"]:
    by_order.setdefault(s["order_id"], []).append(s)
print("shipments per order:", dict(Counter(len(v) for v in by_order.values())))
print("Incoming shipments gekoppeld aan klantorder:", sum(1 for s in T["shipment"] if s["shipment_type"] == "Incoming"))
print("Orders 'Shipped' zonder Outgoing shipment:", sum(1 for k, v in by_order.items() if orders[k]["order_status"] == "Shipped" and all(s["shipment_type"] != "Outgoing" for s in v)))
print("Orders 'Pending' met Outgoing shipment 'Delivered':", sum(1 for s in T["shipment"] if s["shipment_type"] == "Outgoing" and s["shipment_status"] == "Delivered" and orders[s["order_id"]]["order_status"] == "Pending"))
print("Items met packaging_type 'None' maar case_size > 1:", sum(1 for r in T["item"] if r["packaging_type"] == "None" and r["case_size"] > 1))
print("Items waar min_purchase_qty geen veelvoud van case_size is:", sum(1 for r in T["item"] if r["min_purchase_qty"] % r["case_size"]))
print("Negatieve voorraad:", sum(1 for r in T["inventory"] for k in ("quantity_on_hand", "quantity_expected", "quantity_ordered", "quantity_allocated") if r[k] < 0))
print("quantity_allocated > quantity_on_hand:", sum(1 for r in T["inventory"] if r["quantity_allocated"] > r["quantity_on_hand"]))
