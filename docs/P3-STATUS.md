# P3: local adapter delivered, shared integration deferred

The second adapter is `quote-integrity-scaled-v1`. It accepts cents as integer
strings explicitly labelled bidUnits/askUnits/markUnits. It preserves the same
fictional contract, multiplier, timestamps, unknown values and refusal rules as
the decimal adapter. It does not represent a second real market/provider.

The report schema is now `quote-integrity-report-v2`, adding `sourceSchema` so
the adapter is explicit. Migration: consumers that only accept v1 must refuse
v2 or explicitly accept v2 plus the source schema; do not infer input units
from magnitude. Existing reason codes and exact row-value meanings remain.

`snapshot.json` uses **this project's** `quote-integrity-local-snapshot-v1`;
it binds exact input/report bytes, embedded profile bytes, CLI/core assemblies
and the declared recipe/context. It explicitly says CMP-04/05 are not integrated.
This is not an owner-defined CMP schema, legal-rights verification, signature
or proof that a caller-declared clock/publication statement is true.

At this checkpoint the shared owner has not supplied a verified pinned CMP-04
or CMP-05 consumer contract/fixture. Existing baseline temporal semantics are
not those extensions. Their owner remains G1. No schema or acceptance authority
is fabricated. **Full P3 is DEFERRED** pending an actual owner version, digest,
fixture and consumer contract tests. The local adapter/manifest portion can run
offline without a live G1 service; independent later work can continue.
