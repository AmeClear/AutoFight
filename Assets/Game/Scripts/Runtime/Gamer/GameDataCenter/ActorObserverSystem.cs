using System.Collections.Generic;
using GameEvent;
using UnityEngine;

/// <summary>
/// Actor 观察系统，维护已注册 Actor ID 列表与主观察目标。
/// </summary>
public class ActorObserverSystem
{
    public const int InvalidTargetId = 0;

    private static ActorObserverSystem _instance;

    public static ActorObserverSystem Instance => _instance ??= CreateInstance();

    private readonly List<int> _registeredActorIds = new List<int>();
    private readonly HashSet<int> _registeredActorIdSet = new HashSet<int>();

    private EventSubscription _registeredSubscription;
    private EventSubscription _unregisteredSubscription;

    /// <summary>
    /// 当前主观察目标的 Actor ID，无效时为 <see cref="InvalidTargetId"/>。
    /// </summary>
    public int MainObserveTargetId { get; private set; } = InvalidTargetId;

    public bool HasMainTarget => MainObserveTargetId != InvalidTargetId;

    public IReadOnlyList<int> RegisteredActorIds => _registeredActorIds;

    private static ActorObserverSystem CreateInstance()
    {
        var system = new ActorObserverSystem();
        system.SubscribeEvents();
        return system;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState()
    {
        _instance?.Dispose();
        _instance = null;
    }

    private void SubscribeEvents()
    {
        _registeredSubscription = EventBus.Subscribe<ActorRegisteredEvent>(OnActorRegistered);
        _unregisteredSubscription = EventBus.Subscribe<ActorUnregisteredEvent>(OnActorUnregistered);
    }

    private void Dispose()
    {
        _registeredSubscription?.Dispose();
        _unregisteredSubscription?.Dispose();
    }

    public bool IsRegistered(int actorId)
    {
        return actorId != InvalidTargetId && _registeredActorIdSet.Contains(actorId);
    }

    public bool IsMainTarget(int actorId)
    {
        return actorId != InvalidTargetId && MainObserveTargetId == actorId;
    }

    /// <summary>
    /// 切换主观察目标。
    /// </summary>
    public bool SwitchObserveTarget(int actorId)
    {
        if (actorId == InvalidTargetId)
        {
            ClearObserveTarget();
            return true;
        }

        if (!IsRegistered(actorId))
        {
            Debug.LogWarning($"[ActorObserverSystem] 切换观察目标失败，ActorId={actorId} 未注册。");
            return false;
        }

        if (MainObserveTargetId == actorId)
            return true;

        var previousTargetId = MainObserveTargetId;
        MainObserveTargetId = actorId;

        PublishTargetChanged(previousTargetId, actorId);
        return true;
    }

    /// <summary>
    /// 切换主观察目标。
    /// </summary>
    public bool SwitchObserveTarget(Actor actor)
    {
        if (actor == null)
        {
            ClearObserveTarget();
            return true;
        }

        return SwitchObserveTarget(actor.GetInstanceID());
    }

    /// <summary>
    /// 清空主观察目标。
    /// </summary>
    public void ClearObserveTarget()
    {
        if (MainObserveTargetId == InvalidTargetId)
            return;

        var previousTargetId = MainObserveTargetId;
        MainObserveTargetId = InvalidTargetId;
        PublishTargetChanged(previousTargetId, InvalidTargetId);
    }

    /// <summary>
    /// 获取当前主观察目标 Actor。
    /// </summary>
    public Actor GetMainObserveTarget()
    {
        return GameDataCenter.Instance.GetActorRecord(MainObserveTargetId)?.Actor;
    }

    /// <summary>
    /// 获取当前主观察目标的数据记录。
    /// </summary>
    public ActorDataRecord GetMainObserveRecord()
    {
        return GameDataCenter.Instance.GetActorRecord(MainObserveTargetId);
    }

    private void OnActorRegistered(ActorRegisteredEvent evt)
    {
        if (evt.Actor == null || evt.ActorId == InvalidTargetId)
            return;

        if (_registeredActorIdSet.Add(evt.ActorId))
            _registeredActorIds.Add(evt.ActorId);

        if (!HasMainTarget)
            SwitchObserveTarget(evt.ActorId);
    }

    private void OnActorUnregistered(ActorUnregisteredEvent evt)
    {
        if (evt.ActorId == InvalidTargetId)
            return;

        if (!_registeredActorIdSet.Remove(evt.ActorId))
            return;

        _registeredActorIds.Remove(evt.ActorId);

        if (MainObserveTargetId != evt.ActorId)
            return;

        if (_registeredActorIds.Count > 0)
            SwitchObserveTarget(_registeredActorIds[0]);
        else
            ClearObserveTarget();
    }

    private void PublishTargetChanged(int previousTargetId, int currentTargetId)
    {
        EventBus.Publish(new ObserveTargetChangedEvent
        {
            PreviousTargetId = previousTargetId,
            CurrentTargetId = currentTargetId,
            PreviousTarget = GameDataCenter.Instance.GetActorRecord(previousTargetId)?.Actor,
            CurrentTarget = GameDataCenter.Instance.GetActorRecord(currentTargetId)?.Actor
        });
    }
}
