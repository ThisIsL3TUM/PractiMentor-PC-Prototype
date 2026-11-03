using UnityEngine;

public class SliderTarget : MonoBehaviour
{
    public enum SliderRepresentation
    {
        _2D,
        _3D
    }
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

    //SLIDER DIMENSION
    [Header("Slider Dimension")]
    [SerializeField] SliderRepresentation representation = SliderRepresentation._2D;

    //MOVEMENT
    [Header("Transform")]
    [SerializeField] private TransformProperty property = TransformProperty.Rotation;
    [SerializeField] private Axis axis = Axis.X;
    [SerializeField] private bool useLocalSpace = true;
    [SerializeField] private float tolerance = 1f;//for scale, this needs to be a lot smaller (e.g. 0.01)

    [Header("Slider Values")]
    [SerializeField] private float initialValue = 0f;
    [SerializeField] protected float targetValue = 60f;

    //SNAPPING
    [Header("Rotation Snapping")]
    [SerializeField] private bool snappingActive;
    [Range(0, 90)][SerializeField] private float rotationSnapAngle = 15f;

    public SliderRepresentation Representation => representation;
    public TransformProperty Property => property;
    public Axis TransformAxis => axis;
    public bool UseLocalSpace => useLocalSpace;
    public float InitialValue => initialValue;
    public float TargetValue => targetValue;
    public bool SnappingActive => snappingActive;
    public float RotationSnapAngle => rotationSnapAngle;
    public float Tolerance => tolerance;

}
