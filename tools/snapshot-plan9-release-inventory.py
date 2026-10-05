"""Inventory actual RC1 content and adopted resources; does not approve new art."""
import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
RESOURCES = ROOT / "game/unity/Assets/Game/Resources"


def read(path):
    return json.loads(path.read_text(encoding="utf-8-sig"))


def snapshot():
    home = read(ROOT / "tmp/plan9-home-catalog.json")
    story = read(RESOURCES / "Story/plan9-story-content.json")
    combat = read(RESOURCES / "Combat/battle-formal.json")
    adoption = read(ROOT / "docs/production/plan9-asset-adoption.json")
    assert home["status"] == "release"
    assert all(not a["placeholder"] for a in home["assets"])
    assert all(not g["unmade"] for g in home["gardens"])
    assets = {a["resourcePath"]: a for a in adoption["assets"]}
    assert all(a["resourcePath"] in assets for a in home["assets"])
    for asset in assets.values():
        path = ROOT / asset["file"]
        assert hashlib.sha256(path.read_bytes()).hexdigest() == asset["sha256"].lower(), str(path)
    chapters = [{"id": c["id"], "ownerId": c["ownerId"], "title": c["title"],
                 "poemIds": [p["id"] for p in c["poems"]]} for c in story["chapters"]]
    counts = {"heroines": len(combat["heroines"]), "implementedJobs": len(combat["jobs"]),
              "continuationJobs": 8, "colossi": len(home["colossusIds"]), "worlds": 7,
              "chapters": len(chapters), "poems": sum(len(c["poemIds"]) for c in chapters),
              "events": len(story["events"]), "gardens": len(home["gardens"]),
              "furniture": len(home["furniture"]), "adoptedImageAudioResources": len(assets)}
    assert tuple(counts[k] for k in ("heroines", "implementedJobs", "colossi", "chapters", "poems", "events", "gardens", "furniture")) == (5, 5, 15, 60, 450, 25, 9, 10)
    result = {"schemaVersion": 1, "generatedOn": "2026-10-05", "scope": "initial-five-rc1",
              "homeContentVersion": home["contentVersion"], "storyContentVersion": story["contentVersion"],
              "counts": counts, "heroines": [{"id": h["id"], "name": h["name"], "jobId": h["jobId"]} for h in combat["heroines"]],
              "colossusIds": home["colossusIds"], "chapters": chapters,
              "events": [{"id": e["id"], "ownerId": e["ownerId"], "title": e["title"], "cgResourcePath": e["cgResourcePath"]} for e in story["events"]],
              "gardens": home["gardens"], "furnitureIds": [f["id"] for f in home["furniture"]],
              "assetAcceptance": "plan9-asset-adoption.json", "futureRoster": "post-plan9-jobs.md"}
    output = ROOT / "docs/production/plan9-release-content-inventory.json"
    output.write_text(json.dumps(result, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print("PLAN9_RELEASE_INVENTORY_PASS", counts)


if __name__ == "__main__":
    snapshot()
