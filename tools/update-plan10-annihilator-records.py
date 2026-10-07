"""Reconcile only the requested two forms into authoring records."""
import json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
def read(p):return json.loads(p.read_text(encoding='utf-8-sig'))
def write(p,d):p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n',encoding='utf8')
p=ROOT/'docs/production/plan10-heroine-roster.json';d=read(p)
for holy in [False,True]:
    hid='heroine.annihilator'+('-holy' if holy else '')
    entry=dict(id=hid,personId='heroine.annihilator',referenceName='アナイアレイター'+('（聖夜）' if holy else ''),jobId='job.healer' if holy else 'job.fighter',stage='implemented',available=True,profile='docs/references/characters/annihilator.md',sourceIds=[('angelica-battle:010戦闘_アナイアレイター(聖夜).mkv' if holy else 'angelica-battle:009戦闘_アナイアレイター.mkv'),('angelica-angel:010_アナイアレイター(聖夜).mkv' if holy else 'angelica-angel:009_アナイアレイター.mkv')],unresolved=['参考調査：全ボイス・全詩と画面で切れた条件文'],implementation='docs/production/plan10-annihilator-implementation.md')
    d['heroines']=[x for x in d['heroines'] if x['id']!=hid]+[entry]
write(p,d)
p=ROOT/'docs/references/heroine-expansion-source-inventory.json';d=read(p)
for source in d['sources']:
    for f in source.get('files',[]):
        if f['relativePath'].startswith(('009','010')) and 'アナイアレイター' in f['relativePath']:
            f.update(observationStatus='plan10-partial-evidence-extracted',heroineId='heroine.annihilator'+('-holy' if '(聖夜)' in f['relativePath'] else ''),evidence='docs/references/characters/annihilator.md')
write(p,d)
p=ROOT/'docs/production/plan10-implementation-plan.md';s=p.read_text(encoding='utf-8-sig')
s=s.replace('状態：Rの正式実装・受入れ。','状態：R・アナイアレイター通常版／聖夜版の正式実装・受入れ。')
s=s.replace('| ファイター | スレイヤー | 計画9提供済み |','| ファイター | スレイヤー／アナイアレイター | 通常版追加・加入／編成対応 |')
s=s.replace('| ヒーラー | 未選定 | 資料照合待ち |','| ヒーラー | アナイアレイター（聖夜） | 正式実装・生命操作対応 |')
note='\n## 通常版・聖夜版の追加\n\n[アナイアレイター実装](plan10-annihilator-implementation.md)を追加。現在は8形態・7人・7ジョブ。残り6ジョブを今後照合する。通常版と聖夜版は一人の天使として好感度・恋人関係を共有し、同時編成・庭の二重配置を禁止する。育成と物語の既読は形態ごとに保持する。\n'
if '## 通常版・聖夜版の追加' not in s:s+=note
p.write_text(s,encoding='utf8')
for name in ['docs/README.md','docs/references/README.md']:
    p=ROOT/name;s=p.read_text(encoding='utf-8-sig')
    link='production/plan10-annihilator-implementation.md' if name=='docs/README.md' else 'characters/annihilator.md'
    if link not in s:s+='\n- [アナイアレイター通常版・聖夜版の人物と実装]('+link+')\n'
    p.write_text(s,encoding='utf8')
print('Roster, source inventory and plan10 index updated.')
