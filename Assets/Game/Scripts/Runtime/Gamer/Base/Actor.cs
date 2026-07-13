using GAS.Runtime;
using UnityEngine;

public class Actor : MonoBehaviour
{
    protected AbilitySystemComponent asc;
    protected MoveComponent moveComponent;
    protected ActorBar healthBar;

    private void Awake()
    {
        asc = GetComponent<AbilitySystemComponent>();
        moveComponent = GetComponent<MoveComponent>();
        healthBar = GetComponent<ActorBar>();
        Init();
    }

    protected virtual void Start()
    {
        RefreshStatusBars();
    }

    protected virtual void Init()
    {
        asc.InitWithPreset(1);
        InitAttribute();
    }

    protected virtual void InitAttribute()
    {
        asc.AttrSet<AS_Fight>().InitAttackValue(10);

        asc.AttrSet<AS_Fight>().HealthValue.RegisterPostBaseValueChange(OnHpChange);
        asc.AttrSet<AS_Fight>().StamValue.RegisterPostBaseValueChange(OnStamChange);
        asc.AttrSet<AS_Fight>().DefProgress.RegisterPostBaseValueChange(OnDefChange);

        _ = GameDataCenter.Instance;
        _ = ActorObserverSystem.Instance;
        ActorEventPublisher.Bind(this, asc);
    }

    protected virtual void OnDestroy()
    {
        ActorEventPublisher.Unbind(this);
    }

    protected virtual void OnHpChange(AttributeBase attributeBase, float oldValue, float newValue)
    {
        if (healthBar == null)
            return;

        healthBar.SetHealth(attributeBase.CurrentValue, attributeBase.MaxValue);
    }

    protected void RefreshStatusBars()
    {
        if (healthBar == null)
            return;

        AS_Fight fight = asc.AttrSet<AS_Fight>();
        healthBar.SetHealth(fight.HealthValue.CurrentValue, fight.HealthValue.MaxValue);
        healthBar.SetStamina(fight.StamValue.CurrentValue, fight.StamValue.MaxValue);
        healthBar.SetDefense(fight.DefProgress.CurrentValue, fight.DefProgress.MaxValue);
    }

    protected void RefreshHealthBar()
    {
        RefreshStatusBars();
    }

    protected virtual void OnStamChange(AttributeBase attributeBase, float oldValue, float newValue)
    {
        if (healthBar == null)
            return;

        healthBar.SetStamina(attributeBase.CurrentValue, attributeBase.MaxValue);
    }

    protected virtual void OnDefChange(AttributeBase attributeBase, float oldValue, float newValue)
    {
        if (healthBar == null)
            return;

        healthBar.SetDefense(attributeBase.CurrentValue, attributeBase.MaxValue);
    }
}
