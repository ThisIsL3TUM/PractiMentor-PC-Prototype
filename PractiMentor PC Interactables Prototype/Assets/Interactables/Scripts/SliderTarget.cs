using UnityEngine;

public class SliderTarget : MonoBehaviour
{
    public enum Axis
    {
        X, Y, Z
    }

    //MOVEMENT - ROTATION
    [Header("Movement - Rotation")]
    [SerializeField] private Axis axis = Axis.X;
    [SerializeField] private float initialRotation;
    [SerializeField] protected float targetRotation = 60f;

    //SNAPPING
    [Header("Snapping")]
    [SerializeField] private bool snappingActive;
    [Range(0, 90)][SerializeField] private float rotationSnapAngle = 15f;
    [SerializeField] private float tolerance = 1f;

    public Axis RotationAxis => axis;
    public float InitialRotation => initialRotation;
    public float TargetRotation => targetRotation;
    public bool SnappingActive => snappingActive;
    public float RotationSnapAngle => rotationSnapAngle;
    public float Tolerance => tolerance;

}
