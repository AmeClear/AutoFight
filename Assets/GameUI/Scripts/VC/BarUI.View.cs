/*---------------------------------
 *Title:UI表现层脚本自动化生成工具
 *Date:2026/7/13 17:17:55
 *Description:UI 表现层，该层只负责界面的交互、表现相关的更新，不允许编写任何业务逻辑代码
 *注意:以下文件是自动生成的，会覆盖原有的代码
---------------------------------*/
using UnityEngine.UI;
using UnityEngine;
using GameEvent;
using GAS.Runtime;

public partial class BarUI : WindowBase
{

	public BarUIDataComponent dataComp;
	private EventSubscription _actorRegisteredSubscription;
	private EventSubscription _attributeChangedSubscription;

	#region 声明周期函数
	//调用机制与Mono Awake一致
	public override void OnAwake()
	{
		dataComp = gameObject.GetComponent<BarUIDataComponent>();
		dataComp.InitComponent(this);
		base.OnAwake();
	}
	//物体显示时执行
	public override void OnShow()
	{
		base.OnShow();
		RefreshFromMainTarget();
	}
	//物体隐藏时执行
	public override void OnHide()
	{
		base.OnHide();
	}
	//物体销毁时执行
	public override void OnDestroy()
	{
		_actorRegisteredSubscription?.Dispose();
		_actorRegisteredSubscription = null;
		_attributeChangedSubscription?.Dispose();
		_attributeChangedSubscription = null;
		base.OnDestroy();
	}
	protected override void OnUIListener()
	{
		_actorRegisteredSubscription = EventBus.Subscribe<ActorRegisteredEvent>(OnActorRegistered);
		_attributeChangedSubscription = EventBus.Subscribe<ActorAttributeChangedEvent>(OnActorAttributeChanged);
	}
	#endregion
	#region 表现变化
	private void OnActorRegistered(ActorRegisteredEvent e)
	{
		if (e?.InitialAttributes == null || dataComp == null)
			return;

		// 仅刷新当前主观察目标，避免其他 Actor 注册覆盖 HUD
		if (ActorObserverSystem.Instance.HasMainTarget &&
			!ActorObserverSystem.Instance.IsMainTarget(e.ActorId))
			return;

		RefreshBars(e.InitialAttributes);
	}

	private void OnActorAttributeChanged(ActorAttributeChangedEvent e)
	{
		if (e == null || dataComp == null)
			return;

		if (!ActorObserverSystem.Instance.IsMainTarget(e.ActorId))
			return;

		RefreshBarByAttribute(e.AttributeFullName, e.CurrentValue, e.MaxValue);
	}

	private void RefreshFromMainTarget()
	{
		var record = ActorObserverSystem.Instance.GetMainObserveRecord();
		if (record?.Attributes == null || record.Attributes.Count == 0)
			return;

		var snapshots = new AttributeSnapshot[record.Attributes.Count];
		var index = 0;
		foreach (var pair in record.Attributes)
			snapshots[index++] = pair.Value;

		RefreshBars(snapshots);
	}

	private void RefreshBars(AttributeSnapshot[] snapshots)
	{
		if (snapshots == null || dataComp == null)
			return;

		foreach (var snapshot in snapshots)
		{
			if (snapshot == null)
				continue;

			RefreshBarByAttribute(snapshot.AttributeFullName,
				snapshot.CurrentValue, snapshot.MaxValue);
		}
	}

	private void RefreshBarByAttribute(string attributeFullName, float current, float max)
	{
		if (attributeFullName == AS_Fight.Lookup.HealthValue)
		{
			SetBar(dataComp.HPFillImage, dataComp.HPTxtText, current, max);
			return;
		}

		if (attributeFullName == AS_Fight.Lookup.StamValue)
		{
			SetBar(dataComp.StaFillImage, dataComp.StaTxtText, current, max);
			return;
		}

		if (attributeFullName == AS_Fight.Lookup.DefProgress)
		{
			SetBar(dataComp.DefFillImage, dataComp.DefTxtText, current, max);
		}
	}

	private static void SetBar(Image fillImage, Text valueText, float current, float max)
	{
		float safeMax = Mathf.Max(max, 0.0001f);
		float fill = Mathf.Clamp01(current / safeMax);

		if (fillImage != null)
			fillImage.fillAmount = fill;

		if (valueText != null)
			valueText.text = $"{Mathf.CeilToInt(current)}/{Mathf.CeilToInt(max)}";
	}
	#endregion
	#region UI组件事件
	#endregion
}
