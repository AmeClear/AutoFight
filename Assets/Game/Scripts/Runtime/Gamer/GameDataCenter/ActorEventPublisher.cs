using System.Collections.Generic;
using GAS.Runtime;
using GameEvent;
using UnityEngine;

/// <summary>
/// 将 Actor 的 GAS 属性变化桥接到 EventBus。
/// </summary>
public static class ActorEventPublisher
{
    private class BindingContext
    {
        public int ActorId;
        public Actor Actor;
        public AbilitySystemComponent Asc;
        public readonly Dictionary<AttributeBase, AttributeCallbacks> AttributeCallbacksMap =
            new Dictionary<AttributeBase, AttributeCallbacks>();
    }

    private class AttributeCallbacks
    {
        public AttributeBase Attribute;
        public System.Action<AttributeBase, float, float> OnBaseChanged;
        public System.Action<AttributeBase, float, float> OnCurrentChanged;
    }

    private static readonly Dictionary<Actor, BindingContext> Bindings = new Dictionary<Actor, BindingContext>();

    public static void Bind(Actor actor, AbilitySystemComponent asc)
    {
        if (actor == null || asc == null)
            return;

        if (Bindings.ContainsKey(actor))
            return;

        var context = new BindingContext
        {
            ActorId = actor.GetInstanceID(),
            Actor = actor,
            Asc = asc
        };

        Bindings[actor] = context;
        RegisterAllAttributes(context);
        PublishRegisteredEvent(context);
    }

    public static void Unbind(Actor actor)
    {
        if (actor == null || !Bindings.TryGetValue(actor, out var context))
            return;

        UnregisterAllAttributes(context);
        Bindings.Remove(actor);

        EventBus.Publish(new ActorUnregisteredEvent
        {
            ActorId = context.ActorId,
            Actor = actor
        });
    }

    private static void RegisterAllAttributes(BindingContext context)
    {
        foreach (var attributeSet in context.Asc.AttributeSetContainer.Sets.Values)
        {
            foreach (var attributeName in attributeSet.AttributeNames)
            {
                var attribute = attributeSet[attributeName];
                if (attribute == null || context.AttributeCallbacksMap.ContainsKey(attribute))
                    continue;

                var callbacks = new AttributeCallbacks { Attribute = attribute };
                callbacks.OnBaseChanged = (attr, oldValue, newValue) =>
                    PublishAttributeChanged(context, attr, AttributeChangePhase.BaseValue, oldValue, newValue);
                callbacks.OnCurrentChanged = (attr, oldValue, newValue) =>
                    PublishAttributeChanged(context, attr, AttributeChangePhase.CurrentValue, oldValue, newValue);

                attribute.RegisterPostBaseValueChange(callbacks.OnBaseChanged);
                attribute.RegisterPostCurrentValueChange(callbacks.OnCurrentChanged);
                context.AttributeCallbacksMap[attribute] = callbacks;
            }
        }
    }

    private static void UnregisterAllAttributes(BindingContext context)
    {
        foreach (var pair in context.AttributeCallbacksMap)
        {
            var attribute = pair.Key;
            var callbacks = pair.Value;
            attribute.UnregisterPostBaseValueChange(callbacks.OnBaseChanged);
            attribute.UnregisterPostCurrentValueChange(callbacks.OnCurrentChanged);
        }

        context.AttributeCallbacksMap.Clear();
    }

    private static void PublishRegisteredEvent(BindingContext context)
    {
        var snapshots = new List<AttributeSnapshot>();
        foreach (var callbacks in context.AttributeCallbacksMap.Values)
            snapshots.Add(AttributeSnapshot.FromAttribute(callbacks.Attribute));

        EventBus.Publish(new ActorRegisteredEvent
        {
            ActorId = context.ActorId,
            Actor = context.Actor,
            InitialAttributes = snapshots.ToArray()
        });

        foreach (var snapshot in snapshots)
        {
            EventBus.Publish(new ActorAttributeChangedEvent
            {
                ActorId = context.ActorId,
                Actor = context.Actor,
                AttributeSetName = snapshot.AttributeSetName,
                AttributeName = snapshot.AttributeName,
                AttributeFullName = snapshot.AttributeFullName,
                Phase = AttributeChangePhase.Initialize,
                OldValue = snapshot.BaseValue,
                NewValue = snapshot.BaseValue,
                CurrentValue = snapshot.CurrentValue,
                BaseValue = snapshot.BaseValue,
                MinValue = snapshot.MinValue,
                MaxValue = snapshot.MaxValue
            });
        }
    }

    private static void PublishAttributeChanged(
        BindingContext context,
        AttributeBase attribute,
        AttributeChangePhase phase,
        float oldValue,
        float newValue)
    {
        EventBus.Publish(new ActorAttributeChangedEvent
        {
            ActorId = context.ActorId,
            Actor = context.Actor,
            AttributeSetName = attribute.SetName,
            AttributeName = attribute.ShortName,
            AttributeFullName = attribute.Name,
            Phase = phase,
            OldValue = oldValue,
            NewValue = newValue,
            CurrentValue = attribute.CurrentValue,
            BaseValue = attribute.BaseValue,
            MinValue = attribute.MinValue,
            MaxValue = attribute.MaxValue
        });
    }
}
