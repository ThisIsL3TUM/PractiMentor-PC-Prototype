using System;
using UnityEngine;
using UnityEngine.UI;

public class SliderTESTER : MonoBehaviour
{
    //general set up stuff
    [Header("General Set Up")]
    [SerializeField] private Slider slider;
    [SerializeField] private Transform transformReference;//for both rotation and position

    //elements related to general rotation
    [Header("Rotation Set Up")]
    [SerializeField] private float initialRotation = 0;
    [SerializeField] private float targetRotation = 90f;

    //elements related to object rotation snapping
    [SerializeField] private Boolean isSnapping = false;
    [Range(0, 15)][SerializeField] private float rotationSnapAngle = 15f;

    private Transform targetObject;

    public enum SliderRotationDirection
    {
        X, 
        Y, 
        Z
    }

    [SerializeField] private SliderRotationDirection rotationDirection = SliderRotationDirection.X;

    public event Action TargetTransformReached;

    private bool targetReached;
    [SerializeField] private float targetTolerance = 1f;

    private void Awake()
    {
        slider.onValueChanged.AddListener(RotateTarget);
    }
    
    public void SetTarget(Transform target)
    {
        targetObject = target;
        targetReached = false;
    }

    public void ClearTarget()
    {
        targetObject = null;
        targetReached = false;
    }

    private void RotateTarget(float value)
    {
        if (targetObject == null)
            return;

        float angle = Mathf.Lerp(targetRotation, initialRotation, value);

        if (isSnapping)
        {
            if (rotationSnapAngle > 0f)
            {
                angle = initialRotation + Mathf.Round((angle - initialRotation) / rotationSnapAngle) * rotationSnapAngle;
            }
        }

        switch(rotationDirection)
        {
            case SliderRotationDirection.X:
                targetObject.rotation = transformReference.rotation * Quaternion.Euler(angle, 0f, 0f);
                break;
            case SliderRotationDirection.Y:
                targetObject.rotation = transformReference.rotation * Quaternion.Euler(0f, angle, 0f);
                break;
            case SliderRotationDirection.Z:
                targetObject.rotation = transformReference.rotation * Quaternion.Euler(0f, 0f, angle);
                break;
        }

        float angleDifference = Mathf.Abs(Mathf.DeltaAngle(angle, targetRotation));

        if (!targetReached && angleDifference <= targetTolerance)
        {
            targetReached = true;
            TargetTransformReached?.Invoke();
        }
    }

    private void OnDestroy()
    {
        slider.onValueChanged.RemoveListener(RotateTarget);
    }
}
