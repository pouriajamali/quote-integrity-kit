using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Json;
using QuoteIntegrity;

if (args is ["--version"]) { Console.WriteLine("Quote Integrity Kit 0.1.0; profile synthetic-european-call-v1"); return 0; }
try
{
    byte[] bytes;
    string output;
    var demo = false;
    if (args is ["validate", var input, var destination])
    {
        bytes = ReadBounded(input);
        output = destination;
    }
    else if (args is ["demo", var demoOutput])
    {
        demo = true;
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("demo.json")!;
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        bytes = memory.ToArray();
        output = demoOutput;
    }
    else { Console.Error.WriteLine("Usage: quote-integrity validate <snapshot.json> <new-output-dir> | demo <new-output-dir> | --version"); return 1; }

    var report = Evaluator.Assess(bytes); // A malformed input creates no report directory.
    if (demo && (report.Decision != "REJECT" || report.Accepted != 1 || report.Quarantined != 2))
        throw new InputException("DEMO_EXPECTATION_FAILED");
    if (File.Exists(output) || Directory.Exists(output)) throw new InputException("OUTPUT_EXISTS");
    Directory.CreateDirectory(output);
    var pending = Path.Combine(output, "INCOMPLETE");
    WriteNew(pending, "An output is complete only when report.json exists, with no INCOMPLETE marker.\n");
    var options = new JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    var json = JsonSerializer.Serialize(report, options) + "\n";
    var html = Html(report);
    if (Encoding.UTF8.GetByteCount(json) > 4 * 1024 * 1024 || Encoding.UTF8.GetByteCount(html) > 4 * 1024 * 1024)
        throw new InputException("REPORT_SIZE");
    WriteNew(Path.Combine(output, "report.html"), html);
    WriteNew(Path.Combine(output, "report.pending.json"), json);
    File.Move(Path.Combine(output, "report.pending.json"), Path.Combine(output, "report.json"));
    File.Delete(pending);
    Console.WriteLine($"{report.Decision}: {report.Accepted} accepted, {report.Quarantined} quarantined; input SHA256 {report.InputSha256}");
    if (demo) Console.WriteLine("Demo expected REJECT/1 accepted/2 quarantined matched. Exit0 means the demo matched expectations.");
    return demo || report.Decision == "ADMIT_PROFILE" ? 0 : 2;
}
catch (InputException e) { Console.Error.WriteLine("Incomplete: " + e.Code); return 1; }
catch (Exception e) when (e is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
{
    Console.Error.WriteLine("Incomplete: IO_OR_PATH_FAILURE");
    return 1;
}

static byte[] ReadBounded(string input)
{
    using var stream = new FileStream(input, FileMode.Open, FileAccess.Read, FileShare.Read);
    var buffer = new byte[Evaluator.MaxInputBytes + 1];
    var count = 0;
    while (count < buffer.Length)
    {
        var n = stream.Read(buffer, count, buffer.Length - count);
        if (n == 0) break;
        count += n;
    }
    if (count > Evaluator.MaxInputBytes) throw new InputException("INPUT_SIZE");
    return buffer[..count];
}

static void WriteNew(string path, string text)
{
    using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
    var bytes = new UTF8Encoding(false, true).GetBytes(text);
    stream.Write(bytes);
}

static string Html(Assessment r)
{
    static string H(string? value) => WebUtility.HtmlEncode(value ?? "unknown");
    var s = new StringBuilder("<!doctype html><html lang=\"en\"><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width,initial-scale=1\"><title>Quote Integrity Report</title><style>body{font:16px system-ui;max-width:1100px;margin:2rem auto;padding:0 1rem;color:#182433}h1{margin-bottom:.3rem}code{overflow-wrap:anywhere}table{border-collapse:collapse;width:100%;font-size:14px}th,td{text-align:left;border-bottom:1px solid #ccc;padding:.65rem;vertical-align:top}th{background:#edf3f8}.scroll{overflow-x:auto}.warn{border-left:4px solid #a65b00;padding:1rem;background:#fff5df}dt{font-weight:bold}dd{margin:0 0 1rem}footer{font-size:13px;color:#526175}</style><main><h1>Quote Integrity Report</h1>");
    s.Append($"<p><strong>{H(r.Decision)}</strong> — {r.Accepted} accepted, {r.Quarantined} quarantined.</p>");
    s.Append($"<p>Profile: <code>{H(r.Profile)}</code><br>Declared asOf: {H(r.AsOf)}<br>Source kind: {H(r.SourceKind)}<br>Input SHA256: <code>{H(r.InputSha256)}</code></p>");
    s.Append("<p class=\"warn\">Displayed bid/ask, mark, arithmetic midpoint and actual fill have different meanings. This report evaluates declared context and a fictional profile. It does not authenticate data, prove legal rights, validate a pricing model or promise execution.</p>");
    s.Append("<h2>Separate dataset checks</h2><dl>");
    foreach (var (label, value) in new[] { ("Row quality", r.RowQuality), ("Declared-set coverage", r.Coverage), ("Freshness at declared asOf", r.Freshness), ("Data-use declaration", r.UseDeclaration) })
        s.Append($"<dt>{label}</dt><dd>{H(value)}</dd>");
    s.Append($"</dl><p>Coverage reasons: {H(string.Join(", ", r.CoverageReasons))}</p><h2>Row decisions</h2><div class=\"scroll\"><table><thead><tr><th>Ordinal / id</th><th>Decision / reasons</th><th>Bid / ask (cents per unit)</th><th>Mark (cents per unit)</th><th>Arithmetic midpoint (USD per unit)</th><th>Bid / ask contract premium (cents)</th></tr></thead><tbody>");
    foreach (var row in r.Rows)
        s.Append($"<tr><td>{row.Ordinal} / {H(row.Id)}</td><td>{H(row.Decision)}<br>{H(string.Join(", ", row.Reasons))}</td><td>{H(row.BidUnits)} / {H(row.AskUnits)}</td><td>{H(row.MarkUnits)}</td><td>{H(row.Midpoint)}</td><td>{H(row.BidContractCents)} / {H(row.AskContractCents)}</td></tr>");
    s.Append("</tbody></table></div><footer><p>Accepted rows alone satisfy declared coverage; external completeness remains unproven. Row ordinals refer to the captured input bytes bound by SHA256. Outputs may contain user values; share only invented or licensed examples.</p><p>Complete output requires report.json, report.html and no INCOMPLETE marker. Missing/partial reports are not admission.</p></footer></main></html>\n");
    return s.ToString();
}
