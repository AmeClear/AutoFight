/*---------------------------------
 *Title:UI表现层脚本自动化生成工具
 *Date:2026/7/13 17:17:55
 *Description:UI 表现层，该层只负责界面的交互、表现相关的更新，不允许编写任何业务逻辑代码
 *注意:以下文件是自动生成的，会覆盖原有的代码
---------------------------------*/
using UnityEngine.UI;
using UnityEngine;
using GameEvent;

public partial class BarUI:WindowBase
{
	
		 public BarUIDataComponent dataComp;
	
		 #region 声明周期函数
		 //调用机制与Mono Awake一致
		 public override void OnAwake()
		 {
			 dataComp=gameObject.GetComponent<BarUIDataComponent>();
			 dataComp.InitComponent(this);
			 base.OnAwake();
		 }
		 //物体显示时执行
		 public override void OnShow()
		 {
			 base.OnShow();
		 }
		 //物体隐藏时执行
		 public override void OnHide()
		 {
			 base.OnHide();
		 }
		 //物体销毁时执行
		 public override void OnDestroy()
		 {
			 base.OnDestroy();
		 }
    protected override void OnUIListener()
    {
        EventBus.Subscribe<ActorRegisteredEvent>(OnActorRegistered);
    }
    #endregion
    #region 表现变化
	private void OnActorRegistered(ActorRegisteredEvent e)
    { 

    }
    #endregion
    #region UI组件事件
    #endregion
}
