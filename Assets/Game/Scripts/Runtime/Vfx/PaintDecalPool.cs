using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 内置管线颜料弹痕对象池。由 <see cref="CuePaintDecal"/> 在命中点刷贴花。
/// </summary>
[DisallowMultipleComponent]
public class PaintDecalPool : MonoBehaviour
{
    private const string ShaderName = "AutoFight/PaintDecal";
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    private static PaintDecalPool _instance;
    private static Mesh _quadMesh;

    private readonly Queue<PaintDecal> _idle = new Queue<PaintDecal>();
    private readonly List<PaintDecal> _active = new List<PaintDecal>();

    private Material _sharedMaterial;
    private int _created;

    public static PaintDecalPool Ensure()
    {
        if (_instance != null)
            return _instance;

        var root = new GameObject("PaintDecalPool");
        DontDestroyOnLoad(root);
        _instance = root.AddComponent<PaintDecalPool>();
        return _instance;
    }

    public static int RaycastFirst(Ray ray, float range, float radius, LayerMask mask, Transform owner,
        RaycastHit[] buffer, out RaycastHit hit)
    {
        hit = default;
        if (buffer == null || buffer.Length == 0)
            return 0;

        var count = GAS.Runtime.AbilityAreaUtil.Raycast3DNonAlloc(
            ray.origin, ray.direction, range, radius, buffer, mask);

        for (var i = 0; i < count; i++)
        {
            var candidate = buffer[i];
            if (candidate.collider == null)
                continue;

            if (owner != null &&
                (candidate.collider.transform == owner || candidate.collider.transform.IsChildOf(owner)))
                continue;

            hit = candidate;
            return 1;
        }

        return 0;
    }

    public void Spawn(in RaycastHit hit, Color color, float size, float lifetime, float fadeDuration,
        float surfaceOffset, bool attachToHit, Shader shader)
    {
        EnsureMaterial(shader);
        if (_sharedMaterial == null)
            return;

        var decal = GetDecal();
        decal.Bind(hit, color, size, lifetime, fadeDuration, surfaceOffset, attachToHit, _sharedMaterial);
        _active.Add(decal);
    }

    private void LateUpdate()
    {
        var dt = Time.deltaTime;
        for (var i = _active.Count - 1; i >= 0; i--)
        {
            var decal = _active[i];
            if (decal.Tick(dt))
                continue;

            _active.RemoveAt(i);
            Recycle(decal);
        }
    }

    private PaintDecal GetDecal()
    {
        if (_idle.Count > 0)
            return _idle.Dequeue();

        _created++;
        var go = new GameObject($"PaintDecal_{_created}");
        go.transform.SetParent(transform, false);
        go.SetActive(false);
        return new PaintDecal(go, GetQuadMesh());
    }

    private void Recycle(PaintDecal decal)
    {
        decal.Release();
        decal.Transform.SetParent(transform, false);
        _idle.Enqueue(decal);
    }

    private void EnsureMaterial(Shader shader)
    {
        if (_sharedMaterial != null)
            return;

        var resolved = shader != null ? shader : Shader.Find(ShaderName);
        if (resolved == null)
        {
            Debug.LogError("[PaintDecalPool] 找不到 Shader AutoFight/PaintDecal。");
            resolved = Shader.Find("Sprites/Default");
        }

        _sharedMaterial = new Material(resolved)
        {
            name = "PaintDecal_Shared",
            hideFlags = HideFlags.HideAndDontSave
        };
    }

