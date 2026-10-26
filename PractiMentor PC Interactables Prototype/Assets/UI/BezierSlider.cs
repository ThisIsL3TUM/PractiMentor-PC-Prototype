using UnityEngine;
using UnityEngine.UIElements;
using DG.Tweening;

public class BezierSlider : UnityEngine.UIElements.Slider
{
    [Header("Reset")]
    public float resetDelay = 0.75f;
    public float resetDuration = 0.75f;
    public float resetValue = 1f;

    [Header("Drag Adjustments")]
    public float dragFollowDuration = 0.10f;

    [Header("Brezier Shape")]
    public Vector2 controlPoint1 = new(0.3f, 0.1f);
    public Vector2 controlPoint2 = new(0.7f, 0.9f);

    [Header("Appearance")]
    public Color trackColor = new Color(1f, 0.94f, 0.85f);//antiqueWhite
    public Color curveColor = new Color(0.4f, 0.85f, 1f); //aliceBlue
    public Color knobColor = Color.silver;
    public Color fillingColor = Color.orange;
    public int curveSegments = 32;

    [Header("Track Appearance")]
    public float backgroundTrackWidth = 10f;
    public float curveTrackWidth = 4f;
    public float knobSize = 20f;

    public bool IsPointerHeld { get; private set; }

    private VisualElement curveElement;
    private VisualElement knobElement;

    private int resetVersion;

    private Tween resetTween;

    private bool automaticResetEnabled = true;

    private IVisualElementScheduledItem dragFollowSchedule;
    private float pointerTargetValue;

    private float timedProgress;

    private float pointerGrabOffset;

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

        knobElement.style.backgroundColor = Color.cadetBlue;

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

    public void SetAutomaticResetEnabled (bool enabled)
    {
        automaticResetEnabled = enabled;

        if (!enabled)
        {
            resetTween?.Kill();
            resetTween = null;
            resetVersion++;
        }
    }

    private Vector2 GetSliderLocalPosition (Vector2 sliderPosition)
    {
        return this.WorldToLocal(sliderPosition);
    }

    private void OnPointerDown(PointerDownEvent evt)
    {

        if (evt.button != 0)
            return;

        Vector2 localPosition = GetSliderLocalPosition(evt.position);

        bool clickedKnob = knobElement.layout.Contains(localPosition);

        if (!clickedKnob)
        { 
            evt.StopPropagation();
            return;
        }

        pointerGrabOffset = localPosition.y - knobElement.layout.center.y;

        IsPointerHeld = true;

        resetTween?.Kill();
        resetTween = null;

        resetVersion++;

        this.CapturePointer(evt.pointerId);

        pointerTargetValue = GetValueFromPointer(new Vector2 (localPosition.x, localPosition.y - pointerGrabOffset));

        StartDragFollow();

        evt.StopPropagation();
    }

    private void OnPointerMove(PointerMoveEvent evt)
    {
        if (!this.HasPointerCapture(evt.pointerId))
            return;

        Vector2 localPosition = GetSliderLocalPosition(evt.position);

        pointerTargetValue = GetValueFromPointer(new Vector2(localPosition.x, localPosition.y - pointerGrabOffset));

        evt.StopPropagation();
    }

    private void OnPointerUp(PointerUpEvent evt)
    {
        if (!this.HasPointerCapture(evt.pointerId))
            return;

        IsPointerHeld = false;

        dragFollowSchedule?.Pause();

        this.ReleasePointer(evt.pointerId);

        evt.StopPropagation();

        if(automaticResetEnabled)
            ScheduleReset();
    }

    private float GetValueFromPointer(Vector2 pointerPosition)
    {
        float height = contentRect.height;

        if (height <= 0f)
            return value;

        float normalizedValue = 1f - Mathf.Clamp01(pointerPosition.y / height);

        return Mathf.Lerp(lowValue, highValue, normalizedValue);
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
        .SetEase(Ease.InOutCirc)
        .OnComplete(() =>
        {
            if (scheduledVersion == resetVersion)
                resetTween = null;
        });
    }
    private Vector2 EvaluateBezier(float t)
    {
        //for horizontal path
        //Vector2 start = new(0f, 0.5f);
        //Vector2 end = new(1f, 0.5f);

        //for vertical path
        Vector2 start = new(0.5f, 0f);
        Vector2 end = new(0.5f, 1f);

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

        DrawBezierPath(painter, trackColor, backgroundTrackWidth, 1f);
        DrawBezierPath(painter, curveColor, curveTrackWidth, 1f);

        if(timedProgress > 0f)
        {
            DrawBezierPath(painter, fillingColor, curveTrackWidth, timedProgress);
        }
    }

    private void DrawBezierPath(Painter2D painter, Color color, float width, float endT)
    {
        painter.strokeColor = color;
        painter.lineWidth = width;

        painter.BeginPath();

        int segments = Mathf.Max(2, curveSegments);
        int visibleSegments = Mathf.Max(1, Mathf.RoundToInt(segments * endT));

        for(int i = 0; i <= visibleSegments; i++)
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

        knobElement.style.backgroundColor = knobColor;

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

    private void StartDragFollow()
    {
        dragFollowSchedule ??= schedule.Execute(UpdateDragFollow).Every(16);

        dragFollowSchedule.Resume();
    }

    private void UpdateDragFollow()
    {
        if (!IsPointerHeld)
            return;

        float duration = Mathf.Max(0.01f, dragFollowDuration);

        float followAmount = 1f - Mathf.Exp(-Time.unscaledDeltaTime / duration);

        value = Mathf.Lerp(value, pointerTargetValue, followAmount);
    }

    public void SetTimedProgress(float progress)
    {
        timedProgress = Mathf.Clamp01(progress);
        curveElement.MarkDirtyRepaint();
    }

    public void ResetTimedProgress()
    {
        timedProgress = 0f;
        curveElement.MarkDirtyRepaint();
    }
}
