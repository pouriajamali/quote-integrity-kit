"""Curate original synthetic inputs with literal contract-derived expectations.

This module does not import or run the product. Running it writes the public
fixture files; expected outcomes below are deliberately explicit.
"""
import copy
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
ROW = dict(id="q1", instrument="SYNTH-20261130-C-100", venue="FICTIONAL",
           currency="USD", eventTime="2026-11-20T11:59:30Z",
           observedAt="2026-11-20T11:59:31Z", bid="5.10", ask="5.11", mark="7.00")
BASE = dict(schema="quote-integrity-v1", profile="synthetic-european-call-v1",
            sourceKind="ORIGINAL_SYNTHETIC", dataUse="allow",
            asOf="2026-11-20T12:00:00Z", maxAgeSeconds=60,
            expectedIds=["q1"], quotes=[ROW])
cases = []


def add(name, changes=None, reasons=(), freshness=None, envelope=None,
        decision=None, values=None, fatal=None):
    data = copy.deepcopy(BASE)
    if changes:
        data["quotes"][0].update(changes)
    if envelope:
        data.update(envelope)
    if fatal:
        expected = dict(fatal=fatal)
    else:
        bad = bool(reasons)
        row = dict(decision="QUARANTINE" if bad else "ACCEPT", reasons=list(reasons))
        if values:
            row.update(values)
        expected = dict(decision=decision or ("REJECT" if bad else "ADMIT_PROFILE"),
                        rowQuality="FAIL" if bad else "PASS",
                        coverage="FAIL" if bad else "PASS",
                        freshness=freshness or ("UNKNOWN" if bad else "PASS"),
                        rows=[row])
    cases.append(dict(name=name, input=data, expected=expected))


add("odd-cent-midpoint-is-not-fill", values=dict(bidUnits="510", askUnits="511",
    midpoint="5.105", bidContractCents="51000", askContractCents="51100"))
add("null-mark-is-allowed", dict(mark=None))
add("zero-is-not-missing", dict(bid="0", ask="0", mark=None),
    values=dict(bidUnits="0", askUnits="0", midpoint="0", bidContractCents="0", askContractCents="0"))
add("stale", dict(eventTime="2026-11-20T11:58:59Z"), ["STALE"], "FAIL")
add("freshness-boundary-inclusive", dict(eventTime="2026-11-20T11:59:00Z"))
add("crossed", dict(bid="5.12"), ["CROSSED"])
add("locked-quote-is-allowed", dict(bid="5.11"))
add("missing-bid-null", dict(bid=None), ["MISSING_BID"])
add("missing-ask-null", dict(ask=None), ["MISSING_ASK"])
add("unknown-field", dict(unknownVendorFlag=True), ["UNKNOWN_FIELD"])
add("excess-precision", dict(bid="5.101"), ["BID_PRICE_FORMAT"])
add("negative-option-premium", dict(bid="-1.00"), ["NEGATIVE_BID"])
add("negative-mark", dict(mark="-1.00"), ["NEGATIVE_MARK"])
add("numeric-token-is-not-string", dict(bid=5.1), ["FIELD_TYPE", "MISSING_BID"])
add("not-observed-at-cutoff", dict(observedAt="2026-11-20T12:00:01Z"), ["NOT_OBSERVED_AT_CUTOFF"], "FAIL")
add("event-after-observation", dict(eventTime="2026-11-20T11:59:32Z"), ["EVENT_AFTER_OBSERVATION"], "FAIL")
add("future-event", dict(eventTime="2026-11-20T12:00:01Z", observedAt="2026-11-20T12:00:01Z"),
    ["FUTURE_EVENT", "NOT_OBSERVED_AT_CUTOFF"], "FAIL")
add("expiry-equality-refused", dict(eventTime="2026-11-30T16:00:00Z", observedAt="2026-11-30T16:00:00Z"),
    ["EXPIRED"], "FAIL", dict(asOf="2026-11-30T16:00:00Z"))
