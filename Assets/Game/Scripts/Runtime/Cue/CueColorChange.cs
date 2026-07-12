using GAS.Runtime;
using UnityEngine;

[CreateAssetMenu(fileName = "NewColorChangeCue", menuName = "GAS/Cue/CueColorChange")]
public class CueColorChange : GameplayCueDurational
{
    [Tooltip("相对于 Owner 的子节点路径，留空则使用 Owner 自身")]
    public string rendererRelativePath;

    [Tooltip("是否包含子物体上的 Renderer")]
    public bool includeChildren;

    [Tooltip("Shader 颜色属性名，Built-in 为 _Color，URP 为 _BaseColor")]
    public string colorPropertyName = "_Color";

    public Color targetColor = Color.white;

    [Tooltip("每帧向目标颜色过渡的比例 (0-1)")]
    [Range(0f, 1f)]
    public float lerpFactor = 0.15f;

    public override GameplayCueDurationalSpec CreateSpec(GameplayCueParameters parameters)
    {
        return new CueColorChangeSpec(this, parameters);
    }
}

public class CueColorChangeSpec : GameplayCueDurationalSpec<CueColorChange>
{
    private readonly Renderer[] _renderers;
    private readonly Color[] _originalColors;
    private readonly Color[] _currentColors;
    private readonly int _colorPropertyId;
    private readonly MaterialPropertyBlock _propertyBlock;
    private bool _isActive;

    public CueColorChangeSpec(CueColorChange cue, GameplayCueParameters parameters) : base(cue, parameters)
    {
        _colorPropertyId = Shader.PropertyToID(cue.colorPropertyName);
        _propertyBlock = new MaterialPropertyBlock();
        _renderers = ResolveRenderers(out _originalColors, out _currentColors);
    }

    public override void OnAdd()
    {
        _isActive = true;
        ApplyColors();
    }

    public override void OnRemove()
    {
        _isActive = false;
        RestoreColors();
    }

    public override void OnGameplayEffectActivate()
    {
        _isActive = true;
    }

    public override void OnGameplayEffectDeactivate()
    {
        _isActive = false;
        RestoreColors();
    }

    public override void OnTick()
    {
        if (!_isActive || _renderers == null || _renderers.Length == 0) return;

        for (var i = 0; i < _renderers.Length; i++)
        {
            _currentColors[i] = Color.Lerp(_currentColors[i], cue.targetColor, cue.lerpFactor);
        }

        ApplyColors();
    }

    private Renderer[] ResolveRenderers(out Color[] originalColors, out Color[] currentColors)
    {
        originalColors = null;
        currentColors = null;

        var targetTransform = string.IsNullOrEmpty(cue.rendererRelativePath)
            ? Owner.transform
            : Owner.transform.Find(cue.rendererRelativePath);

        if (targetTransform == null)
        {
            Debug.LogError(
                $"[CueColorChange] Renderer path not found: \"{cue.rendererRelativePath}\" on {Owner.name}");
            return null;
        }

        var renderers = cue.includeChildren
            ? targetTransform.GetComponentsInChildren<Renderer>()
            : targetTransform.GetComponents<Renderer>();

        if (renderers == null || renderers.Length == 0)
        {
            Debug.LogError(
                $"[CueColorChange] No Renderer found on {Owner.name}, path: \"{cue.rendererRelativePath}\"");
            return null;
        }

        originalColors = new Color[renderers.Length];
        currentColors = new Color[renderers.Length];

        for (var i = 0; i < renderers.Length; i++)
        {
            originalColors[i] = GetRendererColor(renderers[i]);
            currentColors[i] = originalColors[i];
        }

        return renderers;
    }

    private Color GetRendererColor(Renderer renderer)
    {
        var material = renderer.sharedMaterial;
        if (material != null && material.HasProperty(_colorPropertyId))
            return material.GetColor(_colorPropertyId);

        renderer.GetPropertyBlock(_propertyBlock);
        if (_propertyBlock.HasColor(_colorPropertyId))
            return _propertyBlock.GetColor(_colorPropertyId);

        return Color.white;
    }

    private void ApplyColors()
    {
        if (_renderers == null) return;

        for (var i = 0; i < _renderers.Length; i++)
        {
            _renderers[i].GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetColor(_colorPropertyId, _currentColors[i]);
            _renderers[i].SetPropertyBlock(_propertyBlock);
        }
    }

    private void RestoreColors()
    {
        if (_renderers == null) return;

        for (var i = 0; i < _renderers.Length; i++)
        {
            _renderers[i].SetPropertyBlock(null);
        }
    }
}