    private static Mesh GetQuadMesh()
    {
        if (_quadMesh != null)
            return _quadMesh;

        _quadMesh = new Mesh
        {
            name = "PaintDecalQuad",
            hideFlags = HideFlags.HideAndDontSave,
            vertices = new[]
            {
                new Vector3(-0.5f, -0.5f, 0f),
                new Vector3(0.5f, -0.5f, 0f),
                new Vector3(-0.5f, 0.5f, 0f),
                new Vector3(0.5f, 0.5f, 0f)
            },
            uv = new[]
            {
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(0f, 1f),
                new Vector2(1f, 1f)
            },
            triangles = new[] { 0, 2, 1, 2, 3, 1 },
            normals = new[] { Vector3.forward, Vector3.forward, Vector3.forward, Vector3.forward },
            colors = new[] { Color.white, Color.white, Color.white, Color.white }
        };
        _quadMesh.RecalculateBounds();
        return _quadMesh;
    }

    private sealed class PaintDecal
    {
        private readonly MeshRenderer _renderer;
        private readonly MaterialPropertyBlock _block = new MaterialPropertyBlock();
        private Color _color;
        private float _lifetime;
        private float _fadeDuration;
        private float _remain;
        private Transform _follow;
        private Vector3 _localPoint;
        private Quaternion _localRotation;
        private Vector3 _localScale;

        public Transform Transform { get; }

        public PaintDecal(GameObject go, Mesh mesh)
        {
            Transform = go.transform;
            var filter = go.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            _renderer = go.AddComponent<MeshRenderer>();
            _renderer.shadowCastingMode = ShadowCastingMode.Off;
            _renderer.receiveShadows = false;
            _renderer.lightProbeUsage = LightProbeUsage.Off;
            _renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            _renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
        }

        public void Bind(in RaycastHit hit, Color color, float size, float lifetime, float fadeDuration,
            float surfaceOffset, bool attachToHit, Material material)
        {
            var normal = hit.normal.sqrMagnitude < 0.0001f ? Vector3.up : hit.normal.normalized;
            var position = hit.point + normal * surfaceOffset;
            var rotation = Quaternion.LookRotation(normal) *
                           Quaternion.AngleAxis(Random.Range(0f, 360f), Vector3.forward);

            _color = color;
            _lifetime = Mathf.Max(0.05f, lifetime);
            _fadeDuration = Mathf.Clamp(fadeDuration, 0f, _lifetime);
            _remain = _lifetime;
            _follow = attachToHit && hit.collider != null ? hit.collider.transform : null;

            Transform.SetParent(null, false);
            Transform.SetPositionAndRotation(position, rotation);
            Transform.localScale = Vector3.one * Mathf.Max(0.05f, size);

            if (_follow != null)
            {
                _localPoint = _follow.InverseTransformPoint(position);
                _localRotation = Quaternion.Inverse(_follow.rotation) * rotation;
                var lossy = _follow.lossyScale;
                _localScale = new Vector3(
                    Transform.localScale.x / Mathf.Max(0.0001f, lossy.x),
                    Transform.localScale.y / Mathf.Max(0.0001f, lossy.y),
                    Transform.localScale.z / Mathf.Max(0.0001f, lossy.z));
            }

            _renderer.sharedMaterial = material;
            ApplyColor(1f);
            Transform.gameObject.SetActive(true);
        }

        public bool Tick(float dt)
        {
            if (_follow != null)
            {
                Transform.SetPositionAndRotation(
                    _follow.TransformPoint(_localPoint),
                    _follow.rotation * _localRotation);
                var lossy = _follow.lossyScale;
                Transform.localScale = new Vector3(
                    _localScale.x * lossy.x,
                    _localScale.y * lossy.y,
                    _localScale.z * lossy.z);
            }

            _remain -= dt;
            if (_remain <= 0f)
                return false;

            var alpha = 1f;
            if (_fadeDuration > 0f && _remain < _fadeDuration)
                alpha = Mathf.Clamp01(_remain / _fadeDuration);

            ApplyColor(alpha);
            return true;
        }

        public void Release()
        {
            _follow = null;
            Transform.gameObject.SetActive(false);
        }

        private void ApplyColor(float alpha)
        {
            var faded = _color;
            faded.a *= alpha;
            _block.Clear();
            _block.SetColor(ColorId, faded);
            _renderer.SetPropertyBlock(_block);
        }
    }
}
