using GAS.Runtime;
using UnityEngine;

public class Actor : MonoBehaviour
{
    protected AbilitySystemComponent asc;
    protected MoveComponent moveComponent;
    protected WeaponHolder weaponHolder;

    public WeaponHolder WeaponHolder => weaponHolder;

    private void Awake()
    {
        asc = GetComponent<AbilitySystemComponent>();
        moveComponent = GetComponent<MoveComponent>();
        weaponHolder = GetComponent<WeaponHolder>();
        Init();
    }

    protected virtual void Start()
    {
    }

    protected virtual void Init()
    {
        asc.InitWithPreset(1);
        InitAttribute();
    }

    protected virtual void InitAttribute()
    {
        

        _ = GameDataCenter.Instance;
        _ = ActorObserverSystem.Instance;
        ActorEventPublisher.Bind(this, asc);
        ActorAbilityCooldownPublisher.Bind(this, asc);
    }

    protected virtual void OnDestroy()
    {
        ActorAbilityCooldownPublisher.Unbind(this);
        ActorEventPublisher.Unbind(this);
    }

    protected virtual void OnHpChange(AttributeBase attributeBase, float oldValue, float newValue)
    {
       
    }

    
}
