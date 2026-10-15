using System;
namespace NewAster.Core
{
    public static class DailyReactionProfiles
    {
        public static ReactionStyleProfile For(string personId)
        {
            switch(personId){
                case "heroine.slayer":return new ReactionStyleProfile{main="active",sub="elegant"};
                case "heroine.iconoclast":return new ReactionStyleProfile{main="bold",sub="teasing"};
                case "heroine.undermine":return new ReactionStyleProfile{main="reserved",sub="elegant"};
                case "heroine.echidna":return new ReactionStyleProfile{main="elegant",sub="cool"};
                case "heroine.excalipan":return new ReactionStyleProfile{main="innocent",sub="affectionate"};
                case "heroine.r":return new ReactionStyleProfile{main="affectionate",sub="active"};
                case "heroine.annihilator":return new ReactionStyleProfile{main="cool",sub="reserved"};
                case "heroine.shell":return new ReactionStyleProfile{main="reserved",sub="innocent"};
                case "heroine.oriflamme":return new ReactionStyleProfile{main="bold",sub="active"};
                case "heroine.nighthawk":return new ReactionStyleProfile{main="active",sub="cool"};
                case "heroine.arcane":return new ReactionStyleProfile{main="innocent",sub="teasing"};
                case "heroine.shangrila":return new ReactionStyleProfile{main="cool",sub="elegant"};
                case "heroine.iron-maiden":return new ReactionStyleProfile{main="teasing",sub="cool"};
                case "heroine.avalanche":return new ReactionStyleProfile{main="teasing",sub="active"};
                case "heroine.anathema":return new ReactionStyleProfile{main="elegant",sub="affectionate"};
                case "heroine.shiva":return new ReactionStyleProfile{main="active",sub="affectionate"};
                case "heroine.aeon":return new ReactionStyleProfile{main="cool",sub="teasing"};
                case "heroine.invisible":return new ReactionStyleProfile{main="reserved",sub="affectionate"};
                case "heroine.viper":return new ReactionStyleProfile{main="teasing",sub="cool"};
                case "heroine.weapon-master":return new ReactionStyleProfile{main="active",sub="affectionate"};
                case "heroine.vindicator":return new ReactionStyleProfile{main="cool",sub="reserved"};
                case "heroine.april":return new ReactionStyleProfile{main="elegant",sub="affectionate"};
                case "heroine.estreya":return new ReactionStyleProfile{main="active",sub="bold"};
                default:throw new ArgumentException("A main reaction style must be authored for "+personId);
            }
        }
    }
}
