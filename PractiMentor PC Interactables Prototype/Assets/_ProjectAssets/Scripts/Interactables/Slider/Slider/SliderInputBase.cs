using System;
using UnityEngine;

public abstract class SliderInputBase : MonoBehaviour
{
    public abstract bool IsPointerHeld { get; }
    public abstract float NormalizedValue { get; }

    public event Action<float> ValueChanged;

    protected void RaiseValueChanged(float value)
    {
        ValueChanged?.Invoke(value);
    }

    public abstract void SetValue(float value);
    public abstract void SetVisible(bool visible);
    public abstract void ResetSlider();
    public virtual void SetTimedProgress(float progress) { }
    public virtual void ResetTimedProgress() { }
    public virtual void SetAutomaticResetEnabled(bool enabled) { }
    public virtual void FollowObjectOnScreen(Transform target, Camera camera, Vector2 offset){}
    public virtual void FollowWorldTarget(Transform target, Vector3 offset){}
}
