"""Check all repository documentation links, including preserved historical reports."""
import re,subprocess
from pathlib import Path
from urllib.parse import unquote
R=Path(__file__).resolve().parents[1]
paths=['README.md','game/README.md']+[str(p.relative_to(R)) for p in (R/'docs').rglob('*.md')]
missing=[];checked=0
for rel in paths:
    p=R/rel
    if p.suffix!='.md':continue
    text=p.read_text(encoding='utf-8-sig')
    for target in re.findall(r'\]\(([^)]+)\)',text):
        target=target.strip('<>').split('#')[0]
        if not target or re.match(r'\w+://',target) or target.startswith('/') or target.startswith('mailto:'):continue
        checked+=1
        if not (p.parent/unquote(target)).exists():missing.append((rel,target))
assert not missing,missing
print(f'DOC_LINKS_PASS {checked} local links')
