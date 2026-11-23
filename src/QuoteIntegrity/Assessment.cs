using System.Globalization;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace QuoteIntegrity;

public sealed class InputException(string code) : Exception(code)
{
    public string Code { get; } = code;
}

public sealed record QuoteDecision(int Ordinal, string? Id, string Decision, string[] Reasons,
    string? BidUnits, string? AskUnits, string? MarkUnits, string? Midpoint,
    string? BidContractCents, string? AskContractCents);

public sealed record Assessment(string Schema, string Profile, string SourceSchema, string InputSha256,
    string SourceKind, string AsOf, string Decision, string RowQuality, string Coverage,
    string Freshness, string UseDeclaration, int Accepted, int Quarantined,
    string[] CoverageReasons, QuoteDecision[] Rows, string[] Limits);

public static class Evaluator
{
    public const string Profile = "synthetic-european-call-v1";
    public const int MaxInputBytes = 1024 * 1024;
    private static readonly DateTimeOffset Expiry = DateTimeOffset.Parse("2026-11-30T16:00:00Z", CultureInfo.InvariantCulture);
    private static readonly Regex IdPattern = new("\\A[A-Za-z0-9][A-Za-z0-9_-]{0,63}\\z", RegexOptions.CultureInvariant);
    private static readonly Regex PricePattern = new("\\A-?[0-9]+(?:\\.[0-9]{1,2})?\\z", RegexOptions.CultureInvariant);
    private static readonly string[] EnvelopeFields = ["schema", "profile", "sourceKind", "dataUse", "asOf", "maxAgeSeconds", "expectedIds", "quotes"];
    private static readonly string[] RowFields = ["id", "instrument", "venue", "currency", "eventTime", "observedAt", "bid", "ask", "mark"];

