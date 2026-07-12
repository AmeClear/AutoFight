using UnityEngine;
using UnityEngine.UI;

public class UnitHealthBar : MonoBehaviour
{
    [Header("引用")]
    [SerializeField] private RectTransform uiRect;
    [SerializeField] private Image fillImage;
    [SerializeField] private Image staminaFillImage;
    [SerializeField] private Image defenseFillImage;
    [SerializeField] private Text healthText;

    [Header("偏移")]
    [SerializeField] private Vector3 worldOffset = Vector3.zero;

    [Header("性能优化")]
    [SerializeField] private int positionUpdateInterval = 3;
    [SerializeField] private float positionUpdateDistanceThreshold = 0.25f;
    [SerializeField] private float offScreenPadding = 50f;
    [SerializeField] private float screenPositionDirtyThreshold = 0.5f;

    [Header("显示")]
    [SerializeField] private bool smoothFill;
    [SerializeField] private float fillSmoothSpeed = 8f;
    [SerializeField] private bool showHealthText;

    private Transform headPoint;
    private Camera targetCamera;
    private Renderer visibilityRenderer;

    private float healthTargetFill = 1f;
    private float healthDisplayFill = 1f;
    private float staminaTargetFill = 1f;
    private float staminaDisplayFill = 1f;
    private float defenseTargetFill = 1f;
    private float defenseDisplayFill = 1f;

    private Vector3 lastSampledWorldPos = Vector3.positiveInfinity;
    private Vector2 lastScreenPos = new Vector2(float.NaN, float.NaN);
    private bool isBound;

    public bool IsBound => isBound;

    private void Awake()
    {
        if (uiRect == null)
            uiRect = transform as RectTransform;

        if (fillImage != null)
            healthDisplayFill = fillImage.fillAmount;

        if (staminaFillImage != null)
            staminaDisplayFill = staminaFillImage.fillAmount;

        if (defenseFillImage != null)
            defenseDisplayFill = defenseFillImage.fillAmount;

        if (healthText != null)
            healthText.gameObject.SetActive(showHealthText);
    }

    public void SetTarget(Transform head, Camera camera)
    {
        headPoint = head;
        targetCamera = camera;
        isBound = headPoint != null && targetCamera != null;
        lastSampledWorldPos = Vector3.positiveInfinity;
        lastScreenPos = new Vector2(float.NaN, float.NaN);
        gameObject.SetActive(isBound);
    }

    public void SetCamera(Camera camera)
    {
        targetCamera = camera;
        isBound = headPoint != null && targetCamera != null;
    }

    public void SetVisibilityRenderer(Renderer renderer)
    {
        visibilityRenderer = renderer;
    }

    public void ClearTarget()
    {
        headPoint = null;
        targetCamera = null;
        visibilityRenderer = null;
        isBound = false;
        lastSampledWorldPos = Vector3.positiveInfinity;
        lastScreenPos = new Vector2(float.NaN, float.NaN);
    }

    public void SetHealth(float currentHp, float maxHp)
    {
        ApplyFill(fillImage, ref healthTargetFill, ref healthDisplayFill, currentHp, maxHp);

        if (healthText != null && showHealthText)
        {
            float safeMax = Mathf.Max(maxHp, 0.0001f);
            healthText.text = $"{Mathf.CeilToInt(currentHp)}/{Mathf.CeilToInt(safeMax)}";
        }
    }

    public void SetStamina(float currentStamina, float maxStamina)
    {
        ApplyFill(staminaFillImage, ref staminaTargetFill, ref staminaDisplayFill, currentStamina, maxStamina);
    }

    public void SetDefense(float currentDefense, float maxDefense)
    {
        ApplyFill(defenseFillImage, ref defenseTargetFill, ref defenseDisplayFill, currentDefense, maxDefense);
    }

    private void ApplyFill(Image image, ref float targetFill, ref float displayFill, float current, float max)
    {
        if (image == null)
            return;

        float safeMax = Mathf.Max(max, 0.0001f);
        float nextTarget = Mathf.Clamp01(current / safeMax);

        if (Mathf.Abs(targetFill - nextTarget) <= 0.001f)
            return;

        targetFill = nextTarget;

        if (!smoothFill)
        {
            displayFill = targetFill;
            image.fillAmount = displayFill;
        }
    }

    private void Update()
    {
        if (!isBound)
            return;

        UpdateFillSmooth();
        TryUpdateScreenPosition();
    }

    private void UpdateFillSmooth()
    {
        if (!smoothFill)
            return;

        SmoothOne(fillImage, ref healthDisplayFill, healthTargetFill);
        SmoothOne(staminaFillImage, ref staminaDisplayFill, staminaTargetFill);
        SmoothOne(defenseFillImage, ref defenseDisplayFill, defenseTargetFill);
    }

    private void SmoothOne(Image image, ref float displayFill, float targetFill)
    {
        if (image == null)
            return;

        if (Mathf.Approximately(displayFill, targetFill))
            return;

        displayFill = Mathf.Lerp(displayFill, targetFill, fillSmoothSpeed * Time.deltaTime);
        image.fillAmount = displayFill;
    }

    private void TryUpdateScreenPosition()
    {
        if (headPoint == null || targetCamera == null || uiRect == null)
            return;

        if (visibilityRenderer != null && !visibilityRenderer.isVisible)
        {
            SetBarActive(false);
            return;
        }

        Vector3 worldPos = headPoint.position + worldOffset;
        if (!ShouldUpdatePosition(worldPos))
            return;

        Vector3 screenPos = targetCamera.WorldToScreenPoint(worldPos);
        lastSampledWorldPos = worldPos;

        if (screenPos.z < 0f || IsOffScreen(screenPos))
        {
            SetBarActive(false);
            return;
        }

        SetBarActive(true);

        if (IsScreenPositionDirty(screenPos))
        {
            uiRect.position = screenPos;
            lastScreenPos = screenPos;
        }
    }

    private void SetBarActive(bool active)
    {
        if (gameObject.activeSelf != active)
            gameObject.SetActive(active);
    }

    private bool ShouldUpdatePosition(Vector3 worldPos)
    {
        if (Time.frameCount % positionUpdateInterval == 0)
            return true;

        if (lastSampledWorldPos == Vector3.positiveInfinity)
            return true;

        return (worldPos - lastSampledWorldPos).sqrMagnitude >=
               positionUpdateDistanceThreshold * positionUpdateDistanceThreshold;
    }

    private bool IsScreenPositionDirty(Vector3 screenPos)
    {
        if (float.IsNaN(lastScreenPos.x))
            return true;

        return (screenPos.x - lastScreenPos.x) * (screenPos.x - lastScreenPos.x) +
               (screenPos.y - lastScreenPos.y) * (screenPos.y - lastScreenPos.y) >=
               screenPositionDirtyThreshold * screenPositionDirtyThreshold;
    }

    private bool IsOffScreen(Vector3 screenPos)
    {
        return screenPos.x < -offScreenPadding
               || screenPos.x > Screen.width + offScreenPadding
               || screenPos.y < -offScreenPadding
               || screenPos.y > Screen.height + offScreenPadding;
    }
}
