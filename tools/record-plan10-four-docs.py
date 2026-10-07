"""Publish individual implementation records and an unaltered-game QA montage."""
from pathlib import Path
from PIL import Image,ImageDraw
R=Path(__file__).resolve().parents[1]
forms=[('slayer-swim','スレイヤー水着','ジェネラル','SlayerSwim','隊長を選び、5つの編成位置に応じた補正を適用する。本人の行動でリソースが増え、MAX15の強化は300Clock持続する。隊長の死亡、期限切れ、保存失敗と再試行を検証した。'),('arcane','アルケイン','ガンナー','Arcane','選択した弾倉から攻撃時に1発消費し、支援では消費しない。再装填は各6発、MAX10の一斉射撃は選択弾倉を空にする。映像で確認できた攻撃スキル1つと、独自の支援・全体攻撃2つを区別して記録した。'),('arcane-academy','アルケイン学園','ギャンブラー','ArcaneAcademy','3×3 SLOTを3行と2対角線の5ラインで判定する。2つ一致・3つ一致、777、重複ラインの発動を検証した。外れではリソースを消費せず通常WT0、状態異常の待機は適用する。スキル直接選択によるSLOTの迂回を禁止した。'),('shangrila','シャングリラ','スナイパー','Shangrila','通常支援は選択した味方1人、MAX15の詠唱中は本人以外の味方全員へ支援する。支援から支援を再発動せず、支援で本人のWTやリソースを消費しない。MAXは攻撃力のスナップショットを使用し、死亡・気絶・不在・対象部位の消失で中止する。')]
for key,name,job,cls,behavior in forms:
    text=f'''# 計画10：{name}

2026-10-07。{job}の{name}を追加し、加入・育成・神器・編成・戦闘・庭・ADVへ接続した。

{behavior}

人物の概要、性格、口調、動画の観察時刻、元の意匠と本作の創作は[人物資料](../references/characters/{key}.md)に保存。天使の羽と光輪を含む専用美術18点を生成し、無加工で採用した。生成指示は `game/art-source/plan10/{key}-generation.json`。原動画と参考切り出し画像はゲームやGitへ同梱しない。

3章30ページ・18詩・5イベント40段落を追加した。[{name}の物語](../story-text/{name}の物語.txt)に全文を保存。衣装違いは同一人物の関係を共有し、育成・神器・読書進行は別に保持する。

[美術検証](plan10-{key}-art-validation.json)、[実行検証](plan10-{key}-acceptance.json)に採用ハッシュと画面を記録する。全4形態を含む最終実行ファイルは `game/Builds/plan10-shangrila/newASTER.exe`。個別段階のビルドは `game/Builds/plan10-{key}/newASTER.exe`。

共通の最終検証はCore8,546項目、Unity C#185ソースが合格。`tools/validate-plan10-expanded-roster.ps1`、`Plan10{cls}Build.ValidateAndBuild`、`tools/validate-plan10-{key}-player.ps1`で再検証できる。画面採取は隔離した保存データを使う。

![ゲーム内検証](images/{key}-game-comparison.jpg)
'''
    (R/f'docs/production/plan10-{key}-implementation.md').write_text(text,encoding='utf8')
canvas=Image.new('RGB',(1600,940),(18,30,38));d=ImageDraw.Draw(canvas)
for i,(key,name,job,cls,behavior) in enumerate(forms):
    candidates=sorted((R/f'tmp/plan10-{key}').glob(f'player-*/*-detail.png'))
    wide=[p for p in candidates if abs(Image.open(p).width/Image.open(p).height-16/9)<.01]
    p=max(wide or candidates,key=lambda p:Image.open(p).width);im=Image.open(p).convert('RGB');im.thumbnail((800,450))
    x=(i%2)*800;y=(i//2)*470;canvas.paste(im,(x+(800-im.width)//2,y));d.text((x+12,y+451),key+' / '+job.encode('ascii','ignore').decode(),fill='white')
canvas.save(R/'docs/production/images/four-heroines-game-comparison.jpg',quality=92)
print('Four implementation records and gameplay montage saved.')
