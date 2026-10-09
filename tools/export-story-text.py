"""Export the complete currently shipped story prose for editorial review."""
import json
from pathlib import Path

root = Path(__file__).resolve().parents[1]
source = root / 'game/unity/Assets/Game/Resources/Story/plan10-shangrila-story-content.json'
content = json.loads(source.read_text(encoding='utf-8'))
output = root / 'docs/story-text'
output.mkdir(parents=True, exist_ok=True)
chapters = ['物語本文（現在のゲーム収録分）', '編集用書き出し。変更はゲームに自動反映されません。', '']
for chapter in content['chapters']:
    chapters += ['=' * 60, chapter['title'], 'ID: ' + chapter['id'], '', '【導入】', chapter['introduction'], '']
    if chapter.get('pages'):
        chapters += [f'【{i+1}ページ】\n{page}' for i, page in enumerate(chapter['pages'])]
        chapters += ['', '収録詩ID：' + '、'.join(poem['id'] for poem in chapter['poems']), '']
        continue
    for index, poem in enumerate(chapter['poems'], 1):
        chapters += [f'【詩 {index}】 ' + poem['text'], 'ID: ' + poem['id'], poem['body'], '']
    chapters += ['【結び】', chapter['conclusion'], '']
events = ['回想本文（現在のゲーム収録分）', '編集用書き出し。変更はゲームに自動反映されません。', '']
for event in content['events']:
    events += ['=' * 60, event['title'], 'ID: ' + event['id'], '人物ID: ' + event['ownerId'], '', *event['paragraphs'], '']
for name, lines in [('物語本文.txt', chapters), ('回想本文.txt', events)]:
    (output / name).write_text('\n'.join(lines).rstrip() + '\n', encoding='utf-8-sig')
print(f"STORY_TEXT_EXPORT_PASS {len(content['chapters'])} chapters / {len(content['events'])} recollections")

# Individual exports also come from the shipped catalog, not an older script snapshot.
names = {
    'Rの物語と交流.txt': ['r'],
    'アナイアレイターと聖夜の物語.txt': ['annihilator', 'annihilator-holy'],
    'シェルの物語.txt': ['shell'], 'オリフラムの物語.txt': ['oriflamme'],
    'ナイトホークの物語.txt': ['nighthawk'], 'スレイヤー水着の物語.txt': ['slayer-swim'],
    'アルケインの物語.txt': ['arcane'], 'アルケイン学園の物語.txt': ['arcane-academy'],
    'シャングリラの物語.txt': ['shangrila'],
}
for name, keys in names.items():
    owners = {'heroine.' + key for key in keys}
    lines = []
    for chapter in content['chapters']:
        if chapter['ownerId'] in owners:
            lines += [chapter['title'], 'ID: ' + chapter['id']]
            lines += [f'【{i+1}ページ】\n{page}' for i, page in enumerate(chapter.get('pages', []))]
    for event in content['events']:
        if event['ownerId'] in owners:
            lines += [event['title'], 'ID: ' + event['id'], *event['paragraphs']]
    (output / name).write_text('\n\n'.join(lines) + '\n', encoding='utf-8')
