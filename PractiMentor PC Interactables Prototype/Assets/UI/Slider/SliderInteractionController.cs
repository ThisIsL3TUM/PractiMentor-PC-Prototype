using System;
using DG.Tweening;
using UnityEngine;

public class SliderInteractionController : MonoBehaviour
{
    [SerializeField] private SliderInputBase slider2D;
    [SerializeField] private SliderInputBase slider3D;

    //general slider feel customization
    [SerializeField] private float resetDuration = 0.5f;
    [SerializeField, Range(1f, 5f)] private float sliderResistance = 5f;

    private SliderInputBase activeInput;
    private SliderTarget activeTarget;
    private Transform targetObject;

    //position, rotation and scale target points
    private Vector3 startingPosition;
    private Vector3 startingWorldPosition;
    private Quaternion startingRotation;
    private Quaternion startingWorldRotation;
    private Vector3 startingScale;

    private float startingValue;
    private bool hasTarget;

    public bool IsPointerHeld => activeInput != null && activeInput.IsPointerHeld;
    public bool IsTargetReached { get; private set; }

    public event Action TargetTransformReached;

    public void SetTarget(SliderTarget target)
    {
        if (target == null)
        {
            Debug.LogWarning($"{name}: Tried to assign a null SliderTarget.");
            return;
        }

        SliderInputBase selectedInput = GetInput(target.Representation);

        if (selectedInput == null)
        {
            Debug.LogError($"{name}: SliderTarget has no transform.");
            ClearTarget();
            return;
        }

        if (activeTarget == target && activeInput == selectedInput && hasTarget)
            return;


        if (activeInput != null)
        {
            activeInput.ValueChanged -= OnValueChanged;
            activeInput.SetVisible(false);
        }

        activeTarget = target;
        activeInput = selectedInput;
        targetObject = target.transform;

        if (targetObject == null)
        {
            Debug.LogError($"{name}: SliderTarget has no transform");
            return;
        }

        startingPosition = targetObject.localPosition;
        startingWorldPosition = targetObject.position;

        startingRotation = targetObject.localRotation;
        startingWorldRotation = targetObject.rotation;

        startingScale = targetObject.localScale;

        startingValue = GetStartingValue();

        IsTargetReached = false;
        hasTarget = false;

        activeInput.ResetSlider();

        activeInput.ValueChanged -= OnValueChanged;
        activeInput.ValueChanged += OnValueChanged;

        hasTarget = true;
    }
    public void RearmTarget(SliderTarget target)
    {
        if (target == null)
        {
            Debug.LogWarning($"{name}: tried to rearm a null SliderTarget");
            return;
        }

        SetTarget(target);
    }
    private SliderInputBase GetInput(SliderTarget.SliderRepresentation representation)
    {
        return representation == SliderTarget.SliderRepresentation._2D
            ? slider2D
            : slider3D;
    }
    private float GetStartingValue()
    {
        switch (activeTarget.Property)
        {
            case SliderTarget.TransformProperty.Position:
                return GetAxisValue(startingPosition);

            case SliderTarget.TransformProperty.Rotation:
                return GetAxisValue(
                    activeTarget.UseLocalSpace
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
        return activeTarget.TransformAxis switch
        {
            SliderTarget.Axis.X => value.x,
            SliderTarget.Axis.Y => value.y,
            SliderTarget.Axis.Z => value.z,
            _ => 0f
        };
    }
    private void OnValueChanged(float normalizedValue)
    {
        if (!hasTarget || activeTarget == null || targetObject == null)
            return;

        normalizedValue = Mathf.Clamp01(normalizedValue);

        float progress = Mathf.Pow(1f - normalizedValue, sliderResistance);
        float currentValue;

        switch (activeTarget.Property)
        {
            case SliderTarget.TransformProperty.Position:
                currentValue = Mathf.Lerp(activeTarget.InitialValue, activeTarget.TargetValue, progress);
                ApplyPosition(currentValue);
                break;

            case SliderTarget.TransformProperty.Rotation:
                float targetAngle = startingValue + Mathf.DeltaAngle(startingValue, activeTarget.TargetValue);
                currentValue = Mathf.Lerp(startingValue, targetAngle, progress);

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
            if (!IsTargetReached)
            {
                IsTargetReached = true;
                TargetTransformReached?.Invoke();
            }
        }
        else
        {
            IsTargetReached = false;
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
    private void OnDisable()
    {
        ClearTarget();
    }
    public void ClearTarget()
    {
        if (activeInput != null)
        {
            activeInput.ValueChanged -= OnValueChanged;
            activeInput.SetVisible(false);
        }

        activeInput = null;
        activeTarget = null;
        targetObject = null;

        hasTarget = false;
        IsTargetReached = false;
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
                return targetObject.DORotateQuaternion(startingWorldRotation, resetDuration).SetEase(Ease.InOutCirc);


            case SliderTarget.TransformProperty.Scale:
                return targetObject.DOScale(startingScale, resetDuration).SetEase(Ease.InOutCirc);
        }

        return null;
    }
    public void SetVisible(bool visible)
    {
        activeInput?.SetVisible(visible);
    }

    public void SetTimedProgress(float progress)
    {
        activeInput?.SetTimedProgress(progress);
    }

    public void ResetTimedProgress()
    {
        activeInput?.ResetTimedProgress();
    }

    public void SetAutomaticResetEnabled(bool enabled)
    {
        activeInput?.SetAutomaticResetEnabled(enabled);
    }
    public void FollowObjectOnScreen(Transform target, Camera camera, Vector2 offset)
    {
        activeInput?.FollowObjectOnScreen(target, camera, offset);
    }
}
