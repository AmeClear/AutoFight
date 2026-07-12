using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu(fileName = "NewRangeProfile", menuName = "Range System/Range Profile")]
public class RangeProfile : ScriptableObject
{
    public enum ShapeType
    {
        Circle,
        Sector,
        Rectangle,
        Ring,
        CustomPolygon
    }

    [TabGroup("基本设置", "形状")]
    [BoxGroup("基本设置/形状/类型"), LabelText("形状类型")]
    [Tooltip("选择技能范围的几何形状，不同形状有不同的参数需求")]
    [EnumToggleButtons]
    public ShapeType shapeType = ShapeType.Circle;

    [BoxGroup("基本设置/形状/尺寸"), LabelText("半径")]
    [Tooltip("技能范围的主要半径尺寸")]
    [MinValue(0.1), SuffixLabel("米", true)]
    public float radius = 5f;

    [BoxGroup("基本设置/形状/尺寸"), LabelText("内半径"), ShowIf("shapeType", ShapeType.Ring)]
    [Tooltip("环形范围的内半径，必须小于主半径")]
    [MinValue(0.1), MaxValue("@radius - 0.1")]
    [SuffixLabel("米", true)]
    public float innerRadius = 3f;

    [BoxGroup("基本设置/形状/尺寸"), LabelText("角度"), ShowIf("shapeType", ShapeType.Sector)]
    [Tooltip("扇形范围的角度，0-360度")]
    [Range(1, 360), SuffixLabel("度", true)]
    public float angle = 90f;

    [BoxGroup("基本设置/形状/尺寸"), LabelText("矩形尺寸"), ShowIf("shapeType", ShapeType.Rectangle)]
    [Tooltip("矩形范围的长宽尺寸")]
    [MinValue(0.1)]
    public Vector2 rectangleSize = new Vector2(5, 5);

    [BoxGroup("基本设置/形状/高级"), LabelText("分辨率")]
    [Tooltip("圆形/扇形/环形边缘的平滑度，值越高越平滑但性能消耗越大")]
    [Range(8, 128)]
    public int resolution = 64;

    [TabGroup("基本设置", "颜色")]
    [BoxGroup("基本设置/颜色/主色"), LabelText("主颜色")]
    [Tooltip("技能范围区域的主要颜色和透明度")]
    [ColorUsage(true, true)]
    public Color mainColor = new Color(1, 0, 0, 0.5f);

    [BoxGroup("基本设置/颜色/边缘"), LabelText("边缘颜色")]
    [Tooltip("技能范围边缘的颜色，通常比主色更亮或更暗")]
    public Color edgeColor = Color.red;

    [BoxGroup("基本设置/颜色/边缘"), LabelText("边缘宽度")]
    [Tooltip("边缘效果的宽度比例，0-1之间")]
    [Range(0, 0.5f)]
    public float edgeWidth = 0.2f;

    [BoxGroup("基本设置/颜色/边缘"), LabelText("边缘发光"), ToggleLeft]
    [Tooltip("是否启用边缘发光效果，使边缘更加醒目")]
    public bool edgeGlow = true;

    [BoxGroup("基本设置/颜色/边缘"), LabelText("发光强度"), ShowIf("edgeGlow")]
    [Tooltip("边缘发光效果的强度，值越大越亮")]
    [Range(1, 5)]
    public float edgeGlowIntensity = 2f;

    [TabGroup("视觉效果", "动画")]
    [BoxGroup("视觉效果/动画/脉冲"), LabelText("脉冲效果"), ToggleLeft]
    [Tooltip("是否启用脉冲动画效果，使技能范围有呼吸感")]
    public bool pulsate = false;

    [BoxGroup("视觉效果/动画/脉冲"), LabelText("脉冲速度"), ShowIf("pulsate")]
    [Tooltip("脉冲动画的速度，值越大脉冲越快")]
    [MinValue(0.1)]
    public float pulsateSpeed = 1f;

    [BoxGroup("视觉效果/动画/旋转"), LabelText("旋转效果"), ToggleLeft]
    [Tooltip("是否启用旋转动画效果，使技能范围绕中心旋转")]
    public bool rotate = false;

    [BoxGroup("视觉效果/动画/旋转"), LabelText("旋转速度"), ShowIf("rotate")]
    [Tooltip("旋转动画的速度，正值顺时针，负值逆时针")]
    public float rotateSpeed = 20f;

    [TabGroup("视觉效果", "纹理")]
    [BoxGroup("视觉效果/纹理/图案"), LabelText("图案纹理")]
    [Tooltip("应用于技能范围的图案纹理，可以创建特殊效果如魔法阵等")]
    [PreviewField(60, ObjectFieldAlignment.Left)]
    public Texture2D patternTexture;

    [BoxGroup("视觉效果/纹理/图案"), LabelText("图案平铺"), ShowIf("@patternTexture != null")]
    [Tooltip("图案纹理的平铺次数，值越大图案越小越密集")]
    [MinValue(0.1)]
    public float patternTiling = 1f;

    [TabGroup("扩展效果", "扩散效果")]
    [BoxGroup("扩展效果/扩散效果/设置"), LabelText("启用扩散效果"), ToggleLeft]
    [Tooltip("是否启用内圈向外圈扩散的动画效果")]
    public bool enableExpansion = false;

    [BoxGroup("扩展效果/扩散效果/设置"), LabelText("扩散时间"), ShowIf("enableExpansion")]
    [Tooltip("内圈扩散到外圈所需的时间（秒）")]
    [MinValue(0.1)]
    public float expansionDuration = 2f;

