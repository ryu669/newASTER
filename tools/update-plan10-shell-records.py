import json,datetime
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
def read(p):return json.loads(p.read_text(encoding='utf-8-sig'))
def write(p,d):p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n',encoding='utf8')
p=ROOT/'docs/production/plan10-heroine-roster.json';d=read(p)
entry=dict(id='heroine.shell',personId='heroine.shell',referenceName='シェル',jobId='job.panzer',stage='implemented',available=True,profile='docs/references/characters/shell.md',sourceIds=['angelica-battle:048戦闘_シェル.mkv','angelica-angel:048_シェル.mkv'],unresolved=['参考調査：全ボイスの文字起こしと全詩の精読'],implementation='docs/production/plan10-shell-implementation.md');d['heroines']=[x for x in d['heroines'] if x['id']!='heroine.shell']+[entry];write(p,d)
p=ROOT/'docs/references/heroine-expansion-source-inventory.json';d=read(p)
for source in d['sources']:
    if source.get('id') not in ['angelica-battle','angelica-angel']:continue
    name='048戦闘_シェル.mkv' if source['id']=='angelica-battle' else '048_シェル.mkv'
    row=next((x for x in source['files'] if x['relativePath']==name),None)
    if row is None:
        path=Path(source['path'])/name;st=path.stat();row=dict(relativePath=name,bytes=st.st_size,modifiedUtc=datetime.datetime.fromtimestamp(st.st_mtime,datetime.timezone.utc).isoformat());source['files'].append(row);source['count']=len(source['files'])
    row.update(observationStatus='plan10-partial-evidence-extracted',heroineId='heroine.shell',evidence='docs/references/characters/shell.md')
write(p,d)
p=ROOT/'docs/production/plan10-implementation-plan.md';s=p.read_text(encoding='utf-8-sig').replace('| パンツァー | 未選定 | 資料照合待ち |','| パンツァー | シェル | 正式実装・装甲／生身／有限ツール対応 |').replace('状態：R・アナイアレイター通常版／聖夜版の正式実装・受入れ。','状態：R・アナイアレイター通常版／聖夜版・シェルの正式実装・受入れ。').replace('現在は8形態・7人・7ジョブ。残り6ジョブを今後照合する。','通常版・聖夜版追加時点は8形態・7人・7ジョブ。')
if '## パンツァー・シェルの追加' not in s:s+='\n## パンツァー・シェルの追加\n\n[シェル実装](plan10-shell-implementation.md)と[人物資料](../references/characters/shell.md)を追加。現在は9形態・8人・8ジョブ、残り5ジョブ。装甲耐性・ツール設定を保存し、装甲喪失時の生身、防御、300 Clockの再召喚を実行する。3章30ページ・18詩・5交流と専用美術19点を追加した。\n'
p.write_text(s,encoding='utf8')
p=ROOT/'docs/production/post-plan9-jobs.md';s=p.read_text(encoding='utf-8-sig').replace('現在は7人・8形態・7ジョブで、残り6ジョブは未制作として残す。','パンツァーのシェルも追加し、現在は8人・9形態・8ジョブ。残り5ジョブは未制作として残す。');s=s.replace('将来の追加人数・公開日は未確定。','[シェル実装](plan10-shell-implementation.md)も参照。将来の追加人数・公開日は未確定。');p.write_text(s,encoding='utf8')
for name,link,label in [('docs/README.md','production/plan10-shell-implementation.md','シェルのパンツァー実装'),('docs/references/README.md','characters/shell.md','シェルの性格・口調・概要')]:
    p=ROOT/name;s=p.read_text(encoding='utf-8-sig')
    if link not in s:s+='\n- ['+label+']('+link+')\n'
    p.write_text(s,encoding='utf8')
p=ROOT/'docs/requirements/game.md';s=p.read_text(encoding='utf-8-sig')
if '計画10シェル' not in s:s+='\n### 計画10シェル（2026-10-06）\n\nパンツァーのシェルを追加。装甲耐性とツール2枠の事前設定を保存し、装甲HPへの通常回復を拒否、生身では防御のみ、BREAKからClock300生存で再召喚する。消費済みツールを補充しない。旧保存の育成・残高・関係・既読を維持し、無償加入、専用育成と神器、3章30ページ・18詩・5交流、性格・口調・概要の資料を提供する。\n'
p.write_text(s,encoding='utf8');print('Shell records and source inventory reconciled.')
