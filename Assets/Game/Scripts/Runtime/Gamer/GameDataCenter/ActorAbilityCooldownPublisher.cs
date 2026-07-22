using System.Collections.Generic;
using GAS.Runtime;
using GameEvent;
using UnityEngine;

/// <summary>
/// 将 Actor 技能冷却状态桥接到 EventBus，供 UI 等模块解耦订阅。
/// </summary>
public static class ActorAbilityCooldownPublisher
{
    private class AbilityCooldownState
    {
        public float TimeRemaining = -1f;
        public float Duration;
        public bool IsReady = true;
    }

    private class BindingContext
    {
        public int ActorId;
        public Actor Actor;
        public AbilitySystemComponent Asc;
        public readonly Dictionary<string, AbilityCooldownState> LastStates =
            new Dictionary<string, AbilityCooldownState>();
    }

    private class TickerBehaviour : MonoBehaviour
    {
        private void Update()
        {
            Tick();
        }
    }

    private static readonly Dictionary<Actor, BindingContext> Bindings = new Dictionary<Actor, BindingContext>();
    private static TickerBehaviour _ticker;
    private const float PublishEpsilon = 0.01f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState()
    {
        Bindings.Clear();
        _ticker = null;
    }

    /// <summary>
    /// 绑定 Actor，开始监听并广播其技能冷却。
    /// </summary>
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
        EnsureTicker();
        PublishAll(context, force: true);
    }

    /// <summary>
    /// 解绑 Actor，停止冷却广播。
    /// </summary>
    public static void Unbind(Actor actor)
    {
        if (actor == null)
            return;

        Bindings.Remove(actor);
    }

    /// <summary>
    /// 查询指定 Actor 某技能当前冷却。
    /// </summary>
    public static bool TryGetCooldown(Actor actor, string abilityName, out CooldownTimer timer)
    {
        timer = default;
        if (actor == null || string.IsNullOrEmpty(abilityName))
            return false;

        if (!Bindings.TryGetValue(actor, out var context))
            return false;

        return TryReadCooldown(context.Asc, abilityName, out timer);
    }

    /// <summary>
    /// 主动推送指定 Actor 的全部技能冷却（例如 UI OnShow）。
    /// </summary>
    public static void PublishCurrent(Actor actor)
    {
        if (actor == null || !Bindings.TryGetValue(actor, out var context))
            return;

        PublishAll(context, force: true);
    }

    private static void EnsureTicker()
    {
        if (_ticker != null)
            return;

        var go = new GameObject("[ActorAbilityCooldownPublisher]");
        Object.DontDestroyOnLoad(go);
        _ticker = go.AddComponent<TickerBehaviour>();
    }

    private static void Tick()
    {
        if (Bindings.Count == 0)
            return;

        // 拷贝键，避免遍历中 Unbind 修改字典
        _tickBuffer.Clear();
        foreach (var pair in Bindings)
            _tickBuffer.Add(pair.Value);

        for (var i = 0; i < _tickBuffer.Count; i++)
            PublishAll(_tickBuffer[i], force: false);
    }

    private static readonly List<BindingContext> _tickBuffer = new List<BindingContext>();

    private static void PublishAll(BindingContext context, bool force)
    {
        if (context?.Asc == null || context.Actor == null)
            return;

        var specs = context.Asc.AbilityContainer?.AbilitySpecs();
        if (specs == null || specs.Count == 0)
            return;

        foreach (var kv in specs)
        {
            var abilityName = kv.Key;
            if (!TryReadCooldown(context.Asc, abilityName, out var timer))
                continue;

            if (!context.LastStates.TryGetValue(abilityName, out var state))
            {
                state = new AbilityCooldownState();
                context.LastStates[abilityName] = state;
            }

            var isReady = timer.TimeRemaining <= 0f;
            var changed = force
                          || state.IsReady != isReady
                          || Mathf.Abs(state.TimeRemaining - timer.TimeRemaining) >= PublishEpsilon
                          || Mathf.Abs(state.Duration - timer.Duration) >= PublishEpsilon;

            if (!changed)
                continue;

            state.TimeRemaining = timer.TimeRemaining;
            state.Duration = timer.Duration;
            state.IsReady = isReady;

            EventBus.Publish(new ActorAbilityCooldownChangedEvent
            {
                ActorId = context.ActorId,
                Actor = context.Actor,
                AbilityName = abilityName,
                TimeRemaining = Mathf.Max(0f, timer.TimeRemaining),
                Duration = Mathf.Max(0f, timer.Duration)
            });
        }
    }

    private static bool TryReadCooldown(AbilitySystemComponent asc, string abilityName, out CooldownTimer timer)
    {
        timer = default;
        if (asc?.AbilityContainer == null || string.IsNullOrEmpty(abilityName))
            return false;

        if (!asc.AbilityContainer.AbilitySpecs().TryGetValue(abilityName, out var spec) || spec?.Ability == null)
            return false;

        if (spec.Ability.Cooldown == null)
        {
            timer = new CooldownTimer
            {
                TimeRemaining = 0f,
                Duration = spec.Ability.CooldownTime
            };
            return true;
        }

        timer = asc.CheckCooldownFromTags(spec.Ability.Cooldown.TagContainer.GrantedTags);
        if (timer.Duration <= 0f && spec.Ability.CooldownTime > 0f)
            timer.Duration = spec.Ability.CooldownTime;

        return true;
    }
}
