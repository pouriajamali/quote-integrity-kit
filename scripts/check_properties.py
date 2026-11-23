"""Finite seeded numerical, row-order, revision/cutoff and resource controls."""
import copy
import hashlib
import json
import random
import subprocess
import sys
from decimal import Decimal
from pathlib import Path

root = Path(__file__).resolve().parents[1]
cli = Path(sys.argv[1]).resolve()
work = Path(sys.argv[2]).resolve()
work.mkdir(parents=True, exist_ok=False)
base = json.loads((root / "fixtures" / "clean.json").read_text(encoding="utf-8"))
rng = random.Random(20261123)

def assess(name, data, code):
    path = work / (name + ".json")
    path.write_text(json.dumps(data), encoding="utf-8", newline="\n")
    p = subprocess.run([str(cli), "validate", str(path), str(work / name)], capture_output=True, text=True, timeout=20)
    assert p.returncode == code, (name, p.returncode, p.stdout, p.stderr)
    if code == 1:
        assert not (work / name).exists()
        return None
    result = json.loads((work / name / "report.json").read_bytes())
    assert result["inputSha256"] == hashlib.sha256(path.read_bytes()).hexdigest()
    return result

many = copy.deepcopy(base)
many["quotes"] = []
many["expectedIds"] = []
oracle = {}
for i in range(1000):
    bid = rng.randrange(0, 1000001)
    ask = bid + rng.randrange(0, 1001)
    qid = f"q{i:04d}"
    row = dict(base["quotes"][0], id=qid, bid=format(Decimal(bid)/100, ".2f"), ask=format(Decimal(ask)/100, ".2f"), mark=None)
    many["quotes"].append(row)
    many["expectedIds"].append(qid)
    oracle[qid] = dict(bid=str(bid), ask=str(ask), midpoint=(Decimal(bid)+Decimal(ask))/200,
                       bidContract=str(bid*100), askContract=str(ask*100))
decimal = assess("1000-decimal", many, 0)
assert decimal["accepted"] == 1000 and decimal["coverage"] == "PASS"
for row in decimal["rows"]:
    expected = oracle[row["id"]]
    assert row["bidUnits"] == expected["bid"] and row["askUnits"] == expected["ask"]
    assert Decimal(row["midpoint"]) == expected["midpoint"]
    assert row["bidContractCents"] == expected["bidContract"] and row["askContractCents"] == expected["askContract"]

scaled = copy.deepcopy(many)
scaled["schema"] = "quote-integrity-scaled-v1"
for row in scaled["quotes"]:
    o = oracle[row["id"]]
    row["bidUnits"], row["askUnits"], row["markUnits"] = o["bid"], o["ask"], None
    for key in ["bid", "ask", "mark"]:
        del row[key]
integer = assess("1000-scaled", scaled, 0)
assert decimal["rows"] == integer["rows"]
permuted = copy.deepcopy(many)
rng.shuffle(permuted["quotes"])
reordered = assess("permuted", permuted, 0)
def by_id(report):
    return {r["id"]: {k: v for k, v in r.items() if k != "ordinal"} for r in report["rows"]}
assert by_id(decimal) == by_id(reordered)

with_mark = copy.deepcopy(base)
with_mark["quotes"][0]["mark"] = "99.99"
mark = assess("mark-is-not-bid-ask", with_mark, 0)
assert mark["rows"][0]["midpoint"] == "5.105" and mark["rows"][0]["bidUnits"] == "510"

late = copy.deepcopy(base)
late["quotes"][0]["observedAt"] = "2026-11-20T12:00:01Z"
late["quotes"][0]["bid"] = "5.09"
revision = assess("late-revision", late, 2)
assert revision["rows"][0]["reasons"] == ["NOT_OBSERVED_AT_CUTOFF"] and revision["accepted"] == 0
old = assess("original-after-late", base, 0)
assert old["rows"][0]["bidUnits"] == "510"
assert old["inputSha256"] != revision["inputSha256"]

over = copy.deepcopy(many)
over["quotes"].append(copy.deepcopy(over["quotes"][0]))
assess("1001-rows-refused", over, 1)
over_grid = copy.deepcopy(many)
over_grid["expectedIds"].append("q1000")
assess("1001-expected-refused", over_grid, 1)
over_bytes = copy.deepcopy(base)
over_bytes["quotes"][0]["huge"] = "a" * (1024 * 1024)
assess("byte-cap-refused", over_bytes, 1)
print("P5: 1000 Decimal amount/midpoint/notional comparisons, 1000-row cross-adapter equivalence, permutation/mark/revision controls and 3 caps passed; no timing benchmark")
