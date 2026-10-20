using UnityEngine;
using UnityEngine.UIElements;
using DG.Tweening;

public class BezierSlider : UnityEngine.UIElements.Slider
{
    [Header("Reset")]
    public float resetDelay = 0.75f;
    public float resetDuration = 0.75f;
    public float resetValue = 1f;

    [Header("Brezier Shape")]
    public Vector2 controlPoint1 = new(0.3f, 0.1f);
    public Vector2 controlPoint2 = new(0.7f, 0.9f);

    [Header("Appearance")]
    public Color trackColor = new Color(1f, 0.94f, 0.85f);//antiqueWhite
    public Color curveColor = new Color(0.4f, 0.85f, 1f); //aliceBlue
    public int curveSegments = 32;

    [Header("Track Appearance")]
    public float backgroundTrackWidth = 10f;
    public float curveTrackWidth = 4f;
    public float knobSize = 20f;

    private VisualElement curveElement;
    private VisualElement knobElement;

    private int resetVersion;

    private Tween resetTween;

    public BezierSlider(string label = null) : base(label)
    {
        AddToClassList("bezier-slider");

        curveElement = new VisualElement
        {
            name = "bezier-curve",
            pickingMode = PickingMode.Ignore
        };

        curveElement.style.position = Position.Absolute;
        curveElement.style.left = 0;
        curveElement.style.right = 0;
        curveElement.style.top = 0;
        curveElement.style.bottom = 0;

        curveElement.generateVisualContent += DrawCurve;

        hierarchy.Add(curveElement);

        knobElement = new VisualElement
        {
            name = "bezier-knob",
            pickingMode = PickingMode.Ignore
        };

        knobElement.style.position = Position.Absolute;
        knobElement.style.width = knobSize;
        knobElement.style.height = knobSize;

        float radius = knobSize * 0.5f;

        knobElement.style.backgroundColor = curveColor;

        knobElement.style.borderTopLeftRadius = radius;
        knobElement.style.borderTopRightRadius = radius;
        knobElement.style.borderBottomLeftRadius = radius;
        knobElement.style.borderBottomRightRadius = radius;

        hierarchy.Add(knobElement);

        RegisterCallback<PointerDownEvent>(OnPointerDown, TrickleDown.TrickleDown);

        RegisterCallback<PointerMoveEvent>(OnPointerMove, TrickleDown.TrickleDown);

        RegisterCallback<PointerUpEvent>(OnPointerUp, TrickleDown.TrickleDown);

        this.RegisterValueChangedCallback(_ =>
        {
            RefreshKnob();
            curveElement.MarkDirtyRepaint();
        });

        RegisterCallback<GeometryChangedEvent>(_ =>
        {
            RefreshKnob();
            curveElement.MarkDirtyRepaint();
        });
    }

    private void OnPointerDown(PointerDownEvent evt)
    {

        if (evt.button != 0)
            return;

        resetTween.Kill();
        resetTween = null;

        resetVersion++;

        this.CapturePointer(evt.pointerId);

        SetValueFromPointer(evt.localPosition.x);

        evt.StopPropagation();
    }

    private void OnPointerMove(PointerMoveEvent evt)
    {
        if (!this.HasPointerCapture(evt.pointerId))
            return;

        SetValueFromPointer(evt.localPosition.x);

        evt.StopPropagation();
    }

    private void OnPointerUp(PointerUpEvent evt)
    {
        if (!this.HasPointerCapture(evt.pointerId))
            return;

        this.ReleasePointer(evt.pointerId);

        evt.StopPropagation();

        ScheduleReset();
    }

    private void SetValueFromPointer(float pointerX)
    {
        float width = contentRect.width;

        if (width <= 0f)
            return;

        float normalizedValue = Mathf.Clamp01(pointerX / width);

        value = Mathf.Lerp(lowValue, highValue, normalizedValue);
    }

    private void ScheduleReset()
    {
        resetTween?.Kill();

        int scheduledVersion = ++resetVersion;

        resetTween = DOTween.To(() => value,
            newValue => value = newValue,
            resetValue,
            resetDuration
        )
        .SetDelay(resetDelay)
        .SetEase(Ease.InOutSine)
        .OnComplete(() =>
        {
            if (scheduledVersion == resetVersion)
                resetTween = null;
        });
    }
    private Vector2 EvaluateBezier(float t)
    {
        Vector2 start = new(0f, 0.5f);
        Vector2 end = new(1f, 0.5f);

        float inverse = 1f - t;

        return
            inverse * inverse * inverse * start +
            3f * inverse * inverse * t * controlPoint1 +
            3f * inverse * t * t * controlPoint2 +
            t * t * t * end;
    }

    private Vector2 ToPixelPosition(Vector2 normalized)
    {
        return new Vector2(normalized.x * curveElement.contentRect.width,
            (1f - normalized.y) * curveElement.contentRect.height);
    }

    private void DrawCurve(MeshGenerationContext context)
    {
        Rect rect = curveElement.contentRect;

        if (rect.width <= 0f || rect.height <= 0)
            return;

        Painter2D painter = context.painter2D;

        painter.lineCap = LineCap.Round;

        DrawBezierPath(painter, trackColor, backgroundTrackWidth);
        DrawBezierPath(painter, curveColor, curveTrackWidth);
    }

    private void DrawBezierPath(Painter2D painter, Color color, float width)
    {
        painter.strokeColor = color;
        painter.lineWidth = width;

        painter.BeginPath();

        int segments = Mathf.Max(2, curveSegments);

        for(int i = 0; i <= segments; i++)
        {
            float t = i / (float) segments;

            Vector2 normalized = EvaluateBezier(t);
            Vector2 pixel = ToPixelPosition(normalized);

            if(i == 0)
                painter.MoveTo(pixel);
            else 
                painter.LineTo(pixel);
        }
        painter.Stroke();
    }

    private void RefreshKnob()
    {
        if (curveElement == null)
            return;

        if (curveElement.contentRect.width <= 0f)
            return;

        knobElement.style.width = knobSize;
        knobElement.style.height = knobSize;

        float radius = knobSize * 0.5f;

        knobElement.style.backgroundColor = curveColor;

        knobElement.style.borderTopLeftRadius = radius;
        knobElement.style.borderTopRightRadius = radius;
        knobElement.style.borderBottomLeftRadius = radius;
        knobElement.style.borderBottomRightRadius = radius;

        float normalizedValue = Mathf.InverseLerp(lowValue, highValue, value);

        Vector2 normalizedPosition = EvaluateBezier(normalizedValue);

        Vector2 pixelPosition = ToPixelPosition(normalizedPosition);

        float halfKnobSize = knobSize * 0.5f;

        knobElement.style.left = pixelPosition.x - halfKnobSize;
        knobElement.style.top = pixelPosition.y - halfKnobSize;

    }

}
