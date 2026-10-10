using System;
namespace NewAster.Core
{
    // Names follow the supplied character videos; battle values remain newASTER rules.
    public sealed class HeroineAuthoredIdentity
    {
        public string Innate,Personal,Mastery,Weapon,Flavor;
        public HeroineAuthoredIdentity(string innate,string personal,string mastery,string weapon,string flavor)
        {Innate=innate;Personal=personal;Mastery=mastery;Weapon=weapon;Flavor=flavor;}
    }
    public static class HeroineAuthoredNames
    {
        public static HeroineAuthoredIdentity For(string id)
        {
            switch(id){
                case "heroine.slayer":return new HeroineAuthoredIdentity("神殺しの誓い","当たらなければ平気です！","騎士剣の祝福","騎士剣グラウ","守りたい明日がある。そのためなら、空を裂く刃にもなれる。");
                case "heroine.iconoclast":return new HeroineAuthoredIdentity("戦いの中で成長するタイプ","限界を砕くゆうしゃ","ゆうしゃの祝福","ゆうしゃのけん","越えられない壁なら、壊して進む。昨日の自分にも追いつかせない。");
                case "heroine.undermine":return new HeroineAuthoredIdentity("アンブレイカブル","絶対自分領域","ミラクルボディの祝福","にゃんちゃみの盾","傷つくことにも慣れている。それでも、帰る場所までは壊させない。");
                case "heroine.echidna":return new HeroineAuthoredIdentity("黄金の血","怪物のウタ","怪物の姫の祝福","怪物の姫杖","知らない歌が血の奥で響く。恐れず耳を澄ませば、力は応える。");
                case "heroine.excalipan":return new HeroineAuthoredIdentity("パンがります！","工作員の切り札","いい体してるね、パン食べてく？","MP90の銃剣","焼きたての元気を届けよう。ひるまない笑顔で、最後の一発まで。");
                case "heroine.r":return new HeroineAuthoredIdentity("ボクが来たよ！","ボクたちは愛し合える","ロケットスターの祝福","エーオースの音弦","遠くの誰かにも届くように。ひとりの歌を、みんなの居場所に変える。");
                case "heroine.annihilator":return new HeroineAuthoredIdentity("黄泉戸喫","彼岸の灯火","滅びの時は来たり","黒死鎌リーパー","終わりを告げる刃にも、守るための使い道はある。");
                case "heroine.annihilator-holy":return new HeroineAuthoredIdentity("祝祭の輝き","漆羽の御翼","不倶戴天","Love and Hate","黒い翼で包むのは、今夜だけの贈り物。冷えた手にも温もりを。");
                case "heroine.shell":return new HeroineAuthoredIdentity("ヘヴィアーマー","復讐心","フルアーマーブースト","Mk.IV強化装甲","砕けるのは装甲まで。仲間を連れ帰る意志は、何度でも立ち上がる。");
                case "heroine.oriflamme":return new HeroineAuthoredIdentity("騎士の炎","ベストコンディション","魔女の残響","黒の剣リーパー","正義の火は、帰る道まで照らしてこそ。誰かの明日を灰にしない。");
                case "heroine.nighthawk":return new HeroineAuthoredIdentity("青い炎","エアロダイナミクス","よだかの星の祝福","アルビレオ","夜を駆ける青い火。見失いそうな道にも、小さな星を残していく。");
                case "heroine.slayer-swim":return new HeroineAuthoredIdentity("元気爆発！ 神殺し！","夏満喫計画","私の力はこんなものじゃない！","夏色パラソル","夏の光も仲間の笑顔も、ひとつ残らず持ち帰る。号令は明るく、高らかに。");
                case "heroine.arcane":return new HeroineAuthoredIdentity("わたくしのとっておき","ポジティブ思考","冒険者の祝福","ハンドキャノン","まだ見ぬ宝は、きっと次の曲がり角。とっておきは、驚く顔と一緒に。");
                case "heroine.arcane-academy":return new HeroineAuthoredIdentity("わたくしが一番上手く使えるの","自信家","オーパーツは裏切りませんわ","クロノグリモワール","答えを見つけるまで、何度でも頁をめくる。失敗も次の一手の材料ですわ。");
                case "heroine.shangrila":return new HeroineAuthoredIdentity("7時間寝れた","冷静沈着","納期5分前の焦燥","象撃ち銃","眠れた朝の照準はぶれない。仕事を終えたら、今度こそ静かな休息を。");
                default:throw new ArgumentException("Missing authored heroine names: "+id);
            }
        }
    }
}
