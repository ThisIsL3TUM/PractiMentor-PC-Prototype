using UnityEngine;

public class SliderTarget : MonoBehaviour
{
    public enum TransformProperty
    {
        Position, 
        Rotation, 
        Scale
    }
    public enum Axis
    {
        X, Y, Z
    }

    //MOVEMENT - ROTATION
    [Header("Transform")]
    [SerializeField] private TransformProperty property = TransformProperty.Rotation;
    [SerializeField] private Axis axis = Axis.X;
    [SerializeField] private bool useLocalSpace = true;

    [Header("Slider Values")]
    [SerializeField] private float initialValue = 0f;
    [SerializeField] protected float targetValue = 60f;

    //SNAPPING
    [Header("Rotation Snapping")]
    [SerializeField] private bool snappingActive;
    [Range(0, 90)][SerializeField] private float rotationSnapAngle = 15f;
    [SerializeField] private float tolerance = 1f;

    public TransformProperty Property => property;
    public Axis TransformAxis => axis;
    public bool UseLocalSpace => useLocalSpace;
    public float InitialValue => initialValue;
    public float TargetValue => targetValue;
    public bool SnappingActive => snappingActive;
    public float RotationSnapAngle => rotationSnapAngle;
    public float Tolerance => tolerance;

}
