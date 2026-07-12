using System.Collections.Generic;
using UnityEngine;

public class UnitHealthBarPool : MonoBehaviour
{
    private static UnitHealthBarPool instance;

    public static UnitHealthBarPool Instance => instance;

    [Header("引用")]
    [SerializeField] private UnitHealthBar healthBarPrefab;
    [SerializeField] private RectTransform healthBarRoot;
    [SerializeField] private Camera targetCamera;

    [Header("对象池")]
    [SerializeField] private int prewarmCount = 8;

    private readonly Stack<UnitHealthBar> pool = new Stack<UnitHealthBar>();
    private readonly HashSet<UnitHealthBar> activeBars = new HashSet<UnitHealthBar>();

    public RectTransform HealthBarRoot => healthBarRoot;
    public Camera TargetCamera => targetCamera;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Debug.LogWarning("[UnitHealthBarPool] 场景中存在多个对象池，保留最先创建的实例。");
            return;
        }

        instance = this;

        if (targetCamera == null)
            targetCamera = Camera.main;

        Prewarm();
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    public UnitHealthBar Get(Transform headPoint)
    {
        if (healthBarPrefab == null || healthBarRoot == null)
        {
            Debug.LogError("[UnitHealthBarPool] Prefab 或 HealthBarRoot 未配置。");
            return null;
        }

        if (targetCamera == null)
        {
            Debug.LogError("[UnitHealthBarPool] TargetCamera 未配置。");
            return null;
        }

        UnitHealthBar bar = pool.Count > 0 ? pool.Pop() : CreateInstance();
        bar.transform.SetParent(healthBarRoot, false);
        bar.gameObject.SetActive(true);
        bar.SetTarget(headPoint, targetCamera);
        activeBars.Add(bar);
        return bar;
    }

    public void Release(UnitHealthBar bar)
    {
        if (bar == null)
            return;

        if (!activeBars.Remove(bar))
            return;

        bar.ClearTarget();
        bar.gameObject.SetActive(false);
        bar.transform.SetParent(healthBarRoot, false);
        pool.Push(bar);
    }

    public void SetTargetCamera(Camera camera)
    {
        if (camera == null)
            return;

        targetCamera = camera;

        foreach (UnitHealthBar bar in activeBars)
            bar.SetCamera(targetCamera);
    }

    private void Prewarm()
    {
        if (healthBarPrefab == null || healthBarRoot == null)
            return;

        for (int i = 0; i < prewarmCount; i++)
        {
            UnitHealthBar bar = CreateInstance();
            bar.gameObject.SetActive(false);
            pool.Push(bar);
        }
    }

    private UnitHealthBar CreateInstance()
    {
        UnitHealthBar bar = Instantiate(healthBarPrefab, healthBarRoot);
        bar.name = healthBarPrefab.name;
        return bar;
    }
}
