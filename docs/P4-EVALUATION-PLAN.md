# P4: AI triage gate — evaluation deferred

Numeric validity and admission remain entirely deterministic. The current
rules already emit exact reason codes. No model has been evaluated, and no AI
advantage, accuracy, savings or cost result is claimed.

An eligible experiment must first identify an additional user problem, such as
summarising a multi-row issue pattern, beyond restating one reason code. Before
any run, freeze a separately authored held-out corpus with human-reviewed
issue-family labels, a clean/no-issue control cohort, the dataset digest,
prompt/template, runner/model version, split, fixed budget and stopping rule.
The public39-case development corpus has already been used for development
and is not a new blind holdout.

Baseline: a deterministic reason-to-template explanation with all original
reason codes preserved. The model may supply descriptive text only. It cannot
change row/dataset decisions, repair data, invent units, or establish rights.
Compare both methods on the same frozen data and scope:

- False-alarm rate: clean examples incorrectly called problematic / all clean
  examples; also report counts and undefined denominators explicitly.
- Missed-issue rate: labelled issues absent from the explanation / all labelled
  issues; preserve the issue-family breakdown and unsupported claims.
- Actual requests, prompt/output tokens, elapsed time and documented financial
  cost if any. Absent billing/pricing evidence is unknown, not zero cost.
- Human explanation usefulness requires an independent review protocol; a
  grader that saw the labels is not a blind reviewer.

Current outcome: **DEFERRED**. There is no selected executable offline model
runner/pinned model, separately authored blind labels, or actual recorded model
run/cost evidence in this project. No paid provider, credentials, additional
agent or model-setting change is introduced. This dependency does not block
the numeric/reference and bounded-input work in P5. A future negative result is
a valid reason to stop the AI feature; adding it is not required for the kit to
explain deterministic failures.
