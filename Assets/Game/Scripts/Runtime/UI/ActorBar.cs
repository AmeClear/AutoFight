using UnityEngine;

[DisallowMultipleComponent]
public class ActorBar : MonoBehaviour
{
    [Header("挂载点")]
    [SerializeField] private Transform headAnchor;
    [SerializeField] private string headAnchorName = "Head_Top";
    [SerializeField] private Vector3 headAnchorFallbackOffset = new Vector3(0f, 2f, 0f);

    [Header("可见性剔除")]
    [SerializeField] private Renderer visibilityRenderer;

    private UnitHealthBar _healthBar;
    private bool _pendingAcquire;

    public bool IsActive => _healthBar != null && _healthBar.IsBound;

    private void Awake()
    {
        ResolveHeadAnchor();

        if (visibilityRenderer == null)
            visibilityRenderer = GetComponentInChildren<Renderer>();
    }

    private void OnEnable()
    {
        TryAcquireHealthBar();
    }

    private void Start()
    {
        // 兜底：若 OnEnable 时对象池尚未 Awake，下一帧再取一次。
        if (_healthBar == null)
            TryAcquireHealthBar();
    }

    private void OnDisable()
    {
        _pendingAcquire = false;
        ReleaseHealthBar();
    }

    public void SetHealth(float currentHp, float maxHp)
    {
        if (_healthBar == null)
            TryAcquireHealthBar();

        if (_healthBar == null)
            return;

        _healthBar.SetHealth(currentHp, maxHp);
    }

    public void SetStamina(float currentStamina, float maxStamina)
    {
        if (_healthBar == null)
            TryAcquireHealthBar();

        if (_healthBar == null)
            return;

        _healthBar.SetStamina(currentStamina, maxStamina);
    }

    public void SetDefense(float currentDefense, float maxDefense)
    {
        if (_healthBar == null)
            TryAcquireHealthBar();

        if (_healthBar == null)
            return;

        _healthBar.SetDefense(currentDefense, maxDefense);
    }

    private void TryAcquireHealthBar()
    {
        if (_healthBar != null || !isActiveAndEnabled)
            return;

        if (headAnchor == null)
        {
            Debug.LogWarning($"[ActorBar] {name} 缺少 Head 挂载点，血条未创建。", this);
            return;
        }

        UnitHealthBarPool pool = UnitHealthBarPool.Instance;
        if (pool == null)
        {
            if (!_pendingAcquire)
            {
                _pendingAcquire = true;
                Debug.LogWarning($"[ActorBar] {name} 尚未找到 UnitHealthBarPool，将在 Start 再试。", this);
            }

            return;
        }

        _pendingAcquire = false;
        _healthBar = pool.Get(headAnchor);
        if (_healthBar == null)
            return;

        _healthBar.SetVisibilityRenderer(visibilityRenderer);
    }

    private void ResolveHeadAnchor()
    {
        if (headAnchor != null)
            return;

        Transform found = transform.Find(headAnchorName);
        if (found != null)
        {
            headAnchor = found;
            return;
        }

        headAnchor = new GameObject(headAnchorName).transform;
        headAnchor.SetParent(transform, false);
        headAnchor.localPosition = headAnchorFallbackOffset;
    }

    private void ReleaseHealthBar()
    {
        if (_healthBar == null)
            return;

        UnitHealthBarPool pool = UnitHealthBarPool.Instance;
        if (pool != null)
            pool.Release(_healthBar);

        _healthBar = null;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (headAnchor == null && !string.IsNullOrEmpty(headAnchorName))
        {
            Transform found = transform.Find(headAnchorName);
            if (found != null)
                headAnchor = found;
        }
    }
#endif
}