add("one-second-before-expiry", dict(eventTime="2026-11-30T15:59:59Z", observedAt="2026-11-30T15:59:59Z"),
    envelope=dict(asOf="2026-11-30T15:59:59Z"))
add("invalid-calendar-date", dict(eventTime="2026-02-30T12:00:00Z"), ["TIMESTAMP_INVALID"], "FAIL")
add("currency-mismatch", dict(currency="BTC"), ["CURRENCY_MISMATCH"])
add("instrument-mismatch", dict(instrument="SYNTH-PUT"), ["INSTRUMENT_MISMATCH"])
add("venue-mismatch", dict(venue="REAL_EXCHANGE"), ["VENUE_MISMATCH"])
add("price-int64-overflow", dict(bid="92233720368547758.08"), ["BID_PRICE_RANGE"])
add("contract-premium-overflow", dict(bid="922337203685477.59"), ["CONTRACT_PREMIUM_OVERFLOW"])
add("contract-premium-last-whole-cent", dict(bid="922337203685477.58", ask="922337203685477.58"),
    values=dict(bidUnits="92233720368547758", askUnits="92233720368547758", midpoint="922337203685477.58",
                bidContractCents="9223372036854775800", askContractCents="9223372036854775800"))
add("unknown-rights-hold", envelope=dict(dataUse="unknown"), decision="HOLD")
add("deny-rights-reject", envelope=dict(dataUse="deny"), decision="REJECT")
add("unsupported-schema", envelope=dict(schema="quote-integrity-v2"), fatal="SCHEMA_UNSUPPORTED")
add("unsupported-profile", envelope=dict(profile="a-real-venue"), fatal="PROFILE_UNSUPPORTED")
add("no-calendar-inference", envelope=dict(expectedIds=[]), fatal="EXPECTED_IDS")
add("unknown-envelope-field", envelope=dict(forceAdmit=True), fatal="ENVELOPE_FIELDS")
add("invalid-asof", envelope=dict(asOf="2026-11-20T12:00:00+00:00"), fatal="ASOF_INVALID")
add("fractional-age", envelope=dict(maxAgeSeconds=60.5), fatal="AGE_THRESHOLD")

# A valid row cannot turn an incomplete declared set into an admitted dataset.
add("missing-expected-id", envelope=dict(expectedIds=["q1", "q2"]), decision="REJECT")
cases[-1]["expected"]["coverage"] = "FAIL"
add("unexpected-id", envelope=dict(expectedIds=["q2"]), decision="REJECT")
cases[-1]["expected"]["coverage"] = "FAIL"
add("duplicate-id", envelope=dict(quotes=[copy.deepcopy(ROW), copy.deepcopy(ROW)]), decision="REJECT")
cases[-1]["expected"]["coverage"] = "FAIL"
cases[-1]["expected"]["rows"].append(copy.deepcopy(cases[-1]["expected"]["rows"][0]))

for name, transform, expected in [
    ("missing-field", lambda r: r.pop("mark"), ["MISSING_FIELD"]),
    ("not-an-object", lambda r: None, ["ROW_OBJECT"]),
]:
    add(name, reasons=expected)
    if name == "not-an-object":
        cases[-1]["input"]["quotes"] = [None]
    else:
        transform(cases[-1]["input"]["quotes"][0])

target = ROOT / "fixtures"
target.mkdir(exist_ok=True)
def write(name, data):
    (target / name).write_text(json.dumps(data, indent=2, ensure_ascii=True) + "\n", encoding="utf-8")
write("literal-corpus.json", dict(schema="quote-integrity-literal-oracle-v1", origin="ORIGINAL_SYNTHETIC", license="MIT", cases=cases))
write("clean.json", BASE)
demo = copy.deepcopy(BASE)
demo["quotes"] = [copy.deepcopy(ROW), dict(ROW, id="q2", bid="5.12"), dict(ROW, id="q3", eventTime="2026-11-20T11:58:59Z")]
demo["expectedIds"] = ["q1", "q2", "q3"]
write("demo.json", demo)
print(f"Curated {len(cases)} literal cases; wrote clean/demo synthetic examples")
