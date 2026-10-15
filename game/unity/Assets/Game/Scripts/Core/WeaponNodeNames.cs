using System;
namespace NewAster.Core
{
    // newASTER weapon variants. These suffixes are authored here, not source-video trait names.
    public static class WeaponNodeNames
    {
        public static string Name(string heroineId,int route,int tier)
        {
            string[] variants=heroineId=="heroine.iron-maiden"?new[]{"イグニス","アルクス","ウェントゥス"}:
                heroineId=="heroine.aeon"?new[]{"カンタービレ","セレナータ","カプリッチオ"}:
                heroineId=="heroine.avalanche"?new[]{"ロンド","ノクターン","スケルツォ"}:
                heroineId=="heroine.anathema"?new[]{"ロサ","コロナ","ステラ"}:
                heroineId=="heroine.shiva"?new[]{"ソレイユ","エスポワール","エラン"}:
                heroineId=="heroine.invisible"?new[]{"曙光","星虹","帰路"}:
                heroineId=="heroine.viper"?new[]{"暁鐘","花火","帰路"}:
                heroineId=="heroine.vindicator"?new[]{"誓衛","静空","黎明"}:
                heroineId=="heroine.weapon-master"?new[]{"黎光","翠雨","旅路"}:
                heroineId=="heroine.april"?new[]{"萌芽","花冠","再生"}:
                heroineId=="heroine.estreya"?new[]{"黎明","灼星","凱旋"}:
                new[]{"ルーメン","クストス","ウェント"};
            return Format(HeroineAuthoredNames.For(heroineId).Weapon,variants,route,tier);
        }
        public static string Format(string weapon,string[] variants,int route,int tier)
        {
            if(string.IsNullOrWhiteSpace(weapon)||variants==null||variants.Length!=3||route<0||route>2||tier<1||tier>4)throw new ArgumentException("Invalid weapon branch name.");
            return weapon+"・"+variants[route]+new[]{"Ⅰ","Ⅱ","Ⅲ","Ⅳ"}[tier-1];
        }
    }
}
