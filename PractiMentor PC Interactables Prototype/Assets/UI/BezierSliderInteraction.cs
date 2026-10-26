using System;
using UnityEngine;
using UnityEngine.UIElements;
using DG.Tweening;

public class BezierSliderInteraction : MonoBehaviour
{
    [Header("UI Toolkit")]
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private string sliderElementName = "interaction-slider";

    [Header("Slider Customization")]
    [SerializeField] private float resetDuration = 0.5f;
    [SerializeField, Range(1f, 5f)] private float sliderResistance = 5f;

    private BezierSlider slider;

    private SliderTarget activeTarget;
    private Transform targetObject;

    private Vector3 startingPosition;
    private Vector3 startingWorldPosition;
    private Quaternion startingRotation;
    private Quaternion startingWorldRotation;
    private Vector3 startingScale;

    private float startingValue;
    private bool targetReached;
    private bool hasTarget;

    public event Action TargetTransformReached;

    public float ResetDuration => resetDuration;

    public bool IsPointerHeld => slider != null && slider.IsPointerHeld;

    public bool IsTargetReached => targetReached;

    private BezierSlider CreateSlider()
    {
        return new BezierSlider
        {
            name = sliderElementName,

            lowValue = 0f,
            highValue = 1f,
            value = 1f,

            resetValue = 1f,
            resetDelay = 0.5f,
            resetDuration = 1.5f,

            controlPoint1 = new Vector2(1f, 0.25f),
            controlPoint2 = new Vector2(1f, 0.75f),
            curveSegments = 32,

            backgroundTrackWidth = 64f,
            curveTrackWidth = 28f,
            knobSize = 60f,

            curveColor = new Color(0.4f, 0.85f, 1f),
            trackColor = new Color(1f, 0.94f, 0.85f)
        };
    }

    private void OnEnable()
    {
        if (uiDocument == null)
        {
            Debug.LogError($"{name}: UIDocument is not assigned.");
            return;
        }

        VisualElement root = uiDocument.rootVisualElement;

        slider = root.Q<BezierSlider>(sliderElementName);

        if (slider == null)
        {
            slider = CreateSlider();
            root.Add(slider);
        }

        slider.lowValue = 0f;
        slider.highValue = 1f;
        slider.resetValue = slider.highValue;
        slider.SetValueWithoutNotify(slider.highValue);

        slider.RegisterValueChangedCallback(OnSliderValueChanged);
        slider.SetAutomaticResetEnabled(true);
        SetVisible(false);
    }

    public void SetTarget(SliderTarget target)
    {
        if (target == null)
        {
            Debug.LogWarning($"{name}: Tried to assign a null SliderTarget.");
            return;
        }

        if (activeTarget == target && hasTarget)
            return;

        activeTarget = target;
        targetObject = target.transform;

        if (targetObject == null)
        {
            Debug.LogError($"{name}: SliderTarget has no Transform.");
            return;
        }

        startingPosition = targetObject.localPosition;
        startingWorldPosition = targetObject.position;
        startingRotation = targetObject.localRotation;
        startingWorldRotation = targetObject.rotation;
        startingScale = targetObject.localScale;

        startingValue = GetStartingValue();

        targetReached = false;
        hasTarget = true;

        ResetSlider();
    }

    public void ClearTarget()
    {
        activeTarget = null;
        targetObject = null;

        targetReached = false;
        hasTarget = false;
    }

    public void SetVisible(bool visible)
    {
        if (slider == null)
            return;

        slider.style.display = visible
            ? DisplayStyle.Flex
            : DisplayStyle.None;
    }
    private void OnSliderValueChanged(ChangeEvent<float> evt)
    {
        if (!hasTarget || activeTarget == null || targetObject == null)
            return;

        float normalizedValue = Mathf.InverseLerp(slider.lowValue,slider.highValue,evt.newValue);

        float progress = Mathf.Pow(1f - normalizedValue, sliderResistance);
        float currentValue;

        switch (activeTarget.Property)
        {
            case SliderTarget.TransformProperty.Position:
                currentValue = Mathf.Lerp(activeTarget.InitialValue,activeTarget.TargetValue,progress);
                ApplyPosition(currentValue);
                break;

            case SliderTarget.TransformProperty.Rotation:
                float targetAngle = startingValue +Mathf.DeltaAngle(startingValue,activeTarget.TargetValue);
                currentValue = Mathf.Lerp(startingValue,targetAngle,progress);

                if (activeTarget.SnappingActive &&
                    activeTarget.RotationSnapAngle > 0f)
                {
                    float angleFromStart = currentValue - startingValue;

                    currentValue = startingValue + Mathf.Round(angleFromStart / activeTarget.RotationSnapAngle) * activeTarget.RotationSnapAngle;
                }

                ApplyRotation(currentValue);
                break;

            case SliderTarget.TransformProperty.Scale:
                currentValue = Mathf.Lerp(startingValue, activeTarget.TargetValue, progress);

                ApplyScale(currentValue);
                break;

            default:
                return;
        }

        float difference = activeTarget.Property ==
            SliderTarget.TransformProperty.Rotation
            ? Mathf.Abs(Mathf.DeltaAngle(currentValue, activeTarget.TargetValue))
            : Mathf.Abs(currentValue - activeTarget.TargetValue);

        if (difference <= activeTarget.Tolerance)
        {
            if (!targetReached)
            {
                targetReached = true;
                TargetTransformReached?.Invoke();
            }
        }
        else
        {
            targetReached = false;
        }
    }

