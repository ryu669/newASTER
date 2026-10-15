"""Refresh current entry points after Estreya's normal Player acceptance."""
from pathlib import Path
import json
R=Path(__file__).resolve().parents[1]
assert json.loads((R/'docs/production/plan12-estreya-form-record.json').read_text(encoding='utf8'))['status']=='complete_published'
for name in ['README.md','game/README.md','docs/characters/README.md','docs/production/heroine-addition-guide.md']:
    p=R/name;s=p.read_text(encoding='utf8')
    s=s.replace('25形態・22人','26形態・23人').replace('22人物・25形態','23人物・26形態').replace('120章810詩・125交流','123章828詩・130交流')
    s=s.replace('plan12-april/newASTER.exe','plan12-estreya/newASTER.exe').replace('Plan12AprilBuild.ValidateAndBuild','Plan12EstreyaBuild.ValidateAndBuild').replace('Combat/battle-plan12-april','Combat/battle-plan12-estreya').replace('Story/plan12-april-story-content','Story/plan12-estreya-story-content')
    s=s.replace('エイプリルまで追加済み','エイプリル、エストレヤまで追加済み').replace('エイプリルを含む25形態カタログは受入れを完了','エストレヤを含む26形態カタログは受入れを完了')
    # The earlier April completion remains a dated milestone, not today's roster.
    s=s.replace('通常プレイは26形態・23人物。','当時の通常プレイは25形態・22人物。')
    rel='docs/production/' if name=='README.md' else '../docs/production/' if name=='game/README.md' else '../production/' if name=='docs/characters/README.md' else ''
    s+='\n2026-10-15：エストレヤ（アズ・ボーンフリー／バーサーカー）を追加。通常プレイは26形態・23人物。専用20画像、3章30ページ・18詩・5交流、共通13ノードの木、HP条件付き攻撃・反動・ヒートアップ、旧25形態セーブからの加入を確認。[完了記録]('+rel+'plan12-estreya-form-record.json)。\n'
    if name=='docs/characters/README.md':s+='\n- [エストレヤ](estreya.md) — アズ・ボーンフリー／バーサーカー。\n'
    if name.endswith('heroine-addition-guide.md'):s+='\n机用の作業SDは、両足で立ち、腰高の天板へ両手を出す姿勢を指定する。机・椅子は画像へ描き込まず、接地・手の高さ・前後の遮蔽を実際の家具と重ねて確認する。地面の作業向けの膝立ちを机へ流用しない。不適合な原本は不採用理由を残し、別バージョンの専用画像へ交換する。\n'
    if name=='README.md':s=s.replace('Core回帰検証9,413項目、Unity C#221ソース','Core回帰検証12,802項目、Unity C#280ソース')
    p.write_text(s,encoding='utf8')
p=R/'tools/build-heroine-production-roster.py';s=p.read_text(encoding='utf8').replace('18, 20, 21],','18, 20, 21, 26],').replace("'weapon-master', 'april']))","'weapon-master', 'april', 'estreya']))").replace('現行25形態','現行26形態');p.write_text(s,encoding='utf8')
p=R/'docs/production/job-implementation-guide.md';s=p.read_text(encoding='utf8');s+='\n2026-10-15：エストレヤで既存バーサーカーを再利用。3技Lv1〜7とHP49/50/51%の予測・実ダメージ、最大HP5%反動、部位破壊ごとの攻撃累積5%／上限30%／新戦闘でリセットを検証。個人特性をジョブマスタリーへ混在させない。HP閾値と補正値は対で定義し、回復技には設定しない。最新追加の実行版は `Plan12EstreyaBuild.ValidateAndBuild`、確認は `tools/validate-plan12-estreya-player.ps1`。\n';p.write_text(s,encoding='utf8')
print('ESTREYA_HANDOFF_UPDATED')
