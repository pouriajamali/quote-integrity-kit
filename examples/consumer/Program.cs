using QuoteIntegrity;

if (args.Length != 1) { Console.Error.WriteLine("Usage: consumer <synthetic-snapshot.json>"); return 1; }
var r = Evaluator.Assess(File.ReadAllBytes(args[0]));
if (r.Schema != "quote-integrity-report-v2" || r.Profile != "synthetic-european-call-v1") return 1;
Console.WriteLine($"{r.Decision}; accepted={r.Accepted}; quarantined={r.Quarantined}; midpoint={r.Rows[0].Midpoint}; digest={r.InputSha256}");
// This example observes the library result. It performs no downstream action.
return r.Decision == "ADMIT_PROFILE" ? 0 : 2;
