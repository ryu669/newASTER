import json,datetime
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
def read(p):return json.loads(p.read_text(encoding='utf-8-sig'))
def write(p,d):p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n',encoding='utf8')
p=ROOT/'docs/production/plan10-heroine-roster.json';d=read(p)
entry=dict(id='heroine.nighthawk',personId='heroine.nighthawk',referenceName='ナイトホーク',jobId='job.chaser',stage='implemented',available=True,profile='docs/references/characters/nighthawk.md',sourceIds=['angelica-battle:066戦闘_ナイトホーク.mkv','angelica-angel:066_ナイトホーク.mkv'],unresolved=['参考調査：全ボイスの文字起こしと全詩の精読'],implementation='docs/production/plan10-nighthawk-implementation.md');d['heroines']=[x for x in d['heroines'] if x['id']!='heroine.nighthawk']+[entry];write(p,d)
p=ROOT/'docs/references/heroine-expansion-source-inventory.json';d=read(p)
for source in d['sources']:
 if source.get('id') not in ['angelica-battle','angelica-angel']:continue
 name='066戦闘_ナイトホーク.mkv' if source['id']=='angelica-battle' else '066_ナイトホーク.mkv'
 row=next((x for x in source['files'] if x['relativePath']==name),None)
 if row is None:
  path=Path(source['path'])/name;st=path.stat();row=dict(relativePath=name,bytes=st.st_size,modifiedUtc=datetime.datetime.fromtimestamp(st.st_mtime,datetime.timezone.utc).isoformat());source['files'].append(row);source['count']=len(source['files'])
 row.update(observationStatus='plan10-partial-evidence-extracted',heroineId='heroine.nighthawk',evidence='docs/references/characters/nighthawk.md')
write(p,d)
p=ROOT/'docs/production/plan10-implementation-plan.md';s=p.read_text(encoding='utf-8-sig').replace('| チェイサー | 未選定 | 資料照合待ち |','| チェイサー | ナイトホーク | 正式実装・GEAR/NITRO/IGNITION対応 |').replace('現在は9形態・8人・8ジョブ','シェル追加時点は9形態・8人・8ジョブ').replace('現在は10形態・9人・9ジョブ','オリフラム追加時点は10形態・9人・9ジョブ')
if '## チェイサー・ナイトホークの追加' not in s:s+='\n## チェイサー・ナイトホークの追加\n\n[実装記録](plan10-nighthawk-implementation.md)と[人物資料](../references/characters/nighthawk.md)。現在は11形態・10人・10ジョブ。残り3ジョブはスナイパー・ギャンブラー・ジェネラル。GEAR待機短縮、NITRO、対象ごとのIGNITION、3スキル、3章30ページ・18詩・5交流、専用美術18点を追加。\n'
p.write_text(s,encoding='utf8')
for file in ['docs/production/post-plan9-jobs.md','docs/production/battle-v0.2-adoption.md']:
 p=ROOT/file;s=p.read_text(encoding='utf-8-sig')
 s=s.replace('現在は9人・10形態・9ジョブ。残り4ジョブは未制作として残す。','ナイトホークのチェイサーも追加し、現在は10人・11形態・10ジョブ。残り3ジョブは未制作として残す。').replace('未提供は残り4ジョブ。','チェイサーの[ナイトホーク](plan10-nighthawk-implementation.md)も接続し、未提供は残り3ジョブ。')
 if 'ナイトホーク初期調整' not in s:s+='\nナイトホーク初期調整：ギア初期6・上限15、100 Clockごとに2回復。GEAR 1/2/3の待機100/65/35%、消費0/2/4、有料コマンドでNITRO1（上限10）。NITRO5で次の有効スキルの待機0・即READY。対象別IGNITION3で一度だけ敵全体物理200%追加攻撃、再帰なし。調整は本作独自設計。\n'
 p.write_text(s,encoding='utf8')
for file,link,label in [('docs/README.md','production/plan10-nighthawk-implementation.md','ナイトホークのチェイサー実装'),('docs/references/README.md','characters/nighthawk.md','ナイトホークの性格・口調・概要')]:
 p=ROOT/file;s=p.read_text(encoding='utf-8-sig')
 if link not in s:s+='\n- ['+label+']('+link+')\n'
 p.write_text(s,encoding='utf8')
p=ROOT/'docs/requirements/game.md';s=p.read_text(encoding='utf-8-sig')
if '計画10ナイトホーク' not in s:s+='\n### 計画10ナイトホーク（2026-10-07）\n\nチェイサーのナイトホークを追加。GEARによる待機短縮、NITROの原子的消費、対象ごとのIGNITIONを実装。3スキル、育成・神器・庭、旧保存からの無償加入、関係・既読の保存、3章30ページ・18詩・5交流、性格・口調・概要を提供する。\n'
p.write_text(s,encoding='utf8');print('Nighthawk inventory and implementation records updated.')
