"""Update current production entry points after April's review is accepted."""
from pathlib import Path
import json
R=Path(__file__).resolve().parents[1]
assert json.loads((R/'game/unity/Assets/Game/ContentDrafts/April/manifest.json').read_text(encoding='utf-8-sig'))['available']
for p in ['README.md','game/README.md','docs/characters/README.md','docs/production/heroine-addition-guide.md']:
    f=R/p;s=f.read_text(encoding='utf8')
    # Historical dated notes stay as evidence of their earlier catalog.
    if p.endswith('heroine-addition-guide.md'):
        s=s.replace('ウェポンマスターを含む24形態カタログは受入れを完了','エイプリルを含む25形態カタログは受入れを完了').replace('現行は21人物・24形態。','現行は22人物・25形態。')
        s+='\n2026-10-12追記：エイプリル通常版を追加（25形態・22人物）。作業SDは単体だけでなく実際の机で顔・手・接地を確認し、不適合だった初版を専用の第2版へ交換。既存形態の表示はApril専用分岐を除くと同一であることを検証し、完了済みの全素材を再審査しない。専用20点と共通の木、3技、旧24形態セーブ移行の記録は `plan12-april-form-record.json`。\n'
    else:
        s=s.replace('24形態・21人','25形態・22人').replace('21人物・24形態','22人物・25形態').replace('117章792詩・120交流','120章810詩・125交流')
        s=s.replace('plan12-weapon-master/newASTER.exe','plan12-april/newASTER.exe').replace('Plan12WeaponMasterBuild.ValidateAndBuild','Plan12AprilBuild.ValidateAndBuild').replace('Combat/battle-plan12-weapon-master','Combat/battle-plan12-april').replace('Story/plan12-weapon-master-story-content','Story/plan12-april-story-content')
        s=s.replace('ウェポンマスターまで追加済み','ウェポンマスター、エイプリルまで追加済み')
        s+='\nエイプリル通常版の専用20画像・3章・5交流・3技と旧24形態セーブ移行を確認。[完了記録]('+('../' if p=='game/README.md' else '../../' if p=='docs/characters/README.md' else '')+'docs/production/plan12-april-form-record.json)。通常プレイは25形態・22人物。\n'
        if p=='docs/characters/README.md':
            s=s.replace('2026-10-11更新。','2026-10-12更新。')
            s+='\n- [エイプリル](april.md) — ケアト・アーニスリンナ／ファイター。3章・5交流、専用20点、共通構成の木。\n'
    f.write_text(s,encoding='utf8')
f=R/'tools/build-heroine-production-roster.py';s=f.read_text(encoding='utf8').replace('16, 17, 18, 20],','16, 17, 18, 20, 21],').replace("'vindicator', 'weapon-master']))","'vindicator', 'weapon-master', 'april']))").replace('現行24形態','現行25形態').replace('必要21画像','必要20画像');f.write_text(s,encoding='utf8')
print('APRIL_HANDOFF_UPDATED')
