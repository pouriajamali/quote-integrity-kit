# Quote Integrity Kit

Inspect an offline option quote snapshot and explain why each row is accepted
or quarantined before analysis.

First profile: an explicitly fictional European USD call with fixed precision,
expiry and multiplier. Bid, ask, mark, midpoint and fill retain separate meanings.
See [the exact profile](docs/PROFILE.md) and [baseline review](docs/REUSE.md).

## Run the offline demo

Requires an installed .NET10 SDK (this source pins10.0.303); no application
packages, secret, account, network feed or paid service. From the repository root:

```powershell
. ./scripts/env.ps1
dotnet run --project src/QuoteIntegrity.Cli -c Release -- demo artifacts/my-demo
```

Use a **new output directory**. The demo deliberately contains1 accepted and2
quarantined rows; exit0 means these expected results matched. Open
`artifacts/my-demo/report.html` and inspect `report.json`. A result is complete
only with both files and no `INCOMPLETE` marker.

```powershell
dotnet run --project src/QuoteIntegrity.Cli -c Release -- validate fixtures/clean.json artifacts/clean-run
dotnet run --project tests/QuoteIntegrity.Checks -c Release -- fixtures/literal-corpus.json
```

For `validate`: exit0=`ADMIT_PROFILE`, exit2=completed `HOLD`/`REJECT`,
exit1=incomplete/malformed/usage/I/O. Unknown data-use rights produce HOLD;
mechanically valid rows cannot fill missing declared coverage. The input bytes
are bound by SHA256; row ordinal/id, exact scaled prices and reasons appear in
both reports. Midpoint5.105 USD is retained without rounding; it is not a fill.

Status: local C# core/CLI/report and original synthetic fixtures implemented.
P1 has39 literal cases and8 framing controls. Published Windows apphost checks
are separate from model validation and hosted CI. No hosted CI, downloadable
executable release or independent adoption is claimed. See
[P1 evidence](docs/P1-EVIDENCE.md) and [P2 evidence](docs/P2-EVIDENCE.md).

The second explicit integer-cents adapter is implemented. Report-v2 records its
source schema, and a local snapshot binds input/report/profile/code digests.
Full shared CMP-04/05 integration remains deferred; see
[P3 scope and migration](docs/P3-STATUS.md). Local snapshots do not claim shared
schema conformance or data admission authority.

Original public synthetic material is MIT licensed and created with AI
assistance. No real market/account data or private implementation is included.
This checks a declared profile; it does not authenticate data, prove legal
rights, price an option or guarantee execution.
