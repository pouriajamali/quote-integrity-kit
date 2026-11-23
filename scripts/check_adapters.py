"""Independent literal controls for the integer adapter and snapshot bindings."""
import copy
import hashlib
import json
import subprocess
import sys
from pathlib import Path

root = Path(__file__).resolve().parents[1]
cli = Path(sys.argv[1]).resolve()
work = Path(sys.argv[2]).resolve()
work.mkdir(parents=True, exist_ok=False)
base = json.loads((root / "fixtures" / "clean.json").read_text(encoding="utf-8"))
scaled = copy.deepcopy(base)
scaled["schema"] = "quote-integrity-scaled-v1"
row = scaled["quotes"][0]
for key, value in [("bid", "510"), ("ask", "511"), ("mark", "700")]:
    del row[key]
    row[key + "Units"] = value

def assess(name, data, expected_code):
    path = work / (name + ".json")
    path.write_text(json.dumps(data), encoding="utf-8")
    p = subprocess.run([str(cli), "validate", str(path), str(work / name)], capture_output=True, text=True, timeout=20)
    assert p.returncode == expected_code, (name, p.returncode, p.stderr)
    report_bytes = (work / name / "report.json").read_bytes()
    report = json.loads(report_bytes)
    snapshot = json.loads((work / name / "snapshot.json").read_bytes())
    assert snapshot["inputSha256"] == hashlib.sha256(path.read_bytes()).hexdigest()
    assert snapshot["reportSha256"] == hashlib.sha256(report_bytes).hexdigest()
    assert snapshot["coreAssemblySha256"] == hashlib.sha256((cli.parent / "QuoteIntegrity.dll").read_bytes()).hexdigest()
    assert snapshot["cliAssemblySha256"] == hashlib.sha256((cli.parent / "quote-integrity.dll").read_bytes()).hexdigest()
    assert snapshot["cmp04"] == snapshot["cmp05"] == "NOT_INTEGRATED_OWNER_CONTRACT_PENDING"
    return report

decimal_report = assess("decimal", base, 0)
scaled_report = assess("scaled", scaled, 0)
assert decimal_report["rows"] == scaled_report["rows"]
assert scaled_report["rows"][0]["bidUnits"] == "510" and scaled_report["rows"][0]["midpoint"] == "5.105"
assert scaled_report["sourceSchema"] == "quote-integrity-scaled-v1"
assert decimal_report["sourceSchema"] == "quote-integrity-v1"
controls = [
    ("fraction-is-not-units", "510.0", ["BID_PRICE_FORMAT"]),
    ("exponent-is-not-units", "5e2", ["BID_PRICE_FORMAT"]),
    ("plus-is-not-units", "+510", ["BID_PRICE_FORMAT"]),
    ("missing-is-not-zero", None, ["MISSING_BID"]),
    ("overflow", "9223372036854775808", ["BID_PRICE_RANGE"]),
    ("contract-overflow", "92233720368547759", ["CONTRACT_PREMIUM_OVERFLOW"]),
    ("negative", "-1", ["NEGATIVE_BID"]),
    ("crossed", "512", ["CROSSED"]),
    ("wrong-token-type", 510, ["FIELD_TYPE", "MISSING_BID"]),
]
for name, value, reasons in controls:
    data = copy.deepcopy(scaled)
    data["quotes"][0]["bidUnits"] = value
    r = assess(name, data, 2)
    assert r["rows"][0]["reasons"] == reasons, (name, r["rows"])
print("Adapters: decimal/integer row equivalence + 9 rejection controls passed; 11 input/report/code snapshot bindings checked")
