# Contributing

Start with a small original synthetic fixture, a precise expected result and
the contract assumption it exercises. A profile proposal should state venue,
instrument, exercise/settlement, expiry clock/calendar, units, currency,
multiplier, precision and data-use rights. A software MIT license does not grant
rights to a vendor dataset. Do not attach real trading/account data or secrets.

Run the literal checks and published CLI acceptance from README/docs. Add a
correct control case alongside a failure. Do not derive expected results from
the implementation. Changing a wire schema or input semantics requires an
explicit version/migration and tests for affected consumers. Shared CMP schemas
remain under their owner's control. No live service is needed for this demo.

No issue, PR or external outreach is created automatically by the repository.
Hosted CI and binary-release claims require actual evidence; local tests alone
must retain their scope. Preserve negative results and unmeasured quantities.
