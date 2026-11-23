"""Generate reference amounts using Python Decimal, never the C# parser."""
import json
import random
import sys
from decimal import Decimal, localcontext
from pathlib import Path

target = Path(sys.argv[1])
target.parent.mkdir(parents=True, exist_ok=True)
if target.exists():
    raise SystemExit("Use a new numeric reference path")
vectors = []
rng = random.Random(20261123)
units = [0, 1, -1, 99, -99, 100, -100, 511, -511,
         2**63 - 1, -(2**63), (2**63 - 1) // 100, -(2**63) // 100]
units += [rng.randrange(-(2**63), 2**63) for _ in range(512)]
with localcontext() as ctx:
    ctx.prec = 80
    for n in units:
        text = format(Decimal(n) / Decimal(100), ".2f")
        expected = str(int(Decimal(text) * Decimal(100)))
        assert expected == str(n)
        vectors.append(dict(adapter="decimal", input=text, units=expected))
        vectors.append(dict(adapter="scaled", input=str(n), units=str(n)))

for text, expected in [("0005.1", "510"), ("5", "500"), ("-0.00", "0"), ("0.01", "1")]:
    vectors.append(dict(adapter="decimal", input=text, units=expected))
for adapter, text, error in [
    ("decimal", "92233720368547758.08", "PRICE_RANGE"),
    ("decimal", "-92233720368547758.09", "PRICE_RANGE"),
    ("scaled", "9223372036854775808", "PRICE_RANGE"),
    ("scaled", "-9223372036854775809", "PRICE_RANGE"),
    ("decimal", "1.001", "PRICE_FORMAT"), ("decimal", "1e2", "PRICE_FORMAT"),
    ("decimal", " 1.00", "PRICE_FORMAT"), ("decimal", "+1.00", "PRICE_FORMAT"),
    ("decimal", "1,00", "PRICE_FORMAT"), ("decimal", ".1", "PRICE_FORMAT"),
    ("decimal", "1.", "PRICE_FORMAT"), ("decimal", "١.٠٠", "PRICE_FORMAT"),
    ("scaled", "1.0", "PRICE_FORMAT"), ("scaled", "+1", "PRICE_FORMAT"),
    ("scaled", "1\n", "PRICE_FORMAT"), ("decimal", "1.00\n", "PRICE_FORMAT"),
    ("decimal", "0" * 33, "PRICE_FORMAT"), ("scaled", "0" * 33, "PRICE_FORMAT"),
]:
    vectors.append(dict(adapter=adapter, input=text, error=error))
target.write_text(json.dumps(dict(reference="Python Decimal; no product import", seed=20261123,
    tolerance="exact", vectors=vectors), indent=2) + "\n", encoding="utf-8", newline="\n")
print(f"Wrote {len(vectors)} independent numeric vectors")
