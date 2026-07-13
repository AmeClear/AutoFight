using GameEvent;

/// <summary>
/// 属性变化阶段。
/// </summary>
public enum AttributeChangePhase
{
    Initialize,
    BaseValue,
    CurrentValue
}

/// <summary>
/// Actor 注册完成事件，包含初始化时的全部属性快照。
/// </summary>
public class ActorRegisteredEvent : IGameEvent
{
    public int ActorId { get; set; }
    public Actor Actor { get; set; }
    public AttributeSnapshot[] InitialAttributes { get; set; }
}

/// <summary>
/// Actor 属性变化事件。
/// </summary>
public class ActorAttributeChangedEvent : IGameEvent
{
    public int ActorId { get; set; }
    public Actor Actor { get; set; }
    public string AttributeSetName { get; set; }
    public string AttributeName { get; set; }
    public string AttributeFullName { get; set; }
    public AttributeChangePhase Phase { get; set; }
    public float OldValue { get; set; }
    public float NewValue { get; set; }
    public float CurrentValue { get; set; }
    public float BaseValue { get; set; }
    public float MinValue { get; set; }
    public float MaxValue { get; set; }
}

/// <summary>
/// Actor 注销事件。
/// </summary>
public class ActorUnregisteredEvent : IGameEvent
{
    public int ActorId { get; set; }
    public Actor Actor { get; set; }
}

/// <summary>
/// 主观察目标切换事件。
/// </summary>
public class ObserveTargetChangedEvent : IGameEvent
{
    public int PreviousTargetId { get; set; }
    public int CurrentTargetId { get; set; }
    public Actor PreviousTarget { get; set; }
    public Actor CurrentTarget { get; set; }
}
