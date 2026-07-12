using UnityEngine;
using System.Collections;

[ExecuteInEditMode]
public class RangeRenderer : MonoBehaviour
{
    [SerializeField] private RangeProfile rangeProfile;
    [SerializeField] private MeshFilter meshFilter;
    [SerializeField] private MeshRenderer meshRenderer;

    private Material rangeMaterial;
    private Mesh currentMesh;

    // 扩散效果相关
    private bool isExpanding = false;
    private float expansionProgress = 0f;
    private Coroutine expansionCoroutine;

    // Shader属性ID缓存
    private static readonly int MainColorID = Shader.PropertyToID("_MainColor");
    private static readonly int EdgeColorID = Shader.PropertyToID("_EdgeColor");
    private static readonly int EdgeWidthID = Shader.PropertyToID("_EdgeWidth");
    private static readonly int PulsateSpeedID = Shader.PropertyToID("_PulsateSpeed");
    private static readonly int RotateSpeedID = Shader.PropertyToID("_RotateSpeed");
    private static readonly int GlowIntensityID = Shader.PropertyToID("_GlowIntensity");
    private static readonly int PatternTexID = Shader.PropertyToID("_PatternTex");
    private static readonly int PatternTilingID = Shader.PropertyToID("_PatternTiling");
    private static readonly int ExpansionProgressID = Shader.PropertyToID("_ExpansionProgress");
    private static readonly int InnerColorID = Shader.PropertyToID("_InnerColor");
    private static readonly int UseColorTransitionID = Shader.PropertyToID("_UseColorTransition");

    void OnEnable()
    {
        InitializeComponents();
        UpdateRangeDisplay();
    }

    void OnDisable()
    {
        if (rangeMaterial != null && Application.isEditor)
        {
            DestroyImmediate(rangeMaterial);
        }
    }

    void Update()
    {
        if (rangeProfile != null)
        {
            UpdateMaterialProperties();

#if UNITY_EDITOR
            if (!Application.isPlaying && rangeProfile.enableExpansion)
            {
                UpdateExpansionEffect();
            }
#endif
        }
    }

    private void InitializeComponents()
    {
        if (meshFilter == null) meshFilter = GetComponent<MeshFilter>();
        if (meshRenderer == null) meshRenderer = GetComponent<MeshRenderer>();

        if (meshFilter == null) meshFilter = gameObject.AddComponent<MeshFilter>();
        if (meshRenderer == null) meshRenderer = gameObject.AddComponent<MeshRenderer>();
    }

    public void SetProfile(RangeProfile profile)
    {
        rangeProfile = profile;
        UpdateRangeDisplay();
    }

    private void UpdateRangeDisplay()
    {
        if (rangeProfile == null) return;

        GenerateMesh();

        if (rangeProfile.customMaterial != null)
        {
            meshRenderer.material = new Material(rangeProfile.customMaterial);
        }
        else
        {
            if (rangeMaterial == null)
            {
                Shader rangeShader = Shader.Find("Custom/RangeShader");
                if (rangeShader == null)
                {
                    Debug.LogError("RangeShader not found! Using default transparent shader.");
                    rangeShader = Shader.Find("Transparent/Diffuse");
                }
                rangeMaterial = new Material(rangeShader);
            }
            meshRenderer.material = rangeMaterial;
        }

        UpdateMaterialProperties();
    }

    private void GenerateMesh()
    {
        if (rangeProfile == null) return;

        switch (rangeProfile.shapeType)
        {
            case RangeProfile.ShapeType.Circle:
                currentMesh = RangeMeshGenerator.GenerateCircle(rangeProfile.radius, rangeProfile.resolution);
                break;
            case RangeProfile.ShapeType.Sector:
                currentMesh = RangeMeshGenerator.GenerateSector(rangeProfile.radius, rangeProfile.angle, rangeProfile.resolution);
                break;
            case RangeProfile.ShapeType.Ring:
                currentMesh = RangeMeshGenerator.GenerateRing(rangeProfile.radius, rangeProfile.innerRadius, rangeProfile.resolution);
                break;
            case RangeProfile.ShapeType.Rectangle:
                currentMesh = RangeMeshGenerator.GenerateRectangle(rangeProfile.rectangleSize);
                break;
            case RangeProfile.ShapeType.CustomPolygon:
                // 暂未实现，使用圆形替代
                currentMesh = RangeMeshGenerator.GenerateCircle(rangeProfile.radius, rangeProfile.resolution);
                break;
        }

        if (currentMesh != null)
        {
            meshFilter.mesh = currentMesh;
        }
    }

