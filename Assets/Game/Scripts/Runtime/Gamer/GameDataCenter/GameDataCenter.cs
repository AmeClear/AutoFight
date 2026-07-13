using System.Collections.Generic;
using GameEvent;
using UnityEngine;

/// <summary>
/// 游戏数据管理中心，记录并管理每个 Actor 的属性初始化与变化历史。
/// </summary>
public class GameDataCenter
{
    public const int DefaultMaxHistoryPerActor = 256;

    private static GameDataCenter _instance;

    public static GameDataCenter Instance => _instance ??= CreateInstance();

    private readonly Dictionary<int, ActorDataRecord> _actorRecords = new Dictionary<int, ActorDataRecord>();
    private readonly Dictionary<Actor, int> _actorLookup = new Dictionary<Actor, int>();

    private EventSubscription _registeredSubscription;
    private EventSubscription _changedSubscription;
    private EventSubscription _unregisteredSubscription;

    public int MaxHistoryPerActor { get; set; } = DefaultMaxHistoryPerActor;

    public IReadOnlyDictionary<int, ActorDataRecord> ActorRecords => _actorRecords;

    private static GameDataCenter CreateInstance()
    {
        var center = new GameDataCenter();
        center.SubscribeEvents();
        return center;
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
        _changedSubscription = EventBus.Subscribe<ActorAttributeChangedEvent>(OnActorAttributeChanged);
        _unregisteredSubscription = EventBus.Subscribe<ActorUnregisteredEvent>(OnActorUnregistered);
    }

    private void Dispose()
    {
        _registeredSubscription?.Dispose();
        _changedSubscription?.Dispose();
        _unregisteredSubscription?.Dispose();
    }

    public ActorDataRecord GetActorRecord(int actorId)
    {
        _actorRecords.TryGetValue(actorId, out var record);
        return record;
    }

    public ActorDataRecord GetActorRecord(Actor actor)
    {
        if (actor == null)
            return null;

        return _actorLookup.TryGetValue(actor, out var actorId)
            ? GetActorRecord(actorId)
            : null;
    }

    public IReadOnlyList<AttributeChangeRecord> GetChangeHistory(int actorId, string attributeFullName = null)
    {
        var record = GetActorRecord(actorId);
        return record?.GetHistory(attributeFullName) ?? System.Array.Empty<AttributeChangeRecord>();
    }

    public AttributeSnapshot GetAttribute(int actorId, string attributeFullName)
    {
        return GetActorRecord(actorId)?.GetAttribute(attributeFullName);
    }

    private void OnActorRegistered(ActorRegisteredEvent evt)
    {
        if (evt.Actor == null)
            return;

        var record = new ActorDataRecord
        {
            ActorId = evt.ActorId,
            Actor = evt.Actor,
            ActorName = evt.Actor.name,
            RegisterTime = Time.time,
            IsActive = true
        };

        if (evt.InitialAttributes != null)
        {
            foreach (var snapshot in evt.InitialAttributes)
            {
                record.Attributes[snapshot.AttributeFullName] = snapshot;
                AppendChangeRecord(record, AttributeChangeRecord.Create(
                    evt.ActorId,
                    snapshot.AttributeFullName,
                    AttributeChangePhase.Initialize,
                    snapshot.BaseValue,
                    snapshot.BaseValue));
            }
        }

        _actorRecords[evt.ActorId] = record;
        _actorLookup[evt.Actor] = evt.ActorId;
    }

    private void OnActorAttributeChanged(ActorAttributeChangedEvent evt)
    {
        if (evt.Actor == null)
            return;

        if (!_actorRecords.TryGetValue(evt.ActorId, out var record))
        {
            record = new ActorDataRecord
            {
                ActorId = evt.ActorId,
                Actor = evt.Actor,
                ActorName = evt.Actor.name,
                RegisterTime = Time.time,
                IsActive = true
            };
            _actorRecords[evt.ActorId] = record;
            _actorLookup[evt.Actor] = evt.ActorId;
        }

        if (!record.Attributes.TryGetValue(evt.AttributeFullName, out var snapshot))
        {
            snapshot = new AttributeSnapshot
            {
                AttributeSetName = evt.AttributeSetName,
                AttributeName = evt.AttributeName,
                AttributeFullName = evt.AttributeFullName
            };
            record.Attributes[evt.AttributeFullName] = snapshot;
        }

        snapshot.BaseValue = evt.BaseValue;
        snapshot.CurrentValue = evt.CurrentValue;
        snapshot.MinValue = evt.MinValue;
        snapshot.MaxValue = evt.MaxValue;

        if (evt.Phase == AttributeChangePhase.Initialize)
            return;

        AppendChangeRecord(record, AttributeChangeRecord.Create(
            evt.ActorId,
            evt.AttributeFullName,
            evt.Phase,
            evt.OldValue,
            evt.NewValue));
    }

    private void OnActorUnregistered(ActorUnregisteredEvent evt)
    {
        if (!_actorRecords.TryGetValue(evt.ActorId, out var record))
            return;

        record.IsActive = false;
        record.Actor = null;

        if (evt.Actor != null)
            _actorLookup.Remove(evt.Actor);
    }

    private void AppendChangeRecord(ActorDataRecord record, AttributeChangeRecord changeRecord)
    {
        record.ChangeHistory.Add(changeRecord);

        var overflow = record.ChangeHistory.Count - MaxHistoryPerActor;
        if (overflow <= 0)
            return;

        record.ChangeHistory.RemoveRange(0, overflow);
    }
}
