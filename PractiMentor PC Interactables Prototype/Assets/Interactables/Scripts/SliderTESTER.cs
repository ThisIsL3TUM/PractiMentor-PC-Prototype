using UnityEngine;
using UnityEngine.UI;

public class SliderTESTER : MonoBehaviour
{
    [SerializeField] private Slider slider;
    [SerializeField] private Transform rotationReference;
    [SerializeField] private float initialRotation = 0;
    [SerializeField] private float targetRotation = 90f;

    private Transform targetObject;

    private void Awake()
    {
        slider.onValueChanged.AddListener(RotateTarget);
    }
    
    public void SetTarget(Transform target)
    {
        targetObject = target;
    }

    public void ClearTarget()
    {
        targetObject = null;
    }

    private void RotateTarget(float value)
    {
        if (targetObject == null)
            return;

        float angle = Mathf.Lerp(targetRotation, initialRotation, value);

        targetObject.rotation = rotationReference.rotation * Quaternion.Euler(0f, 0f, angle);
    }

    private void OnDestroy()
    {
        slider.onValueChanged.RemoveListener(RotateTarget);
    }
}
