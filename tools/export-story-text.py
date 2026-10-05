"""Export the complete currently shipped story prose for editorial review."""
import json
from pathlib import Path

root = Path(__file__).resolve().parents[1]
source = root / 'game/unity/Assets/Game/Resources/Story/plan9-story-content.json'
content = json.loads(source.read_text(encoding='utf-8'))
output = root / 'docs/story-text'
output.mkdir(parents=True, exist_ok=True)
chapters = ['物語本文（現在のゲーム収録分）', '編集用書き出し。変更はゲームに自動反映されません。', '']
for chapter in content['chapters']:
    chapters += ['=' * 60, chapter['title'], 'ID: ' + chapter['id'], '', '【導入】', chapter['introduction'], '']
    for index, poem in enumerate(chapter['poems'], 1):
        chapters += [f'【詩 {index}】 ' + poem['text'], 'ID: ' + poem['id'], poem['body'], '']
    chapters += ['【結び】', chapter['conclusion'], '']
events = ['回想本文（現在のゲーム収録分）', '編集用書き出し。変更はゲームに自動反映されません。', '']
for event in content['events']:
    events += ['=' * 60, event['title'], 'ID: ' + event['id'], '人物ID: ' + event['ownerId'], '', *event['paragraphs'], '']
for name, lines in [('物語本文.txt', chapters), ('回想本文.txt', events)]:
    (output / name).write_text('\n'.join(lines).rstrip() + '\n', encoding='utf-8-sig')
print(f"STORY_TEXT_EXPORT_PASS {len(content['chapters'])} chapters / {len(content['events'])} recollections")
