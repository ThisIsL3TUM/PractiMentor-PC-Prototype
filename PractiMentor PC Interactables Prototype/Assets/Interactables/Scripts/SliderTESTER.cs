using System;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using UnityEngine.EventSystems;

public class SliderTESTER : MonoBehaviour, IPointerUpHandler
{
    //general set up stuff
    [Header("General Set Up")]
    [SerializeField] private Slider slider;
    [SerializeField] private Transform transformReference;//for both rotation and position
    [SerializeField] private float sliderMoveChargeRate = 1f;
    [SerializeField] private float resetDuration = 0.5f;

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

    private void Awake()
    {
        slider.onValueChanged.AddListener(RotateTarget);
    }
    
    public void SetTarget(SliderTarget target)
    {
        if (activeTarget == target && hasInitialRotation)
            return;

        activeTarget = target;
        targetObject = target.transform;
        targetObjectInitialRotation = targetObject.rotation;
        hasInitialRotation = true;
        targetReached = false;
    }

    public void ClearTarget()
    {
        activeTarget = null;
        targetObject = null;
        hasInitialRotation = false;
        targetReached = false;
    }

    private void RotateTarget(float value)
    {
        if (activeTarget == null)
            return;

        float angle = Mathf.Lerp(activeTarget.TargetRotation, activeTarget.InitialRotation, value);

        if (activeTarget.SnappingActive && activeTarget.RotationSnapAngle > 0f)
        {
            angle = activeTarget.InitialRotation + Mathf.Round((angle - activeTarget.InitialRotation) / activeTarget.RotationSnapAngle) * activeTarget.RotationSnapAngle;
        }

        switch(activeTarget.RotationAxis)
        {
            case SliderTarget.Axis.X:
                targetObject.rotation = transformReference.rotation * Quaternion.Euler(angle, 0f, 0f);
                break;
            case SliderTarget.Axis.Y:
                targetObject.rotation = transformReference.rotation * Quaternion.Euler(0f, angle, 0f);
                break;
            case SliderTarget.Axis.Z:
                targetObject.rotation = transformReference.rotation * Quaternion.Euler(0f, 0f, angle);
                break;
        }

        float angleDifference = Mathf.Abs(Mathf.DeltaAngle(angle, activeTarget.TargetRotation));


        if (!targetReached && angleDifference <= activeTarget.Tolerance)
        {
            targetReached = true;
            targetObject.DORotateQuaternion(targetObjectInitialRotation, 0.5f).SetEase(Ease.InOutSine);
            TargetTransformReached?.Invoke();

        }
    }

    private void OnDestroy()
    {
        slider.onValueChanged.RemoveListener(RotateTarget);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!targetReached && targetObject != null)
        {
            slider.DOKill();
            slider.DOValue(slider.maxValue, resetDuration).SetEase(Ease.InOutSine);
        }
    }
}
