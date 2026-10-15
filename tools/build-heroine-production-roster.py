"""Build the production candidate list from the two user-supplied local folders.

This inventories names and recordings only; it does not assign jobs or approve production.
Kyoshin names are manually observed in video frames, not inferred from anonymous filenames.
"""
from pathlib import Path
import collections
import json
import re

ROOT = Path(__file__).resolve().parents[1]
AA_ROOT = Path('D:/M02_gamemovie/ANGELICAASTER/天使戦闘')
KY_ROOT = Path('D:/M02_gamemovie/巨神と誓女/巨神と誓女_誓女データ')
OUT = ROOT / 'docs/production'
IMPLEMENTED = dict(zip(
    [1, 5, 9, 10, 12, 13, 15, 23, 24, 32, 48, 50, 55, 56, 66, 2, 3, 6, 11, 45, 16, 17, 18, 20, 21, 26],
    ['r', 'iconoclast', 'annihilator', 'annihilator-holy', 'arcane',
     'arcane-academy', 'undermine', 'echidna', 'excalipan', 'oriflamme',
     'shell', 'shangrila', 'slayer', 'slayer-swim', 'nighthawk',
     'iron-maiden', 'aeon', 'avalanche', 'anathema', 'shiva', 'invisible', 'viper', 'vindicator', 'weapon-master', 'april', 'estreya']))
VARIANTS = {
    4: ('アイオーン', '1周年'), 8: ('あざらし', '魔王'),
    10: ('アナイアレイター', '聖夜'), 13: ('アルケイン', '学園'),
    22: ('エイプリル', '花嫁'), 25: ('エクスカリパン', '水着'),
    38: ('キャンディストライク', '新春'), 44: ('ザクロ', '花嫁'),
    46: ('シヴァ', '1周年'), 49: ('ジャスミン', 'まいこ'),
    56: ('スレイヤー', '水着'), 61: ('スシエル', '新春'),
    63: ('デウス', 'ハロウィン'), 68: ('ナイトロ', 'チョコ'),
    70: ('ナハティガル', '水着'), 91: ('ホークウィンド', '水着'),
    95: ('ミラージュ', '水着'), 97: ('メサイア', '学園'),
    100: ('ライデン', 'ハロ（ファイル名表記）'), 108: ('ローズ', '水着'),
}