    public void PrepareForReuse()
    {
        StopExpansion();
        expansionProgress = 0f;
        isExpanding = false;
        rangeProfile = null;

        if (meshRenderer != null)
        {
            Material instanceMaterial = meshRenderer.material;
            meshRenderer.sharedMaterial = null;
            if (instanceMaterial != null)
            {
                if (Application.isPlaying)
                    Destroy(instanceMaterial);
                else
                    DestroyImmediate(instanceMaterial);
            }
        }

        rangeMaterial = null;

        if (currentMesh != null)
        {
            if (Application.isPlaying)
                Destroy(currentMesh);
            else
                DestroyImmediate(currentMesh);
            currentMesh = null;
        }

        if (meshFilter != null)
            meshFilter.sharedMesh = null;

        gameObject.SetActive(false);
    }

    private void UpdateMaterialProperties()
    {
        Material mat = meshRenderer.material;
        if (mat == null) return;

        mat.SetColor(MainColorID, rangeProfile.mainColor);
        mat.SetColor(EdgeColorID, rangeProfile.edgeColor);
        mat.SetFloat(EdgeWidthID, rangeProfile.edgeWidth);
        mat.SetFloat(PulsateSpeedID, rangeProfile.pulsate ? rangeProfile.pulsateSpeed : 0);
        mat.SetFloat(RotateSpeedID, rangeProfile.rotate ? rangeProfile.rotateSpeed : 0);
        mat.SetFloat(GlowIntensityID, rangeProfile.edgeGlow ? rangeProfile.edgeGlowIntensity : 1);

        if (rangeProfile.patternTexture != null)
        {
            mat.SetTexture(PatternTexID, rangeProfile.patternTexture);
            mat.SetFloat(PatternTilingID, rangeProfile.patternTiling);
        }

        // 扩散效果属性
        if (rangeProfile.enableExpansion)
        {
            mat.SetFloat(ExpansionProgressID, expansionProgress);
            mat.SetColor(InnerColorID, rangeProfile.innerColor);
            mat.SetFloat(UseColorTransitionID, rangeProfile.colorTransition ? 1 : 0);
        }
        else
        {
            mat.SetFloat(ExpansionProgressID, 1f); // 不启用扩散时显示完整范围
        }
    }

    // 开始扩散效果
    public void StartExpansion()
    {
        if (rangeProfile == null || !rangeProfile.enableExpansion) return;

        if (expansionCoroutine != null)
        {
            StopCoroutine(expansionCoroutine);
        }

        expansionCoroutine = StartCoroutine(ExpansionRoutine());
    }

    // 停止扩散效果
    public void StopExpansion()
    {
        if (expansionCoroutine != null)
        {
            StopCoroutine(expansionCoroutine);
            expansionCoroutine = null;
        }

        isExpanding = false;

        if (rangeProfile != null && !rangeProfile.persistAfterExpansion)
        {
            expansionProgress = rangeProfile.startInnerRatio;
            UpdateMaterialProperties();
        }
    }

    // 重置扩散状态（回到起始内圈比例）
    public void ResetExpansion()
    {
        if (rangeProfile == null) return;

        expansionProgress = rangeProfile.startInnerRatio;
        UpdateMaterialProperties();
    }

    // 手动设置扩散进度 (0..1)
    public void SetExpansionProgress(float progress)
    {
        if (rangeProfile != null && rangeProfile.enableExpansion)
        {
            expansionProgress = Mathf.Clamp01(progress);
            expansionProgress = rangeProfile.startInnerRatio + expansionProgress * (1 - rangeProfile.startInnerRatio);
            UpdateMaterialProperties();
        }
    }

    private IEnumerator ExpansionRoutine()
    {
        isExpanding = true;
        float startTime = Time.time;
        float targetEndRatio = 1f; // 最终比例为1（全尺寸）

        do
        {
            float elapsed = Time.time - startTime;
            float t = Mathf.Clamp01(elapsed / rangeProfile.expansionDuration);

            if (rangeProfile.useExpansionCurve)
            {
                t = rangeProfile.expansionCurve.Evaluate(t);
            }

            expansionProgress = Mathf.Lerp(rangeProfile.startInnerRatio, targetEndRatio, t);
            UpdateMaterialProperties();
            yield return null;

            if (elapsed >= rangeProfile.expansionDuration && !rangeProfile.loopExpansion)
            {
                break;
            }

            if (elapsed >= rangeProfile.expansionDuration && rangeProfile.loopExpansion)
            {
                startTime = Time.time;
            }

        } while (isExpanding && rangeProfile.loopExpansion);

        isExpanding = false;
        expansionCoroutine = null;
    }

#if UNITY_EDITOR
    private void UpdateExpansionEffect()
    {
        if (!Application.isPlaying && rangeProfile.enableExpansion)
        {
            // 在编辑模式下模拟一半进度用于预览
            expansionProgress = rangeProfile.startInnerRatio + 0.5f * (1 - rangeProfile.startInnerRatio);
            UpdateMaterialProperties();
        }
    }
#endif

    // 外部获取当前进度
    public float GetExpansionProgress() => expansionProgress;
    public bool IsExpanding() => isExpanding;
}