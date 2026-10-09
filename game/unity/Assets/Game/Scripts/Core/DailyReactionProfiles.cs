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
                default:throw new ArgumentException("A main reaction style must be authored for "+personId);
            }
        }
    }
}
