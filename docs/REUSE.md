# Baseline review and extension boundary

Read-only public review on 2026-11-23:

| Baseline | Pinned public commit | Reviewed surface | Decision |
| --- | --- | --- | --- |
| [Market Data Integrity Lab](https://github.com/pouriajamali/market-data-integrity-lab) | `94a3e6f5c58e1229245c589128c8dbd24f626633` | README, MIT grant, `src/Integrity/CsvContract.cs` blob `6ad8ed023f6c8a30f7c8e6a70c5d7c249c0c91aa` | Existing six-column OHLC CSV contract cannot represent a bid/ask option quote. Use a dedicated quote profile; no OHLC rule or copied implementation. |
| [Data Admission Gate](https://github.com/pouriajamali/data-admission-gate) | `f22412f75b6562b8054fe972c8a4e98f1864a299` | README, MIT grant, `docs/CONTRACT.md` blob `d313c92bc6a66d894220e3c844968b6b5fee7da4` | Preserve separation of row quality, declared coverage, freshness and asserted use. Its fixed OHLC profile is not a quote adapter. No runtime dependency or source extraction. |

This is a bounded contract review, not a build, release or CI audit of either
baseline. Neither baseline is changed. No private snapshots are copied. MIT
licenses permit public reuse with attribution; no baseline source has been
vendored. New exact decimal parsing belongs to the new quote contract and is
tested independently. Extract a shared package only after two real consumers
need the same semantics.

CMP-04 (evaluation provenance) and CMP-05 (dataset admission) belong to the
Point-in-Time Reference Lab owner. Initially consume versioned manifests and
fixtures offline. Do not invent an implemented G1 schema or require a running
service. Until the owner publishes an actual version/digest contract, consumer
integration is pending; this repository's local report is not CMP certification.

Local toolchain: installed .NET SDK10.0.303, Python3.13.1, Windows; no SDK install
or paid service. Baseline net11 binaries are not executed with this SDK. The
independent application targets net10.0 with no external application packages.
No GitHub Actions are launched in this initial path; baseline no-Actions/quota
instructions are preserved for baseline work. Local checks must not be labelled
hosted CI or a released download.
