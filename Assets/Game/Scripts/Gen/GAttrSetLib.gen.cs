///////////////////////////////////
//// This is a generated file. ////
////     Do not modify it.     ////
///////////////////////////////////

using System;
using System.Collections.Generic;

namespace GAS.Runtime
{
    public class AS_Fight : AttributeSet
    {
        #region AttackValue

        /// <summary>
        /// 
        /// </summary>
        public AttributeBase AttackValue { get; } = new ("AS_Fight", "AttackValue", 10f, CalculateMode.Stacking, (SupportedOperation)31, 0, float.MaxValue);

        public void InitAttackValue(float value)
        {
            AttackValue.SetBaseValue(value);
            AttackValue.SetCurrentValue(value);
        }

        public void SetCurrentAttackValue(float value)
        {
            AttackValue.SetCurrentValue(value);
        }

        public void SetBaseAttackValue(float value)
        {
            AttackValue.SetBaseValue(value);
        }

        public void SetMinAttackValue(float value)
        {
            AttackValue.SetMinValue(value);
        }

        public void SetMaxAttackValue(float value)
        {
            AttackValue.SetMaxValue(value);
        }

        public void SetMinMaxAttackValue(float min, float max)
        {
            AttackValue.SetMinMaxValue(min, max);
        }

        #endregion AttackValue

        #region DefProgress

        /// <summary>
        /// 防御条
        /// </summary>
        public AttributeBase DefProgress { get; } = new ("AS_Fight", "DefProgress", 100f, CalculateMode.Stacking, (SupportedOperation)31, 0, 100);

        public void InitDefProgress(float value)
        {
            DefProgress.SetBaseValue(value);
            DefProgress.SetCurrentValue(value);
        }

        public void SetCurrentDefProgress(float value)
        {
            DefProgress.SetCurrentValue(value);
        }

        public void SetBaseDefProgress(float value)
        {
            DefProgress.SetBaseValue(value);
        }

        public void SetMinDefProgress(float value)
        {
            DefProgress.SetMinValue(value);
        }

        public void SetMaxDefProgress(float value)
        {
            DefProgress.SetMaxValue(value);
        }

        public void SetMinMaxDefProgress(float min, float max)
        {
            DefProgress.SetMinMaxValue(min, max);
        }

        #endregion DefProgress

        #region HealthValue

        /// <summary>
        /// 
        /// </summary>
        public AttributeBase HealthValue { get; } = new ("AS_Fight", "HealthValue", 100f, CalculateMode.Stacking, (SupportedOperation)31, 0, 100);

        public void InitHealthValue(float value)
        {
            HealthValue.SetBaseValue(value);
            HealthValue.SetCurrentValue(value);
        }

        public void SetCurrentHealthValue(float value)
        {
            HealthValue.SetCurrentValue(value);
        }

        public void SetBaseHealthValue(float value)
        {
            HealthValue.SetBaseValue(value);
        }

        public void SetMinHealthValue(float value)
        {
            HealthValue.SetMinValue(value);
        }

        public void SetMaxHealthValue(float value)
        {
            HealthValue.SetMaxValue(value);
        }

        public void SetMinMaxHealthValue(float min, float max)
        {
            HealthValue.SetMinMaxValue(min, max);
        }

        #endregion HealthValue

        #region StamValue

        /// <summary>
        /// 体力
        /// </summary>
        public AttributeBase StamValue { get; } = new ("AS_Fight", "StamValue", 100f, CalculateMode.Stacking, (SupportedOperation)31, 0, 100);

        public void InitStamValue(float value)
        {
            StamValue.SetBaseValue(value);
            StamValue.SetCurrentValue(value);
        }

        public void SetCurrentStamValue(float value)
        {
            StamValue.SetCurrentValue(value);
        }

        public void SetBaseStamValue(float value)
        {
            StamValue.SetBaseValue(value);
        }

        public void SetMinStamValue(float value)
        {
            StamValue.SetMinValue(value);
        }

        public void SetMaxStamValue(float value)
        {
            StamValue.SetMaxValue(value);
        }

        public void SetMinMaxStamValue(float min, float max)
        {
            StamValue.SetMinMaxValue(min, max);
        }

        #endregion StamValue

        public override AttributeBase this[string key]
        {
            get
            {
                switch (key)
                {
                    case "HealthValue":
                        return HealthValue;
                    case "AttackValue":
                        return AttackValue;
                    case "StamValue":
                        return StamValue;
                    case "DefProgress":
                        return DefProgress;
                }

                return null;
            }
        }

        public override string[] AttributeNames { get; } =
        {
            "HealthValue",
            "AttackValue",
            "StamValue",
            "DefProgress",
        };

        public override void SetOwner(AbilitySystemComponent owner)
        {
            _owner = owner;
            HealthValue.SetOwner(owner);
            AttackValue.SetOwner(owner);
            StamValue.SetOwner(owner);
            DefProgress.SetOwner(owner);
        }

        public static class Lookup
        {
            public const string HealthValue = "AS_Fight.HealthValue";
            public const string AttackValue = "AS_Fight.AttackValue";
            public const string StamValue = "AS_Fight.StamValue";
            public const string DefProgress = "AS_Fight.DefProgress";
        }
    }

    public static class GAttrSetLib
    {
        public static readonly Dictionary<string, Type> AttrSetTypeDict = new Dictionary<string, Type>()
        {
            { "Fight", typeof(AS_Fight) },
        };

        public static readonly Dictionary<Type, string> TypeToName = new Dictionary<Type, string>
        {
            { typeof(AS_Fight), nameof(AS_Fight) },
        };

        public static List<string> AttributeFullNames = new List<string>()
        {
            "AS_Fight.HealthValue",
            "AS_Fight.AttackValue",
            "AS_Fight.StamValue",
            "AS_Fight.DefProgress",
        };
    }
}