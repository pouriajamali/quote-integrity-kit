# P2: bounded CLI and traceable report

CLI `validate` captures at most1MiB of UTF8 JSON before creating a report. Bad
structure is incomplete; a known row fault is quarantined. Reports bind exact
captured bytes by SHA256 and each row by ordinal/id. Every row's sorted reason
codes, cents per unit, exact decimal midpoint and checked cents per contract are
inspectable. HTML encodes displayed data. Reports are capped at4MiB each.

Output must be new. Existing output is refused unchanged. `INCOMPLETE` is
written first; HTML and pending JSON precede the final JSON; the marker is
removed last. If a write fails, absence of a final report or presence of the
marker prevents interpreting a partial output as complete. This is not an
atomic filesystem/security or power-loss guarantee.

Reproduction on Windows, SDK10.0.303, no external packages:

```powershell
. ./scripts/env.ps1
dotnet restore src/QuoteIntegrity.Cli --locked-mode
dotnet publish src/QuoteIntegrity.Cli -c Release --no-restore --no-self-contained -o artifacts/publish
python -B scripts/check_cli.py artifacts/publish/quote-integrity.exe artifacts/cli-check-new
```

The22 CLI acceptance checks run actual apphost subprocesses, verify clean/
quarantine/unknown-rights exits and counts, input digest and HTML/JSON agreement,
embedded demo, missing/malformed input, usage, completion markers and refusal to
overwrite an existing output. Each child has a20-second timeout. These checks
do not use the evaluator to generate expected outcomes.

A local framework-dependent ZIP may also be extracted and these same checks
run against the extracted apphost from a path with spaces. It still requires an
installed compatible .NET runtime. Installed-runtime startup is not a clean-OS
or SDK-free claim. Published source and local artifacts are not a public binary
release; no hosted CI run is asserted. CMP-04/05 integration, a second adapter,
AI evaluation, broader numerical properties and independent adoption retain
their own later gates.
