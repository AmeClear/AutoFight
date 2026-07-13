/// <summary>
/// 属性快照，记录某一时刻的完整属性状态。
/// </summary>
public class AttributeSnapshot
{
    public string AttributeSetName { get; set; }
    public string AttributeName { get; set; }
    public string AttributeFullName { get; set; }
    public float BaseValue { get; set; }
    public float CurrentValue { get; set; }
    public float MinValue { get; set; }
    public float MaxValue { get; set; }

    public static AttributeSnapshot FromAttribute(GAS.Runtime.AttributeBase attribute)
    {
        return new AttributeSnapshot
        {
            AttributeSetName = attribute.SetName,
            AttributeName = attribute.ShortName,
            AttributeFullName = attribute.Name,
            BaseValue = attribute.BaseValue,
            CurrentValue = attribute.CurrentValue,
            MinValue = attribute.MinValue,
            MaxValue = attribute.MaxValue
        };
    }

    public void UpdateFromAttribute(GAS.Runtime.AttributeBase attribute)
    {
        BaseValue = attribute.BaseValue;
        CurrentValue = attribute.CurrentValue;
        MinValue = attribute.MinValue;
        MaxValue = attribute.MaxValue;
    }
}
