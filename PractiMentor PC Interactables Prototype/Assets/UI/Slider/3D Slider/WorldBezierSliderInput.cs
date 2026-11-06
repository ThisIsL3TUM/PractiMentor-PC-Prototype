using UnityEngine;
using DG.Tweening;

[ExecuteAlways]
public class WorldBezierSliderInput : SliderInputBase
{
    [Header("Curve")]
    [SerializeField] private Transform startPoint;
    [SerializeField] private Transform controlPoint1;
    [SerializeField] private Transform controlPoint2;
    [SerializeField] private Transform endPoint;

    [Header("Visuals")]
    [SerializeField] private Transform handle;
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private Transform visualRoot;

    [Header("Input")]
    [SerializeField] private Camera interactionCamera;
    [SerializeField] private LayerMask sliderLayer;
    [SerializeField, Min(8)] private int curveSamples = 32;

    [Header("Resets")]
    [SerializeField] private float resetDelay = 0.5f;
    [SerializeField] private float resetDuration = 1.5f;

    private float normalizedValue = 1f;
    private bool isDragging;

    private Transform followTarget;

    private Tween resetTween;

    private static readonly Quaternion SliderPlaneRotation = Quaternion.Euler(-90f, 180f, 0f);

    public override bool IsPointerHeld => isDragging;
    public override float NormalizedValue => normalizedValue;

    private void OnEnable()
    {
        if(interactionCamera == null)
            interactionCamera = Camera.main;

        if (lineRenderer != null)
            lineRenderer.useWorldSpace = false;

        DrawCurve();
        ApplyValue(1f, false);
    }
    private void Update()
    {
        if (!Application.isPlaying)
        {
            DrawCurve();
            ApplyValue(normalizedValue, false);
            return;
        }

        if (!isDragging && Input.GetMouseButtonDown(0) && IsHandleSelected())
        {
            resetTween?.Kill();
            isDragging = true;
        }

        if (isDragging && Input.GetMouseButton(0))
            SetValueFromMouse();

        if(isDragging && Input.GetMouseButtonUp(0))
        {
            isDragging = false;

            if (normalizedValue < 0.999f)
                ScheduleReset();
        }
    }
    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            if (lineRenderer != null)
                lineRenderer.useWorldSpace = false;

            DrawCurve();
            ApplyValue(normalizedValue, false);
        }
    }
    private bool IsHandleSelected()
    {
        Ray ray = interactionCamera.ScreenPointToRay(Input.mousePosition);

        if (!Physics.Raycast(ray, out RaycastHit hit, 100f, sliderLayer))
        {
            Debug.Log("3D slider: no raycast hit");
            return false;
        }

        Debug.Log($"3D slider hit: {hit.transform.name}, layer: {hit.transform.gameObject.layer}");

        return hit.transform == handle || hit.transform.IsChildOf(handle);
    }
    private void SetValueFromMouse()
    {
        Ray mouseRay = interactionCamera.ScreenPointToRay(Input.mousePosition);

        float closestDistance = float.MaxValue;
        float closestT = normalizedValue;

        for(int i = 0; i <= curveSamples; i++)
        {
            float curveT = i / (float)curveSamples;
            Vector3 point = EvaluateCurve(curveT);

            float distance = DistanceToRay(point, mouseRay);

            if(distance < closestDistance)
            {
                closestDistance = distance;
                closestT = 1f - curveT;
            }
        }

        SetValue(closestT);
    }
    private float DistanceToRay(Vector3 point, Ray ray)
    {
        Vector3 toPoint = point - ray.origin;
        float distanceAlongRay = Mathf.Max(0f, Vector3.Dot(toPoint, ray.direction));

        Vector3 closestPoint = ray.origin + ray.direction * distanceAlongRay;

        return Vector3.Distance(point, closestPoint);
    }
    private Vector3 EvaluateCurve(float t)
    {
        float inverse = 1f - t;

        return inverse * inverse * inverse * startPoint.position +
               3f * inverse * inverse * t * controlPoint1.position +
               3f * inverse * t * t * controlPoint2.position +
               t * t * t * endPoint.position;
    }
    private void ScheduleReset()
    {
        resetTween?.Kill();

        resetTween = DOTween.To(() => normalizedValue, SetValue, 1f, resetDuration).SetDelay(resetDelay).SetEase(Ease.InOutCirc).OnComplete(() => resetTween = null);
    }
    private void OnDisable()
    {
        resetTween?.Kill();
        resetTween = null;
    }
    public override void SetValue(float value)
    {
        ApplyValue(value, true);
    }
    private void ApplyValue(float value, bool notify)
    {
        normalizedValue = Mathf.Clamp01(value);

        if(handle != null)
            handle.position = EvaluateCurve(GetCurveT(normalizedValue));

        if(notify)
            RaiseValueChanged(normalizedValue);
    }
    public override void ResetSlider()
    {
        resetTween?.Kill();
        resetTween = null;

        isDragging = false;
        ApplyValue(1f, false);
    }
    public override void SetVisible(bool visible)
    {
        if (visualRoot != null)
            visualRoot.gameObject.SetActive(visible);
    }
    private void DrawCurve()
    {
        if (lineRenderer == null)
            return;

        lineRenderer.positionCount = curveSamples + 1;

        for (int i = 0; i <= curveSamples; i++)
        {
            float t = i / (float)curveSamples;

            Vector3 worldPoint = EvaluateCurve(1f - t);
            Vector3 localPoint = visualRoot.InverseTransformPoint(worldPoint);

            lineRenderer.SetPosition(i, localPoint);
        }
    }
    public override void FollowWorldTarget(Transform target,Vector3 localOffset)
    {
        followTarget = target;
        UpdateFollowPosition();
    }
    private void LateUpdate()
    {
        UpdateFollowPosition();
    }
    private void UpdateFollowPosition()
    {
        if (visualRoot == null || followTarget == null) return;

        visualRoot.SetPositionAndRotation(followTarget.position,followTarget.rotation);
    }

    private float GetCurveT(float sliderValue)
    {
        return 1f - sliderValue;
    }
}