def dump(path, value):
    path.write_text(json.dumps(value, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')


def build():
    observations = json.loads((ROOT / 'docs/references/kyoshin-heroine-name-observations.json').read_text(encoding='utf-8'))
    observed = {r['sourceNumber']: r for r in observations['records']}
    character_index = json.loads((ROOT / 'docs/characters/index.json').read_text(encoding='utf-8'))
    runtime = json.loads((ROOT / character_index['combatSource']).read_text(encoding='utf-8-sig'))
    runtime_ids = {h['id'] for h in runtime['heroines']}
    records = []
    for path in sorted(AA_ROOT.glob('*.mkv')):
        m = re.fullmatch(r'(\d+)戦闘_(.+)\.mkv', path.name)
        if not m:
            raise ValueError(f'Unrecognized video filename: {path.name}')
        number, label = int(m[1]), m[2]
        extra = number == 2 and label == 'アイアンメイデン2'
        name, costume = VARIANTS.get(number, ('アイアンメイデン' if extra else label, '通常'))
        implemented = 'heroine.' + IMPLEMENTED[number] if number in IMPLEMENTED else None
        if implemented and implemented not in runtime_ids:
            raise ValueError(f'Missing implemented form: {implemented}')
        records.append(dict(source='aa', sourceNumber=number, filename=path.name,
            relativeSourcePath=path.name, bytes=path.stat().st_size,
            personName=name, displayName=name if costume == '通常' else f'{name}（{costume}）',
            formKey=f'aa-{number:03}', costumeLabel=costume,
            nameEvidence='source-filename', observedSecond=None,
            recordKind='additional-recording-candidate' if extra else 'form-reference',
            recordNote='末尾2は追加録画候補。同じ002枠に束ね、別衣装として増やさない。' if extra else None,
            implementationId=implemented, implementationStatus='実装済み' if implemented else '未実装'))
    ky_files = sorted(KY_ROOT.glob('*.mp4'))
    for path in ky_files:
        number = int(re.search(r'誓女(\d+)', path.name)[1])
        o = observed[number]
        name = o['displayName']
        costume = o['costumeHint'] or '録画の衣装'
        records.append(dict(source='kyoshin', sourceNumber=number, filename=path.name,
            relativeSourcePath=path.name, bytes=path.stat().st_size,
            personName=name, displayName=name, formKey=f'ky-{number:03}',
            costumeLabel=costume, costumeLabelStatus='正式衣装名未確認',
            nameEvidence='video-name-display', observedSecond=o['observedSecond'],
            recordKind='form-reference',
            recordNote=o['identityNote'], implementationId=None, implementationStatus='未実装',
            companionAudio=path.with_suffix('.wav').name if path.with_suffix('.wav').exists() else None))
    if len(ky_files) != len(observed) or {r['sourceNumber'] for r in records if r['source'] == 'kyoshin'} != set(observed):
        raise ValueError('Kyoshin observation coverage does not match videos.')
    people = []
    groups = collections.defaultdict(list)
    for row in records:
        groups[row['source'], row['personName']].append(row)
    for (source, name), rows in groups.items():
        form_keys = list(dict.fromkeys(r['formKey'] for r in rows))
        number = min(r['sourceNumber'] for r in rows)
        people.append(dict(planningId=f'{source}.person.{number:03}', source=source,
            name=name, sourceNumbers=sorted(set(r['sourceNumber'] for r in rows)),
            formKeys=form_keys, implementedFormIds=sorted(set(r['implementationId'] for r in rows if r['implementationId'])),
            groupingBasis='同じ作品内の名前表示／ファイル名と別衣装接尾辞。作品をまたいだ同一人物認定はしない。',
            productionLot=None, productionPriority=None, profileStatus='既存人物資料を参照' if any(r['implementationId'] for r in rows) else '未着手'))
    totals = {}
    for source in ['aa', 'kyoshin']:
        selected = [r for r in records if r['source'] == source]
        totals[source] = dict(videos=len(selected), people=sum(p['source'] == source for p in people),
            provisionalForms=len(set(r['formKey'] for r in selected)),
            implementedForms=len(set(r['implementationId'] for r in selected if r['implementationId'])))
    if {r['implementationId'] for r in records if r['implementationId']} != runtime_ids:
        raise ValueError('Current implemented heroine roster is not fully covered.')
    catalog = dict(schemaVersion=1, date='2026-10-11', status='候補台帳・量産順未指定',
        sourceRoots={'aa': str(AA_ROOT), 'kyoshin': str(KY_ROOT)}, totals=totals,
        countingPolicy='人物は作品別に管理。衣装は別形態。AA002の2本は同一形態の追加録画候補として暫定統合。巨神113/114はユーザー指定により別人物。巨神の衣装名は見た目の仮分類。',
        coverage='AA107本のファイル名と現行26形態の実装を照合。巨神126本は30秒の画面内名前表示を目視確認。人物設定・全音声・スキルの調査完了を意味しない。',
        people=people, videoRecords=records)
    dump(OUT / 'heroine-production-roster.json', catalog)
    lines = ['# ヒロイン量産候補リスト', '', '更新：2026-10-11。指定された2フォルダを全件収録。量産順・追加ロットは未指定。', '',
        '| 出典 | 人物候補 | 動画 | 暫定形態枠 | 実装済み形態 |', '| --- | ---: | ---: | ---: | ---: |']
    for source, label in [('aa', 'ANGELICA ASTER'), ('kyoshin', '巨神と誓女')]:
        t = totals[source]
        lines.append(f"| {label} | {t['people']} | {t['videos']} | {t['provisionalForms']} | {t['implementedForms']} |")
    lines += ['', '## 数え方・確認範囲', '',
        '- 人物と衣装違いを分離。同名・似た意匠でも作品間では統合しない。管理IDは候補台帳用であり、ゲーム実装IDを新規発行していない。',
        '- AAはファイル名が名前・衣装の根拠。現行26形態は実装IDと照合。「まいこ」「チョコ」「ハロ」は資料の表記を残し、原典の正式衣装名として補完しない。ジャスミンは「まいこ」の録画のみで、通常版を水増ししない。',
        '- 巨神は126本すべての30秒時点で画面左上の名前を確認。別衣装の呼び名は外見による仮分類で、正式衣装名は追加調査する。職業・特性・性格・口調・年齢・全音声・物語は今回確定していない。',
        '- AA002「アイアンメイデン2」は同じ番号の追加録画候補。巨神113は「レディX(少女)」、114は「レディX(大人)」としてユーザー指定により別人物へ分離。したがって形態数は確定人数ではない。',
        '- WAV126本はMP4の付随音声として扱い、人数・動画数に加算しない。動画がないAA番号014・019・051は欠番として記録し、人物名を推測して追加しない。',
        '- 量産の着手前に、人物資料、正式衣装名、ジョブ、特性・装備・祝福、必要20画像を形態単位で確定する。既存の完了済み素材は一括再審査しない。', '',
        '詳細：[機械処理用台帳](heroine-production-roster.json)、[巨神126本の名前観察](../references/kyoshin-heroine-name-observations.json)、[ヒロイン実装手順](heroine-addition-guide.md)。', '']
    for source, title in [('aa', 'ANGELICA ASTER'), ('kyoshin', '巨神と誓女')]:
        lines += [f'## {title}', '', '| 資料番号 | 人物 | 収録形態・資料 | 実装 |', '| --- | --- | --- | --- |']
        for person in [p for p in people if p['source'] == source]:
            selected = [r for r in records if r['source'] == source and r['personName'] == person['name']]
            details = []
            for key in person['formKeys']:
                rs = [r for r in selected if r['formKey'] == key]
                nums = '/'.join(dict.fromkeys(f"{r['sourceNumber']:03}" for r in rs))
                if len(rs) > 1 and len({r['sourceNumber'] for r in rs}) == 1:
                    nums += f'（{len(rs)}本）'
                label = rs[0]['costumeLabel']
                if source == 'kyoshin' and len(person['sourceNumbers']) == 1:
                    label = '録画形態（正式衣装名未確認）'
                if len(rs) > 1:
                    label += '／追加録画候補あり'
                details.append(f'{nums}：{label}')
            implemented = len(person['implementedFormIds'])
            state = '未実装' if not implemented else f"{implemented}/{len(person['formKeys'])}形態実装済み"
            if implemented and source == 'aa':
                details = [detail + ('［実装済み］' if any(r['implementationId'] and r['formKey'] == key for r in selected) else '［未実装］') for detail, key in zip(details, person['formKeys'])]
            lines.append('| ' + ', '.join(f'{n:03}' for n in person['sourceNumbers']) + f" | {person['name']} | " + '、'.join(details) + f' | {state} |')
        lines.append('')
    lines += ['## 元動画の対応', '', '各行の番号から、機械処理用台帳の `videoRecords` にある元ファイル名・サイズ・参照フォルダ・確認時刻へたどれる。AA002は2ファイルを保持し、巨神113/114はそれぞれ別人物・別形態として元ファイルへ対応する。原動画・音声・切り出し画像はリポジトリへ複製しない。', '']
    (OUT / 'heroine-production-roster.md').write_text('\n'.join(lines), encoding='utf-8')
    print(json.dumps(totals, ensure_ascii=False))


if __name__ == '__main__':
    build()
