import json,runpy
from pathlib import Path
R=Path(__file__).resolve().parents[1];runpy.run_path(str(R/'tools/author-plan10-slayer-swim-story.py'))
res=R/'game/unity/Assets/Game/Resources/Story';story=json.loads((res/'plan10-nighthawk-story-content.json').read_text(encoding='utf8'));extra=json.loads((R/'docs/production/plan10-slayer-swim-story-content.json').read_text(encoding='utf8'))
story['chapters']+=extra['chapters'];story['events']+=extra['events'];(res/'plan10-slayer-swim-story-content.json').write_text(json.dumps(story,ensure_ascii=False,indent=2)+'\n',encoding='utf8')
