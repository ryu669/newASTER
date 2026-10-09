using NewAster.Core;
namespace NewAster.Presentation
{
 public sealed partial class PrototypeBootstrap
 {
  private string BookHelpTitle()=>encounter!=null?"戦闘":oopartRequest!=null?"オーパーツの確定":oopartPanel?"オーパーツ強化":bookSystemOpen?"システム":affectionShop?"特別交換":affectionPanel?"好感度":book.Bookmark==BookBookmark.Formation?(formationLayer==2?"誓女の入れ替え":formationLayer==3?"オーパーツの入れ替え":formationLayer==1?"戦闘設定":"編成"):RibbonNames[System.Array.IndexOf(RibbonOrder,book.Bookmark)];
  private string BookHelpText()
  {
   if(encounter!=null)return "行動できる誓女を選び、スキルと対象を決定します。行動順は速度と待機時間で変わります。待機と詠唱は別の時間です。連携可能な行動をつなぐとチェインになります。\n\n対象・部位で攻撃先を選び、人物・状態で能力と効果を、行動順で予定を確認できます。一時停止から手動で再開できます。撤退では好感度EXPを得ません。";
   if(oopartPanel || oopartRequest!=null)return "固定能力と強化で得た能力は加算されます。特殊効果は最大2つで、条件を満たすと有効になります。ジョブ条件が合わない場合も固定能力は有効です。\n\n強化内容と消費を確認して確定してください。保存に失敗した場合は同じ内容で再試行できます。装備の変更に費用はかかりません。";
   if(bookSystemOpen)return "保存で現在の進行を記録します。設定から音量などを変更できます。表紙へ戻っても進行は保持されます。";
   if(affectionShop)return "永遠の誓環は召喚石10,000個で購入できます。好感度Lv20の人物に1個使うと上限が99になります。同じ人物への再使用、返品、付け替えはできません。";
   if(affectionPanel)return "交流で好感度EXPを得られます。100EXPでLvが上がり、Lv10から恋人になります。衣装違いは同じ人物の好感度を共有します。\n\nイベントは必要Lvに達すると閲覧できます。回想ではEXP・報酬・進行は変わりません。";
   if(book.Bookmark==BookBookmark.Formation){
    if(formationLayer==2)return "候補の画像を選ぶと左側に能力とスキルが表示されます。名前で検索し、名前またはLvで並べ替えできます。決定で選んだ誓女を編成に反映します。\n\nほかの枠の誓女を選ぶと枠を入れ替えます。同じ人物の衣装違いを重複編成することはできません。外すと空枠になります。";
    if(formationLayer==3)return "候補の画像を選ぶと左側に能力、特殊効果、発動条件が表示されます。決定で装備を反映します。強化から選んだオーパーツを育成できます。\n\n他の枠に装備中のものは、先に外す必要があります。装備変更は無料です。空の誓女枠にも装備は保持されます。ジョブ条件が合わない特殊効果は無効ですが、固定能力は有効です。";
    if(formationLayer==1)return "神器、かばう対象、ジョブ固有の戦闘設定を変更できます。設定は選択した編成枠の誓女に適用されます。";
    return "誓女の画像で誓女を、下の画像でオーパーツを入れ替えます。各枠のメニューから戦闘設定を開けます。\n\n編成保存で選択中の編成に記録します。保存済みの編成タブを選ぶと呼び出せます。出撃には5人の誓女が必要です。";
   }
   switch(book.Bookmark){
    case BookBookmark.Heroines:
     if(!heroineRosterOpen && growthScreen==GrowthScreen.Skill)return "スキルを選ぶと現在の効果と強化後の効果を確認できます。必要素材を確認し、確定するとスキルLvが上がります。スキルLvでは待機・詠唱・資源消費・チェイン率は変わりません。";
     if(!heroineRosterOpen && growthScreen==GrowthScreen.Level)return "必要素材と到達Lvを確認して強化を確定します。素材は保存が成功すると消費されます。";
     if(!heroineRosterOpen && growthScreen==GrowthScreen.Awakening)return "覚醒によりLv上限が上がります。必要素材と上限の変化を確認して確定してください。";
     if(!heroineRosterOpen && growthScreen==GrowthScreen.Duplicate)return "誓いを重ねると表示された能力が強化されます。必要な資源と変化を確認して確定してください。";
     if(!heroineRosterOpen && growthScreen==GrowthScreen.Weapons)return "根は初期状態で解放・装備済みで、Lvや固有能力はありません。枝のノードには通常能力に加えて固有能力が一つあり、その能力のアイコンを表示します。取得した神器を選んで装備すると能力が適用されます。根を装備で初期状態に戻せます。";
     return "画像を選ぶと人物の詳細を開きます。Lv、覚醒、誓い、スキル、神器を各ページで育成できます。必要素材と変化を確認して確定してください。\n\n部隊の入れ替えは編成のしおりから行います。神器の枝に沿ってノードを取得すると効果が追加されます。";
    case BookBookmark.Items:return "オーパーツと素材の所持数を確認できます。オーパーツは編成枠に装備します。巨神獣撃破時に低確率で入手でき、レリックハントでは対象を狙えます。";
    case BookBookmark.Gardens:return "住民や家具を配置して箱庭の暮らしを見守れます。話す、一緒に過ごす、催事や発見への参加で好感度EXPを得ます。観察だけではEXPは増えません。\n\n模様替えでは配置を編集し、確定で保存します。";
    case BookBookmark.Summoning:return "召喚対象は衣装違いを含む実装済みの全ヒロイン形態です。所持済みも対象です。★6合計3%を全形態で均等配分します。10回召喚に確定枠はありません。\n\n石召喚1回につき1ポイント。100ポイントで選んだ形態の専用チケット1枚と交換できます。交換だけでは加入せず、チケット使用で対象形態を確定入手します。チケット使用では石消費・抽選・ポイント付与はありません。ポイントとチケットは失効しません。\n\n所持済みの形態は専用欠片100、重複Rank最大後は汎用超過素材100になります。特別交換では永遠の誓環を購入できます。オーパーツは巨神獣討伐のレリックハントで入手できます。";
    case BookBookmark.Stories:return "下部の矢印で物語を切り替え、概要と詳細を選べます。解放された物語を選ぶと閲覧できます。回想では報酬や進行は変わりません。";
    case BookBookmark.NewWorld:return "新天地の開拓や配置を行います。必要資源と結果を確認し、確定すると保存されます。";
    case BookBookmark.PossibleWorlds:return "条件を満たした可能世界を選んで探索します。各世界の条件と現在の進行を確認できます。";
    default:return "下部の矢印で対象を切り替え、概要と詳細を選べます。討伐では選択中の編成を使用します。編成のしおりから誓女とオーパーツを準備してください。\n\n上部のしおりで各ページへ移動できます。保存と設定はシステムから開きます。";
   }
  }
 }
}
