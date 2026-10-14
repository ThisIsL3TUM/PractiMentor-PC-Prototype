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

    //USED FOR TESTING PURPOSES, will be up for change!!!!!
    [SerializeField] private bool canObjInteract = false;

    public bool CanThisInteract => canObjInteract;

    public GrabbableHandDirection HandDirection => grabbableHandDirection;
}
