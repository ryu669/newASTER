import json,datetime
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
def read(p):return json.loads(p.read_text(encoding='utf-8-sig'))
def write(p,d):p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n',encoding='utf8')
p=ROOT/'docs/production/plan10-heroine-roster.json';d=read(p)
entry=dict(id='heroine.oriflamme',personId='heroine.oriflamme',referenceName='オリフラム',jobId='job.alchemist',stage='implemented',available=True,profile='docs/references/characters/oriflamme.md',sourceIds=['angelica-battle:032戦闘_オリフラム.mkv','angelica-angel:032_オリフラム.mkv'],unresolved=['参考調査：全ボイスの文字起こしと全詩の精読'],implementation='docs/production/plan10-oriflamme-implementation.md');d['heroines']=[x for x in d['heroines'] if x['id']!='heroine.oriflamme']+[entry];write(p,d)
p=ROOT/'docs/references/heroine-expansion-source-inventory.json';d=read(p)
for source in d['sources']:
 if source.get('id') not in ['angelica-battle','angelica-angel']:continue
 name='032戦闘_オリフラム.mkv' if source['id']=='angelica-battle' else '032_オリフラム.mkv'
 row=next((x for x in source['files'] if x['relativePath']==name),None)
 if row is None:
  path=Path(source['path'])/name;st=path.stat();row=dict(relativePath=name,bytes=st.st_size,modifiedUtc=datetime.datetime.fromtimestamp(st.st_mtime,datetime.timezone.utc).isoformat());source['files'].append(row);source['count']=len(source['files'])
 row.update(observationStatus='plan10-partial-evidence-extracted',heroineId='heroine.oriflamme',evidence='docs/references/characters/oriflamme.md')
write(p,d)
p=ROOT/'docs/production/plan10-implementation-plan.md';s=p.read_text(encoding='utf-8-sig').replace('| アルケミスト | 未選定 | 資料照合待ち |','| アルケミスト | オリフラム | 正式実装・5属性錬成対応 |')
if '## アルケミスト・オリフラムの追加' not in s:s+='\n## アルケミスト・オリフラムの追加\n\n[実装記録](plan10-oriflamme-implementation.md)と[人物資料](../references/characters/oriflamme.md)。現在は10形態・9人・9ジョブ、残り4ジョブ。行動消費なしの5属性錬成、火侵蝕・増幅・共振、3章30ページ・18詩・5交流、専用美術18点を追加。\n'
p.write_text(s,encoding='utf8')
for file in ['docs/production/post-plan9-jobs.md','docs/production/battle-v0.2-adoption.md']:
 p=ROOT/file;s=p.read_text(encoding='utf-8-sig')
 s=s.replace('現在は8人・9形態・8ジョブ。残り5ジョブは未制作として残す。','アルケミストのオリフラムも追加し、現在は9人・10形態・9ジョブ。残り4ジョブは未制作として残す。').replace('未提供は残り5ジョブ。','アルケミストの[オリフラム](plan10-oriflamme-implementation.md)も接続し、未提供は残り4ジョブ。')
 if 'オリフラム初期調整' not in s and 'battle-v0.2' in file:s+='\nオリフラム初期調整：錬成資源初期5・上限15、通常コマンド解決で＋1。5属性は火・雷・闇・光・斬撃。属性別投入を原子的に検証し1回消費、READYとClockは維持。攻撃35%×投入個数、3属性以上混合＋25%、火1個で火傷20。火2＋光1以上を使う弱点錬成は合計×10%（最大50%）の火弱点3ターン。対象ごとに保持し再付与は置換。動画にない調整値は独自設計。\n'
 p.write_text(s,encoding='utf8')
for file,link,label in [('docs/README.md','production/plan10-oriflamme-implementation.md','オリフラムのアルケミスト実装'),('docs/references/README.md','characters/oriflamme.md','オリフラムの性格・口調・概要')]:
 p=ROOT/file;s=p.read_text(encoding='utf-8-sig')
 if link not in s:s+='\n- ['+label+']('+link+')\n'
 p.write_text(s,encoding='utf8')
p=ROOT/'docs/requirements/game.md';s=p.read_text(encoding='utf-8-sig')
if '計画10オリフラム' not in s:s+='\n### 計画10オリフラム（2026-10-06）\n\nアルケミストのオリフラムを追加。人物固有の5属性・属性別投入個数・残資源を検証し、READYとClockを消費せず攻撃／火弱点錬成を行う。3スキル、火傷連携、育成・神器・庭、旧保存からの無償加入と関係・既読の保存、3章30ページ・18詩・5交流、性格・口調・概要を提供する。\n'
p.write_text(s,encoding='utf8');print('Oriflamme inventory and implementation records updated.')
