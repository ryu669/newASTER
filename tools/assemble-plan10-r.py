"""Compose the R expansion without changing the preserved initial-five resources."""
import copy
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
RES = ROOT / "game/unity/Assets/Game/Resources"
def read(path):
    return json.loads(path.read_text(encoding="utf-8-sig"))
def write(path, data):
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(data, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")

combat = read(RES / "Combat/battle-formal.json")
r = copy.deepcopy(combat["heroines"][0])
def replace_owner(value):
    if isinstance(value, str): return value.replace("heroine.slayer", "heroine.r")
    if isinstance(value, list): return [replace_owner(v) for v in value]
    if isinstance(value, dict): return {k: replace_owner(v) for k, v in value.items()}
    return value
r = replace_owner(r)
r.update(name="R", jobId="job.artist", hpBp=9500, attackBp=10500, defenseBp=10000,
         speedBp=10000, traitHpPercent=0, traitAttackPercent=5)
combat["heroines"].append(r)
combat["jobs"].append(dict(id="job.artist", resourceName="歌唱ゲージ", resourceMax=10,
    initialResource=0, gainAtReady=2, gainOnAttack=0, gainOnHit=0,
    hp=320, attack=42, defense=130, magicDefense=200, speed=115, criticalBp=1200))
for slot, name, power, target, recovery in [
    (1, "星音のプリズム", 1.4, "target.selected-enemy", 75),
    (2, "大空のハーモニー", 0, "target.all-living-allies", 75),
    (3, "未来へ響く翼", 2.4, "target.all-enemies", 150),
]:
    s = copy.deepcopy(combat["skills"][0])
    s.update(id=f"heroine.r.{slot}", sourceSkillId=f"heroine.r.{slot}", ownerId="heroine.r",
        name=name, sourceFile="001戦闘_R.mkv", sourceSecond=15,
        powerScale=power, targetRuleId=target, recoveryPercent=recovery,
        conditions=[], criticalBonusBp=1500 if slot == 3 else 0,
        damageType="magic" if slot != 2 else None, attributes=["ビーム", "光"] if slot != 2 else [],
        ignoreDefenseBp=10000 if slot != 2 else 0, damageCap=10000 if slot != 2 else 0,
        chainEligible=slot != 2, effectRuleId="effect.allies-buff" if slot == 2 else "effect.damage",
        selfEffects=[dict(kind="attack", percent=20, turns=5), dict(kind="critical", percent=10, turns=5)] if slot == 2 else [])
    combat["skills"].append(s)
chain = replace_owner(copy.deepcopy(combat["chainActions"][0]))
chain.update(powerScale=.65, damageType="magic", attributes=["光"], ignoreDefenseBp=0)
combat["chainActions"].append(chain)
combat["contentReferences"] += [replace_owner(v) for v in combat["contentReferences"] if v["ownerId"] == "heroine.slayer"]
for ref in combat["contentReferences"]:
    if ref["ownerId"] == "heroine.r": ref["status"] = "implemented"
write(RES / "Combat/battle-plan10.json", combat)

base = read(RES / "Story/plan9-story-content.json")
extra = read(ROOT / "docs/production/plan10-r-story-content.json")
base["chapters"] += extra["chapters"]
base["events"] += extra["events"]
write(RES / "Story/plan10-story-content.json", base)
write(RES / "Illustrations/r-battle-binding.json", dict(heroineId="heroine.r", placeholder=False,
    fullCanvas=True, resourcePath="Illustrations/r-standing-candidate-v1",
    attackResourcePath="Illustrations/r-attack-candidate-v1", hitResourcePath="Illustrations/r-hit-candidate-v1",
    cutinResourcePath="Illustrations/r-cutin-candidate-v1"))
print("Composed six heroines, 18 skills, six chains, 63 chapters, 468 poems and 30 events.")
