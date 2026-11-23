# Delivery gates and evidence boundaries

| Phase | Observed state | Evidence and remaining gate |
| --- | --- | --- |
| P0 | VERIFIED in synthetic scope | Explicit one-contract profile, rights declaration, oracle design and public baseline contract/license review. |
| P1 | VERIFIED locally |39 literal expectations +8 framing controls, exact tolerances. |
| P2 | VERIFIED locally, NOT_RELEASED | Runnable CLI/HTML/JSON and22 published/extracted-apphost checks on Windows with installed runtime. No hosted CI/public binary release. |
| P3 | DEFERRED, local portion implemented | Integer adapter and local digest manifest tested; full CMP-04/05 contract/fixture integration awaits owner's pinned version. |
| P4 | DEFERRED | No independent blind holdout or pinned model run/cost evidence. Deterministic rules remain the authority. |
| P5 | VERIFIED for bounded numeric scope |1072 independent Decimal vectors,1000 quote arithmetic/equivalence controls and finite revision/cap checks. No model or speed benchmark. |
| P6 | DEFERRED, package/examples prepared | Local package/self-authored consumer can be checked; external use, support cost, hosted CI and real public binary delivery remain unverified. |

## Shared complements

CMP-04 and CMP-05 are consumed, not owned, by this kit. Their P0–P6 matrices
remain part of the plan: recipe/cutoff/input-code-config provenance and runner,
reproducibility, safe evaluation and independent recipes for CMP-04; separate
row/coverage/freshness/rights, quarantine, late revisions, drift/resource limits
and maintained policy profiles for CMP-05. Local analogues do not mark the
owner's phases complete or establish shared conformance. A versioned owner
fixture plus digest and consumer tests is required before integration passes.

## Package and consumption example

```powershell
. ./scripts/env.ps1
dotnet pack src/QuoteIntegrity -c Release --no-restore -o artifacts/packages
```

Copy `examples/consumer/` into a new folder outside the source tree. Restore
with `--source <absolute-path-to-artifacts/packages>` and run it on an original
synthetic snapshot. This validates a local NuGet consumer, not a public package
registry or independently authored external adoption. The Python example reads
report-v2 using Decimal and explicitly refuses an unsupported schema/profile.

P6 adoption evidence must distinguish project-authored examples from an outside
developer's reproduction. No such outside use or support-cost sample has been
observed. A commercial-data route also needs an actual licensed data scope and
buyer; no paid service or demand conclusion follows from these fixtures.

For any future executable release, record exact tested source and same-commit
CI, fresh download/startup, platform/runtime scope, hashes and live links. A
source commit, a local package or the repository's existence is not that gate.
