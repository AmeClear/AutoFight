using System.Collections.Generic;

/// <summary>
/// 单个 Actor 的数据记录，包含当前属性快照与变化历史。
/// </summary>
public class ActorDataRecord
{
    public int ActorId { get; set; }
    public Actor Actor { get; set; }
    public string ActorName { get; set; }
    public float RegisterTime { get; set; }
    public bool IsActive { get; set; } = true;

    public Dictionary<string, AttributeSnapshot> Attributes { get; } =
        new Dictionary<string, AttributeSnapshot>();

    public List<AttributeChangeRecord> ChangeHistory { get; } = new List<AttributeChangeRecord>();

    public AttributeSnapshot GetAttribute(string attributeFullName)
    {
        Attributes.TryGetValue(attributeFullName, out var snapshot);
        return snapshot;
    }

    public IReadOnlyList<AttributeChangeRecord> GetHistory(string attributeFullName = null)
    {
        if (string.IsNullOrEmpty(attributeFullName))
            return ChangeHistory;

        var filtered = new List<AttributeChangeRecord>();
        foreach (var record in ChangeHistory)
        {
            if (record.AttributeFullName == attributeFullName)
                filtered.Add(record);
        }

        return filtered;
    }
}
