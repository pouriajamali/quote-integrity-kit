# P5: exact numerical reference and finite properties

The numerical benchmark here concerns **scaling and arithmetic**, not an option
pricing model. Python Decimal provides the independent reference with zero
tolerance; C# uses a BigInteger scaling parser, checked Int64 contract premium
and decimal midpoint. No QuantLib/model/calibration correctness follows.

The frozen seed is20261123. The numeric reference includes525 signed cent
amounts (explicit extremes and512 generated values), two adapters per amount,
four literal syntax controls and18 refusal cases:1072 vectors total. Int64
minimum/maximum, contract-safe boundaries, extra precision, exponent, locale,
non-ASCII digits, whitespace/newline and length errors are explicit.

The independent CLI property checks compare1000 original seeded nonnegative
bid/ask pairs, exact midpoint and contract cents to Python Decimal/integer
expectations; compare both adapters, then preserve decisions under row
permutation. Mark changes cannot replace bid/ask. A revision observed after
the declared cutoff is quarantined; the old snapshot remains unchanged and
has its own digest.1000 rows pass,1001 rows/expected ids and input above1MiB
are refused before a report directory is produced. Each CLI child has a20s
timeout; no throughput/latency comparison is derived from those safety bounds.

```powershell
. ./scripts/env.ps1
python -B scripts/numeric_reference.py artifacts/numeric-reference-new.json
dotnet run --project tests/QuoteIntegrity.Checks -c Release -- --numeric artifacts/numeric-reference-new.json
dotnet publish src/QuoteIntegrity.Cli -c Release --no-restore --no-self-contained -o artifacts/publish
python -B scripts/check_properties.py artifacts/publish/quote-integrity.exe artifacts/properties-new
```

This profile is a bounded offline snapshot and claims no streaming ingestion,
revision store or concurrency guarantee. A streaming adapter is not required
for this profile; introducing one would need a separate bounded contract and
tests. The revision control here is a cutoff refusal on supplied snapshots,
not integration with the pending CMP-04/05 owner contracts. Platform evidence
is Windows/.NET10 only. Full P3 remains deferred independently of these checks.