    private float GetStartingValue()
    {
        switch (activeTarget.Property)
        {
            case SliderTarget.TransformProperty.Position:
                return GetAxisValue(startingPosition);

            case SliderTarget.TransformProperty.Rotation:
                return GetAxisValue(activeTarget.UseLocalSpace
            ? startingRotation.eulerAngles
            : startingWorldRotation.eulerAngles
            );

            case SliderTarget.TransformProperty.Scale:
                return GetAxisValue(startingScale);

            default:
                return 0f;
        }
    }

    private float GetAxisValue(Vector3 value)
    {
        switch (activeTarget.TransformAxis)
        {
            case SliderTarget.Axis.X:
                return value.x;

            case SliderTarget.Axis.Y:
                return value.y;

            case SliderTarget.Axis.Z:
                return value.z;

            default:
                return 0f;
        }
    }

    private void ApplyPosition(float value)
    {
        Vector3 position = activeTarget.UseLocalSpace
        ? startingPosition
        : startingWorldPosition;

        float offset = value - activeTarget.InitialValue;

        switch (activeTarget.TransformAxis)
        {
            case SliderTarget.Axis.X:
                position.x += offset;
                break;

            case SliderTarget.Axis.Y:
                position.y += offset;
                break;

            case SliderTarget.Axis.Z:
                position.z += offset;
                break;
        }

        if (activeTarget.UseLocalSpace)
            targetObject.localPosition = position;
        else
            targetObject.position = position;
    }

    private void ApplyRotation(float value)
    {
        Vector3 rotation = activeTarget.UseLocalSpace
        ? startingRotation.eulerAngles
        : startingWorldRotation.eulerAngles;

        switch (activeTarget.TransformAxis)
        {
            case SliderTarget.Axis.X:
                rotation.x = value;
                break;

            case SliderTarget.Axis.Y:
                rotation.y = value;
                break;

            case SliderTarget.Axis.Z:
                rotation.z = value;
                break;
        }

        Quaternion newRotation = Quaternion.Euler(rotation);

        if (activeTarget.UseLocalSpace)
            targetObject.localRotation = newRotation;
        else
            targetObject.rotation = newRotation;
    }

    private void ApplyScale(float value)
    {
        Vector3 scale = startingScale;

        switch (activeTarget.TransformAxis)
        {
            case SliderTarget.Axis.X:
                scale.x = value;
                break;

            case SliderTarget.Axis.Y:
                scale.y = value;
                break;

            case SliderTarget.Axis.Z:
                scale.z = value;
                break;
        }

        targetObject.localScale = scale;
    }

    public void ResetSlider()
    {
        if (slider == null)
            return;

        slider.SetValueWithoutNotify(slider.highValue);
    }

    public Tween RestoreStartingTransform()
    {
        if (targetObject == null || activeTarget == null)
            return null;

        switch (activeTarget.Property)
        {
            case SliderTarget.TransformProperty.Position:
                if (activeTarget.UseLocalSpace)
                {
                    return targetObject.DOLocalMove(startingPosition, resetDuration).SetEase(Ease.InOutCirc);
                }
                
                return targetObject.DOMove(startingWorldPosition, resetDuration).SetEase(Ease.InOutCirc);
                

            case SliderTarget.TransformProperty.Rotation:
                if (activeTarget.UseLocalSpace)
                {
                    return targetObject.DOLocalRotateQuaternion(startingRotation, resetDuration).SetEase(Ease.InOutCirc);
                }
                return targetObject.DORotateQuaternion(startingWorldRotation,resetDuration).SetEase(Ease.InOutCirc);
                

            case SliderTarget.TransformProperty.Scale:
                return targetObject.DOScale(startingScale, resetDuration).SetEase(Ease.InOutCirc);
        }

        return null;
    }

    public void FollowObjectOnScreen(Transform target, Camera camera, Vector2 offset)
    {
        if (slider == null || target == null || camera == null)
            return;

        if (slider.panel == null)
            return;

        Vector3 screenPosition = camera.WorldToScreenPoint(target.position);

        if (screenPosition.z <= 0)
            return;

        VisualElement panelRoot = slider.panel.visualTree;

        float panelWidth = panelRoot.resolvedStyle.width;
        float panelHeight = panelRoot.resolvedStyle.height;

        if (panelWidth <= 0f || panelHeight <= 0f)
            return;

        float panelX =
            screenPosition.x / Screen.width * panelWidth;

        float panelY = (1f - screenPosition.y / Screen.height) * panelHeight;

        float sliderHeight = slider.resolvedStyle.height;

        slider.style.position = Position.Absolute;
        slider.style.left = panelX + offset.x;
        slider.style.top = panelY - sliderHeight * 0.5f + offset.y;
    }

    public void SetAutomaticResetEnabled(bool enabled)
    {
        slider?.SetAutomaticResetEnabled(enabled);
    }

    private void OnDisable()
    {
        if (slider != null)
        {
            slider.SetAutomaticResetEnabled(true);
            slider.UnregisterValueChangedCallback(OnSliderValueChanged);
        }
    }

    private void OnDestroy()
    {
        if (targetObject != null)
            targetObject.DOKill();
    }

}
