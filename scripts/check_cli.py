"""Exercise the actual published CLI, including refusal and output consistency."""
import hashlib
import json
import subprocess
import sys
from pathlib import Path

root = Path(__file__).resolve().parents[1]
cli = Path(sys.argv[1]).resolve()
work = Path(sys.argv[2]).resolve()
work.mkdir(parents=True, exist_ok=False)
checks = 0

def run(args, code):
    global checks
    p = subprocess.run([str(cli), *map(str, args)], capture_output=True, text=True, timeout=20)
    assert p.returncode == code, (args, code, p.returncode, p.stdout, p.stderr)
    checks += 1
    return p

run(["--version"], 0)
for name, code, decision, accepted, quarantined in [
    ("clean", 0, "ADMIT_PROFILE", 1, 0),
    ("demo", 2, "REJECT", 1, 2),
]:
    target = work / name
    data = root / "fixtures" / (name + ".json")
    run(["validate", data, target], code)
    report = json.loads((target / "report.json").read_text(encoding="utf-8"))
    assert report["decision"] == decision and report["accepted"] == accepted and report["quarantined"] == quarantined
    assert report["inputSha256"] == hashlib.sha256(data.read_bytes()).hexdigest()
    assert not (target / "INCOMPLETE").exists()
    assert not (target / "report.pending.json").exists()
    html = (target / "report.html").read_text(encoding="utf-8")
    assert "Quote Integrity Report" in html and report["inputSha256"] in html and decision in html
    before = {p.name: hashlib.sha256(p.read_bytes()).hexdigest() for p in target.iterdir()}
    run(["validate", data, target], 1)
    assert before == {p.name: hashlib.sha256(p.read_bytes()).hexdigest() for p in target.iterdir()}
    checks += 5
run(["demo", work / "embedded-demo"], 0)
broken = work / "malformed.json"
broken.write_text('{"schema":', encoding="utf-8")
run(["validate", broken, work / "must-not-exist"], 1)
assert not (work / "must-not-exist").exists()
checks += 1
run(["validate", work / "absent.json", work / "absent-output"], 1)
run([], 1)
data = json.loads((root / "fixtures" / "clean.json").read_text(encoding="utf-8"))
data["dataUse"] = "unknown"
unknown = work / "unknown-rights.json"
unknown.write_text(json.dumps(data), encoding="utf-8")
run(["validate", unknown, work / "hold"], 2)
assert json.loads((work / "hold" / "report.json").read_text(encoding="utf-8"))["decision"] == "HOLD"
checks += 1
print(f"CLI acceptance: {checks} checks passed; actual published apphost; Windows local only")
