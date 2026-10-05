"""Bind completed functional captures and explicit visual review to the packaged build."""
import argparse
import hashlib
import json
import re
from pathlib import Path
from PIL import Image

ROOT = Path(__file__).resolve().parents[1]


def read(path):
    return json.loads(path.read_text(encoding="utf-8-sig"))


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("results", nargs="+", type=Path)
    parser.add_argument("--reviewed", required=True, type=Path,
                        help="Explicit image paths inspected by the agent; never inferred from rendering success")
    args = parser.parse_args()
    package = read(ROOT / "docs/production/plan9-package-validation.json")
    reviewed = {str(Path(p).resolve()) for p in read(args.reviewed)}
    cases = []
    for result_path in args.results:
        result = read(result_path)
        assert result["assemblySha256"].lower() == package["assemblySha256"]
        assert result["normalSaveUnchanged"] and not result["performanceMeasured"]
        for capture in result["cases"]:
            image = Path(capture["image"])
            assert str(image.resolve()) in reviewed, f"Visual review missing: {image}"
            with Image.open(image) as pixels:
                assert pixels.size == (capture["height"] * 16 // 9, capture["height"])
                assert any(high > low for low, high in pixels.convert("RGB").getextrema()), f"Blank capture: {image}"
            capture = {**capture, "imageSha256": sha(image), "logSha256": sha(Path(capture["log"])),
                       "state": "visually-reviewed-by-agent", "humanPlaytest": False}
            cases.append(capture)
    identities = {(c["case"], c["height"]) for c in cases}
    assert len(cases) == len(identities) == 84
    assert {c["height"] for c in cases} == {720, 1080}
    build_log = ROOT / "tmp/plan9-rc1-accepted.log"
    build_text = build_log.read_text(encoding="utf-8-sig", errors="replace")
    assert re.search(r"PLAYABLE_BUILD_PASS 1950 assertions", build_text)
    for height in (720, 1080):
        audio_log = next(Path(c["log"]) for c in cases if c["case"] == "story-garden.audio" and c["height"] == height)
        assert "PLAN9_PRODUCTION_AUDIO_PASS" in audio_log.read_text(encoding="utf-8-sig", errors="replace")
    result = {"schemaVersion": 1, "date": "2026-10-05", "version": package["version"],
              "assemblySha256": package["assemblySha256"], "scope": "initial-five-distribution-candidate",
              "unityAssertions": 1950, "coreAssertions": 6988, "cases": cases,
              "normalSaveUnchanged": True, "performanceMeasured": False,
              "physicalInputCount": 5, "listeningSeconds": 0,
              "minimalInputEvidence": "plan9-minimal-input-validation.json",
              "minimalInputBuildNote": "Input smoke predates result-screen copy correction; ADV source unchanged. No repeated input.",
              "buildLogSha256": sha(build_log), "packageManifest": "plan9-package-validation.json"}
    (ROOT / "docs/production/plan9-final-runtime-validation.json").write_text(
        json.dumps(result, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print("PLAN9_FINAL_ACCEPTANCE_PASS images=84 assembly=" + package["assemblySha256"])


if __name__ == "__main__":
    main()
