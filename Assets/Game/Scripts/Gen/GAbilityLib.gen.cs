///////////////////////////////////
//// This is a generated file. ////
////     Do not modify it.     ////
///////////////////////////////////

using System;
using System.Collections.Generic;

namespace GAS.Runtime
{
    public static class GAbilityLib
    {
        public struct AbilityInfo
        {
            public string Name;
            public string AssetPath;
            public Type AbilityClassType;
        }

        public static AbilityInfo Atk = new AbilityInfo { Name = "Atk", AssetPath = "Assets/GAS/Config/GameplayAbilityLib/AIFight/Atk.asset",AbilityClassType = typeof(GAS.Runtime.TimelineAbility) };

        public static AbilityInfo Def = new AbilityInfo { Name = "Def", AssetPath = "Assets/GAS/Config/GameplayAbilityLib/AIFight/Def.asset",AbilityClassType = typeof(GAS.Runtime.TimelineAbility) };


        public static Dictionary<string, AbilityInfo> AbilityMap = new Dictionary<string, AbilityInfo>
        {
            ["Atk"] = Atk,
            ["Def"] = Def,
        };
    }
}