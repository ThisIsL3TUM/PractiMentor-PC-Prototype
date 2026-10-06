using UnityEngine;

public class Grabbable : MonoBehaviour
{
    public enum GrabbableHandDirection
    {
        Left,
        Right,
        Both
    }

    [SerializeField] private GrabbableHandDirection grabbableHandDirection = GrabbableHandDirection.Both;

    public GrabbableHandDirection HandDirection => grabbableHandDirection;
}
