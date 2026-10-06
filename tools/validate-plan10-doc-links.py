"""Check new/current documentation links without rewriting historical reports."""
import re,subprocess
from pathlib import Path
from urllib.parse import unquote
R=Path(__file__).resolve().parents[1]
paths=subprocess.check_output(['git','diff','HEAD','--name-only','--','README.md','docs','game/README.md'],cwd=R).decode().splitlines()
paths+=subprocess.check_output(['git','ls-files','--others','--exclude-standard','--','docs'],cwd=R).decode().splitlines()
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