    public static Assessment Assess(byte[] bytes)
    {
        if (bytes.Length is 0 or > MaxInputBytes) throw new InputException("INPUT_SIZE");
        try { _ = new UTF8Encoding(false, true).GetString(bytes); }
        catch (DecoderFallbackException) { throw new InputException("UTF8_INVALID"); }
        JsonDocument doc;
        try { doc = JsonDocument.Parse(bytes, new JsonDocumentOptions { MaxDepth = 8 }); }
        catch (JsonException) { throw new InputException("JSON_INVALID"); }
        using (doc)
        {
            var root = doc.RootElement;
            CheckDuplicateProperties(root);
            ExactObject(root, EnvelopeFields);
            var sourceSchema = RequiredString(root, "schema");
            if (sourceSchema is not ("quote-integrity-v1" or "quote-integrity-scaled-v1")) throw new InputException("SCHEMA_UNSUPPORTED");
            var scaled = sourceSchema == "quote-integrity-scaled-v1";
            if (RequiredString(root, "profile") != Profile) throw new InputException("PROFILE_UNSUPPORTED");
            var source = RequiredString(root, "sourceKind");
            if (source is not ("ORIGINAL_SYNTHETIC" or "USER_SUPPLIED_UNVERIFIED")) throw new InputException("SOURCE_KIND");
            var use = RequiredString(root, "dataUse");
            if (use is not ("allow" or "deny" or "unknown")) throw new InputException("DATA_USE");
            var asOfText = RequiredString(root, "asOf");
            if (!Time(asOfText, out var asOf)) throw new InputException("ASOF_INVALID");
            var ageElement = root.GetProperty("maxAgeSeconds");
            if (ageElement.ValueKind != JsonValueKind.Number || !Regex.IsMatch(ageElement.GetRawText(), "\\A[0-9]+\\z") ||
                !ageElement.TryGetInt32(out var maxAge) || maxAge is < 0 or > 604800) throw new InputException("AGE_THRESHOLD");
            var expectedArray = root.GetProperty("expectedIds");
            if (expectedArray.ValueKind != JsonValueKind.Array || expectedArray.GetArrayLength() is < 1 or > 1000) throw new InputException("EXPECTED_IDS");
            var expected = new HashSet<string>(StringComparer.Ordinal);
            foreach (var item in expectedArray.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.String || !IdPattern.IsMatch(item.GetString()!) || !expected.Add(item.GetString()!))
                    throw new InputException("EXPECTED_IDS");
            }
            var inputRows = root.GetProperty("quotes");
            if (inputRows.ValueKind != JsonValueKind.Array || inputRows.GetArrayLength() is < 1 or > 1000) throw new InputException("ROW_COUNT");
            var rows = inputRows.EnumerateArray().Select((r, i) => Row(r, i + 1, asOf, maxAge, scaled)).ToArray();
            var coverageReasons = new SortedSet<string>(StringComparer.Ordinal);
            var allIds = rows.Select(r => r.Id).Where(x => x is not null).ToArray();
            if (allIds.Distinct(StringComparer.Ordinal).Count() != allIds.Length) coverageReasons.Add("DUPLICATE_ID");
            if (allIds.Any(id => !expected.Contains(id!))) coverageReasons.Add("UNEXPECTED_ID");
            // Only accepted rows count toward declared coverage: quarantined data cannot fill a gap.
            var acceptedIds = rows.Where(r => r.Decision == "ACCEPT").Select(r => r.Id!).ToHashSet(StringComparer.Ordinal);
            if (!expected.IsSubsetOf(acceptedIds)) coverageReasons.Add("MISSING_ACCEPTED_ID");
            var quality = rows.All(r => r.Decision == "ACCEPT") ? "PASS" : "FAIL";
            var coverage = coverageReasons.Count == 0 ? "PASS" : "FAIL";
            var freshness = rows.Any(r => r.Reasons.Any(IsTemporalReason)) ? "FAIL" : acceptedIds.Count > 0 ? "PASS" : "UNKNOWN";
            var rights = use == "allow" ? "DECLARED_ALLOW" : use == "deny" ? "DECLARED_DENY" : "UNKNOWN";
            var decision = quality == "FAIL" || coverage == "FAIL" || freshness == "FAIL" || use == "deny" ? "REJECT" : use == "unknown" ? "HOLD" : "ADMIT_PROFILE";
            return new("quote-integrity-report-v2", Profile, sourceSchema, Convert.ToHexStringLower(SHA256.HashData(bytes)), source, asOfText,
                decision, quality, coverage, freshness, rights, rows.Count(r => r.Decision == "ACCEPT"), rows.Count(r => r.Decision != "ACCEPT"),
                coverageReasons.ToArray(), rows,
                ["DECLARED_CONTEXT_ONLY", "DECLARED_SET_COVERAGE_ONLY", "SOURCE_AUTHENTICITY_NOT_PROVEN", "LEGAL_RIGHTS_NOT_PROVEN", "FILL_NOT_PROVEN", "NO_PRICING_MODEL_VALIDATION"]);
        }
    }

    public static long ParsePrice(string text)
    {
        if (text.Length is 0 or > 32 || !PricePattern.IsMatch(text)) throw new InputException("PRICE_FORMAT");
        var negative = text.StartsWith('-');
        var parts = (negative ? text[1..] : text).Split('.');
        var digits = parts[0] + (parts.Length == 1 ? "00" : parts[1].PadRight(2, '0'));
        var n = BigInteger.Parse(digits, CultureInfo.InvariantCulture) * (negative ? -1 : 1);
        if (n < long.MinValue || n > long.MaxValue) throw new InputException("PRICE_RANGE");
        return (long)n;
    }

    public static long ParseScaled(string text)
    {
        if (text.Length is 0 or > 32 || !Regex.IsMatch(text, "\\A-?[0-9]+\\z", RegexOptions.CultureInvariant)) throw new InputException("PRICE_FORMAT");
        if (!long.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var value)) throw new InputException("PRICE_RANGE");
        return value;
    }

    private static QuoteDecision Row(JsonElement row, int ordinal, DateTimeOffset asOf, int maxAge, bool scaled)
    {
        var reasons = new SortedSet<string>(StringComparer.Ordinal);
        if (row.ValueKind != JsonValueKind.Object) return new(ordinal, null, "QUARANTINE", ["ROW_OBJECT"], null, null, null, null, null, null);
        var names = row.EnumerateObject().Select(x => x.Name).ToHashSet(StringComparer.Ordinal);
        var fields = scaled ? RowFields.Select(x => x is "bid" or "ask" or "mark" ? x + "Units" : x).ToArray() : RowFields;
        if (names.Except(fields).Any()) reasons.Add("UNKNOWN_FIELD");
        if (fields.Except(names).Any()) reasons.Add("MISSING_FIELD");
        string? Field(string name)
        {
            if (!row.TryGetProperty(name, out var element) || element.ValueKind == JsonValueKind.Null) return null;
            if (element.ValueKind != JsonValueKind.String) { reasons.Add("FIELD_TYPE"); return null; }
            return element.GetString();
        }
        var id = Field("id");
        if (id is null || !IdPattern.IsMatch(id)) { reasons.Add("ID_INVALID"); id = null; }
        if (Field("instrument") != "SYNTH-20261130-C-100") reasons.Add("INSTRUMENT_MISMATCH");
        if (Field("venue") != "FICTIONAL") reasons.Add("VENUE_MISMATCH");
        if (Field("currency") != "USD") reasons.Add("CURRENCY_MISMATCH");
        var eventValid = Time(Field("eventTime"), out var eventTime);
        var observedValid = Time(Field("observedAt"), out var observed);
        if (!eventValid || !observedValid) reasons.Add("TIMESTAMP_INVALID");
        if (asOf >= Expiry || (eventValid && eventTime >= Expiry)) reasons.Add("EXPIRED");
        if (eventValid && observedValid && eventTime > observed) reasons.Add("EVENT_AFTER_OBSERVATION");
        if (observedValid && observed > asOf) reasons.Add("NOT_OBSERVED_AT_CUTOFF");
        if (eventValid && eventTime > asOf) reasons.Add("FUTURE_EVENT");
        if (eventValid && (asOf - eventTime).TotalSeconds > maxAge) reasons.Add("STALE");
        long? Price(string name, bool required)
        {
            var value = Field(scaled ? name + "Units" : name);
            if (value is null) { if (required) reasons.Add("MISSING_" + name.ToUpperInvariant()); return null; }
            try
            {
                var price = scaled ? ParseScaled(value) : ParsePrice(value);
                if (price < 0) { reasons.Add("NEGATIVE_" + name.ToUpperInvariant()); return null; }
                if (required)
                {
                    try { _ = checked(price * 100); }
                    catch (OverflowException) { reasons.Add("CONTRACT_PREMIUM_OVERFLOW"); return null; }
                }
                return price;
            }
            catch (InputException e) { reasons.Add(name.ToUpperInvariant() + "_" + e.Code); return null; }
        }
        var bid = Price("bid", true);
        var ask = Price("ask", true);
        var mark = Price("mark", false);
        if (bid.HasValue && ask.HasValue && bid > ask) reasons.Add("CROSSED");
        string? S(long? n) => n?.ToString(CultureInfo.InvariantCulture);
        var midpoint = bid.HasValue && ask.HasValue ? (((decimal)bid.Value + ask.Value) / 200m).ToString(CultureInfo.InvariantCulture) : null;
        return new(ordinal, id, reasons.Count == 0 ? "ACCEPT" : "QUARANTINE", reasons.ToArray(), S(bid), S(ask), S(mark), midpoint,
            bid.HasValue ? S(checked(bid.Value * 100)) : null, ask.HasValue ? S(checked(ask.Value * 100)) : null);
    }

    private static bool IsTemporalReason(string reason) => reason is "TIMESTAMP_INVALID" or "EXPIRED" or "EVENT_AFTER_OBSERVATION" or "NOT_OBSERVED_AT_CUTOFF" or "FUTURE_EVENT" or "STALE";

    private static bool Time(string? text, out DateTimeOffset value)
    {
        value = default;
        return text is { Length: 20 } && Regex.IsMatch(text, "\\A[0-9]{4}-[0-9]{2}-[0-9]{2}T[0-9]{2}:[0-9]{2}:[0-9]{2}Z\\z") &&
            DateTimeOffset.TryParseExact(text, "yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out value);
    }

    private static string RequiredString(JsonElement root, string field)
    {
        var element = root.GetProperty(field);
        if (element.ValueKind != JsonValueKind.String) throw new InputException("ENVELOPE_FIELD_TYPE");
        return element.GetString()!;
    }

    private static void ExactObject(JsonElement element, string[] fields)
    {
        if (element.ValueKind != JsonValueKind.Object) throw new InputException("ENVELOPE_OBJECT");
        var names = element.EnumerateObject().Select(x => x.Name).ToHashSet(StringComparer.Ordinal);
        if (!names.SetEquals(fields)) throw new InputException("ENVELOPE_FIELDS");
    }

    private static void CheckDuplicateProperties(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var p in element.EnumerateObject())
            {
                if (!names.Add(p.Name)) throw new InputException("DUPLICATE_PROPERTY");
                CheckDuplicateProperties(p.Value);
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
            foreach (var item in element.EnumerateArray()) CheckDuplicateProperties(item);
    }
}
