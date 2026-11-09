using UnityEngine;
using UnityEngine.UIElements;

public class BezierSliderInteraction : SliderInputBase
{
    [Header("UI Toolkit")]
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private string sliderElementName = "interaction-slider";

    private BezierSlider slider;

    public override bool IsPointerHeld => slider != null && slider.IsPointerHeld;

    public override float NormalizedValue => slider == null
        ? 1f
        : Mathf.InverseLerp(slider.lowValue,slider.highValue,slider.value);

    public override void SetValue(float value)
    {
        if (slider == null)
            return;

        float sliderValue = Mathf.Lerp(slider.lowValue,slider.highValue,Mathf.Clamp01(value));

        slider.SetValueWithoutNotify(sliderValue);
    }

    public override void ResetSlider()
    {
        if (slider == null)
            return;

        slider.CancelReset();
        slider.SetValueWithoutNotify(slider.highValue);
    }

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

            backgroundTrackWidth = 96f,
            curveTrackWidth = 42f,
            knobSize = 90f,

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
    public override void SetVisible(bool visible)
    {
        if (slider == null)
            return;

        slider.style.display = visible
            ? DisplayStyle.Flex
            : DisplayStyle.None;
    }

    public override void FollowObjectOnScreen(Transform target, Camera camera, Vector2 offset)
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

        float panelX = screenPosition.x / Screen.width * panelWidth;

        float panelY = (1f - screenPosition.y / Screen.height) * panelHeight;

        float sliderHeight = slider.resolvedStyle.height;

        slider.style.position = Position.Absolute;
        slider.style.left = panelX + offset.x;
        slider.style.top = panelY - sliderHeight * 0.5f + offset.y;
    }

    public override void SetAutomaticResetEnabled(bool enabled)
    {
        slider?.SetAutomaticResetEnabled(enabled);
    }

    public override void SetTimedProgress (float progress)
    {
        slider?.SetTimedProgress(progress);
    }

    public override void ResetTimedProgress()
    {
        slider?.ResetTimedProgress();
    }

    private void OnDisable()
    {
        if (slider != null)
        {
            slider.SetAutomaticResetEnabled(true);
            slider.UnregisterValueChangedCallback(OnSliderValueChanged);
        }
    }
    private void OnSliderValueChanged(ChangeEvent<float> evt)
    {
        float normalizedValue = Mathf.InverseLerp(slider.lowValue,slider.highValue,evt.newValue);

        RaiseValueChanged(normalizedValue);
    }

}
