using UnityEngine;
using UnityEngine.UIElements;

public class BezierSliderExample : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;

    private BezierSlider slider;

    private void OnEnable()
    {
        VisualElement root = uiDocument.rootVisualElement;

        slider = new BezierSlider
        {
            name = "interaction-slider",

            //value settings
            lowValue = 0f,
            highValue = 1f,
            value = 1f,

            //reset settings
            resetValue = 1f,
            resetDelay = 0.75f,

            //curve shape
            controlPoint1 = new Vector2(0.3f, 0.2f),
            controlPoint2 = new Vector2(0.7f, 0.9f),
            curveSegments = 32,

            //appearance
            backgroundTrackWidth = 16f,
            curveTrackWidth = 7f,
            knobSize = 30f,

            curveColor = new Color(0.4f, 0.85f, 1f),
            trackColor = new Color(1f, 0.94f, 0.85f)
        };

        slider.AddToClassList("bezier-slider");

        root.Add(slider);

        slider.RegisterValueChangedCallback(OnSliderValueChanged);
    }
    
    private void OnSliderValueChanged(ChangeEvent<float> changeEvent)
    {
        float normalizedValue = Mathf.InverseLerp(slider.lowValue, slider.highValue, changeEvent.newValue);
        Debug.Log($"Slider value: {changeEvent.newValue}");
    }
}
