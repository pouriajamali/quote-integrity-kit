"""Inspect a versioned report. No API call, order, model or implicit admission."""
import json
import sys
from decimal import Decimal
from pathlib import Path

report = json.loads(Path(sys.argv[1]).read_bytes())
if report["schema"] != "quote-integrity-report-v2" or report["profile"] != "synthetic-european-call-v1":
    raise SystemExit("Unsupported report/profile: explicit migration required")
if report["sourceSchema"] not in ("quote-integrity-v1", "quote-integrity-scaled-v1"):
    raise SystemExit("Unsupported source adapter")
print(f"{report['decision']}: {report['accepted']} accepted, {report['quarantined']} quarantined")
for row in report["rows"]:
    if row["decision"] == "ACCEPT":
        bid = Decimal(row["bidUnits"]) / 100
        ask = Decimal(row["askUnits"]) / 100
        midpoint = Decimal(row["midpoint"])
        assert midpoint == (bid + ask) / 2
        print(f"row {row['ordinal']} {row['id']}: bid={bid}, ask={ask}, arithmetic midpoint={midpoint} USD/unit; fill unknown")
print("Declared profile checks only; legal rights and source authenticity remain unproven.")
raise SystemExit(0 if report["decision"] == "ADMIT_PROFILE" else 2)
