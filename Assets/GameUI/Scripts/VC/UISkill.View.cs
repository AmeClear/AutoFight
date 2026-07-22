/*---------------------------------
 *Title:UI表现层脚本自动化生成工具
 *Date:2026/7/22 18:58:34
 *Description:UI 表现层，该层只负责界面的交互、表现相关的更新，不允许编写任何业务逻辑代码
 *注意:以下文件是自动生成的，会覆盖原有的代码
---------------------------------*/
using UnityEngine.UI;
using UnityEngine;
using GameEvent;
using GAS.Runtime;

public partial class UISkill : WindowBase
{
	public UISkillDataComponent dataComp;

	private EventSubscription _cooldownSubscription;
	private EventSubscription _observeTargetSubscription;
	private EventSubscription _actorRegisteredSubscription;

	#region 声明周期函数
	//调用机制与Mono Awake一致
	public override void OnAwake()
	{
		dataComp = gameObject.GetComponent<UISkillDataComponent>();
		dataComp.InitComponent(this);
		base.OnAwake();
	}

	//物体显示时执行
	public override void OnShow()
	{
		base.OnShow();
		RefreshAtkCooldownFromMainTarget();
	}

	//物体隐藏时执行
	public override void OnHide()
	{
		base.OnHide();
	}

	//物体销毁时执行
	public override void OnDestroy()
	{
		_cooldownSubscription?.Dispose();
		_cooldownSubscription = null;
		_observeTargetSubscription?.Dispose();
		_observeTargetSubscription = null;
		_actorRegisteredSubscription?.Dispose();
		_actorRegisteredSubscription = null;
		base.OnDestroy();
	}

	protected override void OnUIListener()
	{
		_cooldownSubscription = EventBus.Subscribe<ActorAbilityCooldownChangedEvent>(OnAbilityCooldownChanged);
		_observeTargetSubscription = EventBus.Subscribe<ObserveTargetChangedEvent>(OnObserveTargetChanged);
		_actorRegisteredSubscription = EventBus.Subscribe<ActorRegisteredEvent>(OnActorRegistered);
	}
	#endregion

	#region 表现变化
	private void OnActorRegistered(ActorRegisteredEvent e)
	{
		if (e == null)
			return;

		if (ActorObserverSystem.Instance.HasMainTarget &&
			!ActorObserverSystem.Instance.IsMainTarget(e.ActorId))
			return;

		RefreshAtkCooldownFromActor(e.Actor);
	}

	private void OnObserveTargetChanged(ObserveTargetChangedEvent e)
	{
		RefreshAtkCooldownFromActor(e?.CurrentTarget);
	}

	private void OnAbilityCooldownChanged(ActorAbilityCooldownChangedEvent e)
	{
		if (e == null || dataComp == null)
			return;

		if (!ActorObserverSystem.Instance.IsMainTarget(e.ActorId))
			return;

		if (e.AbilityName != GAbilityLib.Atk.Name)
			return;

		SetCooldownFill(e.TimeRemaining, e.Duration);
	}

	private void RefreshAtkCooldownFromMainTarget()
	{
		RefreshAtkCooldownFromActor(ActorObserverSystem.Instance.GetMainObserveTarget());
	}

	private void RefreshAtkCooldownFromActor(Actor actor)
	{
		if (actor == null || dataComp == null)
		{
			SetCooldownFill(0f, 0f);
			return;
		}

		if (ActorAbilityCooldownPublisher.TryGetCooldown(actor, GAbilityLib.Atk.Name, out var timer))
		{
			SetCooldownFill(timer.TimeRemaining, timer.Duration);
			return;
		}

		ActorAbilityCooldownPublisher.PublishCurrent(actor);
	}

	/// <summary>
	/// CD 遮罩：刚进入冷却为满，随剩余时间降到 0。
	/// </summary>
	private void SetCooldownFill(float timeRemaining, float duration)
	{
		if (dataComp?.FillImage == null)
			return;

		if (timeRemaining <= 0f || duration <= 0f)
		{
			dataComp.FillImage.fillAmount = 0f;
			return;
		}

		dataComp.FillImage.fillAmount = Mathf.Clamp01(timeRemaining / duration);
	}
	#endregion

	#region UI组件事件
	#endregion
}
