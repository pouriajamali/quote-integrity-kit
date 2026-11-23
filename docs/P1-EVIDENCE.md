# P1: literal quote contract checks

The original synthetic corpus has39 cases with literal expected dataset/row
decisions, axes and reason sets. It covers valid/locked/zero/null-mark controls,
odd-cent midpoint, crossed/stale quotes, unknown/missing/type errors, expiry
equality, an instant before expiry, observation cutoff, invalid calendar dates,
currency/venue/instrument mismatch, exact price and contract-overflow boundaries,
unknown/denied rights, declared gaps and duplicates. Expected numbers have zero
tolerance. Quarantined rows cannot satisfy declared coverage.

Eight separate framing controls cover malformed/commented JSON, duplicate
envelope/row properties, invalid UTF8, empty/oversize input and a non-object root.
They must fail the whole input, without an admitted prefix.

Run from the repository root, with installed SDK10.0.303 and no package source:

```powershell
. ./scripts/env.ps1
dotnet run --project tests/QuoteIntegrity.Checks -c Release -- fixtures/literal-corpus.json
```

This is a finite synthetic contract check, not model validation, a real-data
quality rate, hosted CI or a platform/performance benchmark. The fixture catalogue
does not import the product and records expected outcomes explicitly. The
test harness compares literal expectations with actual C# results.