    [BoxGroup("扩展效果/扩散效果/设置"), LabelText("初始内圈比例"), ShowIf("enableExpansion")]
    [Tooltip("扩散开始时内圈相对于外圈的比例（0-1）")]
    [Range(0, 0.99f)]
    public float startInnerRatio = 0.2f;

    [BoxGroup("扩展效果/扩散效果/设置"), LabelText("循环扩散"), ShowIf("enableExpansion"), ToggleLeft]
    [Tooltip("是否循环播放扩散效果")]
    public bool loopExpansion = false;

    [BoxGroup("扩展效果/扩散效果/设置"), LabelText("扩散后保持"), ShowIf("enableExpansion"), HideIf("loopExpansion"), ToggleLeft]
    [Tooltip("扩散完成后是否保持最大范围")]
    public bool persistAfterExpansion = true;

    [BoxGroup("扩展效果/扩散效果/颜色"), LabelText("内圈颜色"), ShowIf("enableExpansion")]
    [Tooltip("扩散开始时内圈的颜色")]
    public Color innerColor = new Color(1, 0.5f, 0, 0.8f);

    [BoxGroup("扩展效果/扩散效果/颜色"), LabelText("颜色渐变"), ShowIf("enableExpansion"), ToggleLeft]
    [Tooltip("是否在扩散过程中颜色从内圈颜色渐变到主颜色")]
    public bool colorTransition = true;

    [BoxGroup("扩展效果/扩散效果/高级"), LabelText("使用曲线"), ShowIf("enableExpansion"), ToggleLeft]
    [Tooltip("使用曲线控制扩散速度")]
    public bool useExpansionCurve = false;

    [BoxGroup("扩展效果/扩散效果/高级"), LabelText("扩散曲线"), ShowIf("useExpansionCurve")]
    [Tooltip("控制扩散速度的曲线，X轴是时间(0-1)，Y轴是内圈比例(0-1)")]
    public AnimationCurve expansionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [TabGroup("高级设置", "材质")]
    [InfoBox("使用自定义材质可以完全覆盖默认的着色器效果", InfoMessageType.Info)]
    [BoxGroup("高级设置/材质/自定义"), LabelText("自定义材质")]
    [Tooltip("使用自定义材质替代默认的范围着色器，提供完全的控制权")]
    public Material customMaterial;

    [BoxGroup("高级设置/材质/预览"), HideLabel]
    [ShowInInspector, DisplayAsString, EnableGUI]
    //[InfoBox("当前使用的材质: @((customMaterial != null) ? customMaterial.name : \"默认范围着色器\")", visibleIfMemberName: "@customMaterial != null", InfoMessageType.None)]
    private string MaterialPreview => (customMaterial != null) ? $"使用自定义材质: {customMaterial.name}" : "使用默认范围着色器";

    // 辅助方法用于验证内半径
    private bool ValidateInnerRadius()
    {
        return shapeType != ShapeType.Ring || innerRadius < radius;
    }

    // 辅助方法用于获取当前形状的描述
    private string GetShapeDescription()
    {
        switch (shapeType)
        {
            case ShapeType.Circle:
                return $"圆形范围，半径: {radius}米";
            case ShapeType.Sector:
                return $"扇形范围，半径: {radius}米，角度: {angle}度";
            case ShapeType.Rectangle:
                return $"矩形范围，尺寸: {rectangleSize.x}x{rectangleSize.y}米";
            case ShapeType.Ring:
                return $"环形范围，外半径: {radius}米，内半径: {innerRadius}米";
            case ShapeType.CustomPolygon:
                return "自定义多边形范围";
            default:
                return "未知形状";
        }
    }

    [BoxGroup("基本设置/形状/预览"), HideLabel]
    [ShowInInspector, DisplayAsString, EnableGUI]
    private string ShapePreview => GetShapeDescription();

    // 按钮用于快速测试效果
    [Button(ButtonSizes.Medium), BoxGroup("测试")]
    [Tooltip("在场景中创建一个临时范围显示以预览效果")]
    private void PreviewInScene()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            Debug.Log("预览功能需要在播放模式下使用");
            return;
        }

        // 查找或创建范围系统管理器
        RangeSystemManager manager = RangeSystemManager.Instance;
        

        // 创建预览范围
        manager.CreateExpandingRange(this, Vector3.zero);

        //// 5秒后自动清除预览
        
        //manager.Invoke("ClearAllRanges", 5f);
#endif
    }

    [Button(ButtonSizes.Small), BoxGroup("测试")]
    [Tooltip("重置所有设置为默认值")]
    private void ResetToDefaults()
    {
        shapeType = ShapeType.Circle;
        radius = 5f;
        innerRadius = 3f;
        angle = 90f;
        rectangleSize = new Vector2(5, 5);
        resolution = 64;
        mainColor = new Color(1, 0, 0, 0.5f);
        edgeColor = Color.red;
        edgeWidth = 0.2f;
        edgeGlow = true;
        edgeGlowIntensity = 2f;
        pulsate = false;
        pulsateSpeed = 1f;
        rotate = false;
        rotateSpeed = 20f;
        patternTexture = null;
        patternTiling = 1f;
        customMaterial = null;
        enableExpansion = false;
        expansionDuration = 2f;
        startInnerRatio = 0.2f;
        loopExpansion = false;
        persistAfterExpansion = true;
        innerColor = new Color(1, 0.5f, 0, 0.8f);
        colorTransition = true;
        useExpansionCurve = false;
        expansionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    }
}