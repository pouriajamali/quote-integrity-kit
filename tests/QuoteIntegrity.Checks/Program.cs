using System.Text;
using System.Text.Json;
using QuoteIntegrity;

if (args.Length != 1) { Console.Error.WriteLine("Usage: checks <literal-corpus.json>"); return 1; }
using var corpus = JsonDocument.Parse(File.ReadAllBytes(args[0]));
var cases = corpus.RootElement.GetProperty("cases");
var passed = 0;
foreach (var c in cases.EnumerateArray())
{
    var name = c.GetProperty("name").GetString();
    var input = Encoding.UTF8.GetBytes(c.GetProperty("input").GetRawText());
    var expected = c.GetProperty("expected");
    try
    {
        var actual = Evaluator.Assess(input);
        Equal(expected.GetProperty("decision").GetString(), actual.Decision, name);
        Equal(expected.GetProperty("rowQuality").GetString(), actual.RowQuality, name);
        Equal(expected.GetProperty("coverage").GetString(), actual.Coverage, name);
        Equal(expected.GetProperty("freshness").GetString(), actual.Freshness, name);
        var rows = expected.GetProperty("rows").EnumerateArray().ToArray();
        if (actual.Rows.Length != rows.Length) throw new Exception($"{name}: row count");
        for (var i = 0; i < rows.Length; i++)
        {
            Equal(rows[i].GetProperty("decision").GetString(), actual.Rows[i].Decision, name);
            var reasons = rows[i].GetProperty("reasons").EnumerateArray().Select(x => x.GetString()).Order(StringComparer.Ordinal);
            if (!reasons.SequenceEqual(actual.Rows[i].Reasons)) throw new Exception($"{name}: expected reasons {string.Join(',', reasons)}; actual {string.Join(',', actual.Rows[i].Reasons)}");
            foreach (var field in new[] { "bidUnits", "askUnits", "midpoint", "bidContractCents", "askContractCents" })
            {
                if (!rows[i].TryGetProperty(field, out var e)) continue;
                var a = field switch { "bidUnits" => actual.Rows[i].BidUnits, "askUnits" => actual.Rows[i].AskUnits,
                    "midpoint" => actual.Rows[i].Midpoint, "bidContractCents" => actual.Rows[i].BidContractCents, _ => actual.Rows[i].AskContractCents };
                Equal(e.GetString(), a, name);
            }
        }
        passed++;
    }
    catch (InputException e) when (expected.TryGetProperty("fatal", out var fatal) && fatal.GetString() == e.Code) { passed++; }
}
Console.WriteLine($"Literal corpus: {passed}/{cases.GetArrayLength()} passed");
if (passed != cases.GetArrayLength()) return 1;
var clean = File.ReadAllText(Path.Combine(Path.GetDirectoryName(args[0])!, "clean.json"));
var strictCases = new (string Code, byte[] Bytes)[]
{
    ("JSON_INVALID", Encoding.UTF8.GetBytes("{")),
    ("JSON_INVALID", Encoding.UTF8.GetBytes(clean.Replace("\"schema\":", "\"schema\": /* comment */"))),
    ("DUPLICATE_PROPERTY", Encoding.UTF8.GetBytes(clean.Replace("\"dataUse\": \"allow\"", "\"dataUse\": \"deny\", \"dataUse\": \"allow\""))),
    ("DUPLICATE_PROPERTY", Encoding.UTF8.GetBytes(clean.Replace("\"bid\": \"5.10\"", "\"bid\": \"0\", \"bid\": \"5.10\""))),
    ("UTF8_INVALID", [0xff, 0xfe, 0xfd]),
    ("INPUT_SIZE", []),
    ("INPUT_SIZE", new byte[Evaluator.MaxInputBytes + 1]),
    ("ENVELOPE_OBJECT", Encoding.UTF8.GetBytes("[]"))
};
foreach (var c in strictCases)
{
    try { Evaluator.Assess(c.Bytes); throw new Exception($"Expected fatal {c.Code}"); }
    catch (InputException e) when (e.Code == c.Code) { }
}
Console.WriteLine($"Framing/duplicate-key/cap controls: {strictCases.Length}/{strictCases.Length} passed");
return 0;

static void Equal(string? expected, string? actual, string? name)
{
    if (expected != actual) throw new Exception($"{name}: expected {expected}, actual {actual}");
}
