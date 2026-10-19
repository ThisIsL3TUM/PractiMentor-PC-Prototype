using System;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using UnityEngine.EventSystems;
using NUnit.Framework.Constraints;

public class SliderTESTER : MonoBehaviour, IPointerUpHandler
{
    //general set up stuff
    [Header("General Set Up")]
    [SerializeField] private Slider slider;
    [SerializeField] private Transform transformReference;//for both rotation and position
    [SerializeField] private float sliderMoveChargeRate = 1f;
    [SerializeField] private float resetDuration = 0.5f;

    private Vector3 startingPosition;
    private Vector3 startingWorldPosition;

    private Quaternion startingRotation;
    private Vector3 startingScale;

    //elements related to general rotation
    [Header("Rotation Set Up")]
    [SerializeField] private float initialRotation = 0;
    [SerializeField] private float targetRotation = 90f;

    //elements related to object rotation snapping
    [SerializeField] private Boolean isSnapping = false;
    [Range(0, 90)][SerializeField] private float rotationSnapAngle = 15f;

    private SliderTarget activeTarget;
    private Transform targetObject;

    private Quaternion targetObjectInitialRotation;
    private bool hasInitialRotation;

    public event Action TargetTransformReached;

    public enum SliderRotationDirection
    {
        X, 
        Y, 
        Z
    }

    [SerializeField] private SliderRotationDirection rotationDirection = SliderRotationDirection.X;

    private bool targetReached;
    [SerializeField] private float targetTolerance = 1f;

    private float startingValue;

    private void Awake()
    {
        slider.onValueChanged.AddListener(UpdateTargetTransform);
    }
    
    public void SetTarget(SliderTarget target)
    {
        if (activeTarget == target && hasInitialRotation)
            return;

        activeTarget = target;
        targetObject = target.transform;

        startingPosition = targetObject.localPosition;
        startingWorldPosition = targetObject.position;
        startingRotation = targetObject.localRotation;
        startingScale = targetObject.localScale;

        startingValue = GetStartingValue();
        
        hasInitialRotation = true;
        targetReached = false;

        ResetSlider();
    }

    private float GetStartingValue()
    {
        switch (activeTarget.Property)
        {
            case SliderTarget.TransformProperty.Position:
                return GetAxisValue(startingPosition);
            case SliderTarget.TransformProperty.Rotation:
                return GetAxisValue(startingRotation.eulerAngles);
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
    public void ClearTarget()
    {
        activeTarget = null;
        targetObject = null;
        hasInitialRotation = false;
        targetReached = false;
    }

    private void UpdateTargetTransform(float value)
    {
        if (activeTarget == null || targetObject == null)
            return;

        float normalizedValue = Mathf.InverseLerp(slider.minValue, slider.maxValue, value);

        float progress = 1f - normalizedValue;

        float currentValue = Mathf.Lerp(startingValue, activeTarget.TargetValue, progress);

        if (activeTarget.Property == SliderTarget.TransformProperty.Rotation && activeTarget.SnappingActive && activeTarget.RotationSnapAngle > 0f)
        {
            currentValue = Mathf.Round((currentValue/activeTarget.RotationSnapAngle) * activeTarget.RotationSnapAngle);
        }

        switch (activeTarget.Property)
        {
            case SliderTarget.TransformProperty.Position:
                ApplyPosition(currentValue);
                break;
            case SliderTarget.TransformProperty.Rotation:
                ApplyRotation(currentValue);
                break;
            case SliderTarget.TransformProperty.Scale:
                ApplyScale(currentValue); 
                break;
        }

        float difference = Mathf.Abs(currentValue - activeTarget.TargetValue);

        if (!targetReached && difference <= activeTarget.Tolerance)
        {
            targetReached = true;

            RestoreStartingTransform();
            TargetTransformReached?.Invoke();
        }
    }

    private void ApplyPosition(float value)
    {
        Vector3 position = startingPosition;

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

        if(activeTarget.UseLocalSpace)
            targetObject.localPosition = position;
        else
            targetObject.position = position;
    }
    private void ApplyRotation(float value)
    {
        Vector3 rotation = startingRotation.eulerAngles;

        switch(activeTarget.TransformAxis)
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

    private void OnDestroy()
    {
        slider.onValueChanged.RemoveListener(UpdateTargetTransform);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!targetReached && targetObject != null)
        {
            slider.DOKill();
            slider.DOValue(slider.maxValue, resetDuration).SetEase(Ease.InOutSine);
        }
    }

    public void ResetSlider()
    {
        if (slider != null) 
            slider.SetValueWithoutNotify(slider.maxValue);
    }

    public void RestoreStartingTransform()
    {
        if (activeTarget.UseLocalSpace)
            targetObject.DOLocalMove(startingPosition, resetDuration).SetEase(Ease.InOutSine);
        else
            targetObject.DOMove(startingWorldPosition, resetDuration).SetEase(Ease.InOutSine);

        targetObject.DOLocalRotateQuaternion(startingRotation, resetDuration).SetEase(Ease.InOutSine);
        targetObject.DOScale(startingScale, resetDuration).SetEase(Ease.InOutSine);
    }
}
