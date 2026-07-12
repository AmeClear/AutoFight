using UnityEngine;
using System.Collections.Generic;

public class RangeSystemManager
{
    private static RangeSystemManager _instance;
    public static RangeSystemManager Instance
    {
        get
        {
            if (_instance == null)
                _instance = new RangeSystemManager();
            return _instance;
        }
    }

    [SerializeField] private GameObject rangeRendererPrefab;

    private int _nextInstanceId = 1;
    private readonly Dictionary<int, RangeRenderer> _activeRanges = new Dictionary<int, RangeRenderer>();
    private readonly Stack<RangeRenderer> _pooledRenderers = new Stack<RangeRenderer>();
    private Transform _rendererRoot;

    public int CreateRange(RangeProfile profile, Vector3 position, Transform parent = null)
    {
        if (profile == null)
        {
            Debug.LogError("[RangeSystemManager] CreateRange failed: profile is null.");
            return -1;
        }

        RangeRenderer renderer = GetRangeRenderer();
        if (parent != null)
        {
            renderer.transform.SetParent(parent, false);
            renderer.transform.localPosition = position;
        }
        else
        {
            renderer.transform.SetParent(GetRendererRoot(), false);
            renderer.transform.position = position;
        }

        renderer.SetProfile(profile);

        int handle = _nextInstanceId++;
        _activeRanges.Add(handle, renderer);
        return handle;
    }

    public int CreateExpandingRange(RangeProfile profile, Vector3 position, Transform parent = null,
        bool startExpansionImmediately = true)
    {
        int handle = CreateRange(profile, position, parent);
        if (handle < 0) return handle;

        RangeRenderer renderer = _activeRanges[handle];
        if (renderer != null && profile.enableExpansion && startExpansionImmediately)
            renderer.StartExpansion();

        return handle;
    }

    public void RemoveRange(int handle)
    {
        if (!_activeRanges.TryGetValue(handle, out RangeRenderer renderer))
            return;

        ReturnRangeRenderer(renderer);
        _activeRanges.Remove(handle);
    }

    public RangeRenderer GetRange(int handle)
    {
        _activeRanges.TryGetValue(handle, out RangeRenderer renderer);
        return renderer;
    }

    public void UpdateRangeProfile(int handle, RangeProfile newProfile)
    {
        if (_activeRanges.TryGetValue(handle, out RangeRenderer renderer))
            renderer.SetProfile(newProfile);
    }

    public void StartRangeExpansion(int handle)
    {
        if (_activeRanges.TryGetValue(handle, out RangeRenderer renderer))
            renderer.StartExpansion();
    }

    public void StopRangeExpansion(int handle, bool reset = false)
    {
        if (!_activeRanges.TryGetValue(handle, out RangeRenderer renderer))
            return;

        renderer.StopExpansion();
        if (reset)
            renderer.ResetExpansion();
    }

    public void SetRangeExpansionProgress(int handle, float progress)
    {
        if (_activeRanges.TryGetValue(handle, out RangeRenderer renderer))
            renderer.SetExpansionProgress(progress);
    }

    public void ClearAllRanges()
    {
        foreach (var kvp in _activeRanges)
            ReturnRangeRenderer(kvp.Value);

        _activeRanges.Clear();
    }

    public int ActiveRangeCount => _activeRanges.Count;

    private Transform GetRendererRoot()
    {
        if (_rendererRoot != null)
            return _rendererRoot;

        GameObject root = GameObject.Find("RangeSystemRoot");
        if (root == null)
        {
            root = new GameObject("RangeSystemRoot");
            if (Application.isPlaying)
                Object.DontDestroyOnLoad(root);
        }

        _rendererRoot = root.transform;
        return _rendererRoot;
    }

    private RangeRenderer GetRangeRenderer()
    {
        while (_pooledRenderers.Count > 0)
        {
            RangeRenderer renderer = _pooledRenderers.Pop();
            if (renderer != null)
            {
                renderer.gameObject.SetActive(true);
                return renderer;
            }
        }

        if (rangeRendererPrefab == null)
        {
            GameObject go = new GameObject("RangeRenderer");
            go.transform.SetParent(GetRendererRoot(), false);
            return go.AddComponent<RangeRenderer>();
        }

        GameObject instance = Object.Instantiate(rangeRendererPrefab, GetRendererRoot());
        instance.name = rangeRendererPrefab.name;
        return instance.GetComponent<RangeRenderer>();
    }

    private void ReturnRangeRenderer(RangeRenderer renderer)
    {
        if (renderer == null) return;

        renderer.PrepareForReuse();
        renderer.transform.SetParent(GetRendererRoot(), false);
        _pooledRenderers.Push(renderer);
    }
}
