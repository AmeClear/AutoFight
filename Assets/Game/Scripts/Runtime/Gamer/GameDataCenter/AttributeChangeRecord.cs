using UnityEngine;

/// <summary>
/// 单条属性变化记录。
/// </summary>
public class AttributeChangeRecord
{
    public int ActorId { get; set; }
    public string AttributeFullName { get; set; }
    public AttributeChangePhase Phase { get; set; }
    public float OldValue { get; set; }
    public float NewValue { get; set; }
    public float Timestamp { get; set; }

    public static AttributeChangeRecord Create(
        int actorId,
        string attributeFullName,
        AttributeChangePhase phase,
        float oldValue,
        float newValue)
    {
        return new AttributeChangeRecord
        {
            ActorId = actorId,
            AttributeFullName = attributeFullName,
            Phase = phase,
            OldValue = oldValue,
            NewValue = newValue,
            Timestamp = Time.time
        };
    }
}
