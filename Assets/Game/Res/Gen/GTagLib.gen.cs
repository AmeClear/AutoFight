///////////////////////////////////
//// This is a generated file. ////
////     Do not modify it.     ////
///////////////////////////////////

using System.Collections.Generic;

namespace GAS.Runtime
{
    public static class GTagLib
    {
        public static GameplayTag Ability { get; } = new GameplayTag("Ability");
        public static GameplayTag Ability_CD { get; } = new GameplayTag("Ability.CD");
        public static GameplayTag Ability_FightAction { get; } = new GameplayTag("Ability.FightAction");
        public static GameplayTag Faction { get; } = new GameplayTag("Faction");
        public static GameplayTag Faction_Enemy { get; } = new GameplayTag("Faction.Enemy");
        public static GameplayTag Faction_Player { get; } = new GameplayTag("Faction.Player");

        public static Dictionary<string, GameplayTag> TagMap = new Dictionary<string, GameplayTag>
        {
            ["Ability"] = Ability,
            ["Ability.CD"] = Ability_CD,
            ["Ability.FightAction"] = Ability_FightAction,
            ["Faction"] = Faction,
            ["Faction.Enemy"] = Faction_Enemy,
            ["Faction.Player"] = Faction_Player,
        };
    }
}