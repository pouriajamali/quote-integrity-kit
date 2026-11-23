# Quote Integrity Kit

Inspect an offline option quote snapshot and explain why each row is accepted
or quarantined before analysis.

First profile: an explicitly fictional European USD call with fixed precision,
expiry and multiplier. Bid, ask, mark, midpoint and fill retain separate meanings.
See [the exact profile](docs/PROFILE.md) and [baseline review](docs/REUSE.md).

Status: P0 contract defined. Runnable CLI, fixtures and reports are the next
delivery gates. No hosted CI, executable release or independent adoption is
claimed by this contract-only commit.

Original public synthetic material is MIT licensed and created with AI
assistance. No real market/account data or private implementation is included.
This checks a declared profile; it does not authenticate data, prove legal
rights, price an option or guarantee execution.
