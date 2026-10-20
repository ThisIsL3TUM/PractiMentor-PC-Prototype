using UnityEngine;
using UnityEngine.UIElements;

public class BezierSliderController : MonoBehaviour
{
    [SerializeField] private UIDocument UIDocument;
    [SerializeField] private Transform targetObject;
    [SerializeField] private float initialValue = 0f;
    [SerializeField] float targetValue = 90f;

    private BezierSlider slider;

    private void OnEnable()
    {
        VisualElement root = UIDocument.rootVisualElement;

        slider = root.Q<BezierSlider>("interaction-slider");

        if(slider == null)
        {
            Debug.LogError("Could not find the Bezier Slider!");
            return;
        }

        slider.lowValue = 0f;
        slider.highValue = 1f;
        slider.value = 1f;

        slider.RegisterValueChangedCallback(OnSliderValueChanged);
    }

    private void OnSliderValueChanged(ChangeEvent<float> changeEvent)
    {
        float normalizedValue = Mathf.InverseLerp(slider.lowValue, slider.highValue, changeEvent.newValue);

        float progress = 1f - normalizedValue;

        float currentRotation = Mathf.Lerp(initialValue, targetValue, progress);

        targetObject.localRotation = Quaternion.Euler(0f, currentRotation, 0f);
    }

    private void OnDisable()
    {
        if(slider != null)
            slider.UnregisterValueChangedCallback(OnSliderValueChanged);
    }
}
