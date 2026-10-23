using UnityEngine;

public class Grabbable : MonoBehaviour
{
    public enum GrabbableHandDirection
    {
        Left,
        Right,
        Both
    }

    [Header("General Set Up")]
    [SerializeField] private GrabbableHandDirection grabbableHandDirection = GrabbableHandDirection.Both;

    [Header("Interaction Set Up")]
    [SerializeField] private bool canObjInteract = false;
    [SerializeField] private bool canObjDoActionMore = false;
    [SerializeField] private int amountAction = 3;

    [Header("Timed Interaction Elements")]
    [SerializeField] private bool isInteractionTimed = false;
    [SerializeField] private float timedVal = 3f;


    public bool CanThisInteract => canObjInteract;
    public bool CanActionMultiple => canObjDoActionMore;
    public int ActionCount => amountAction;
    public bool IsInteractionTimed => isInteractionTimed;
    public float TimeAmount => timedVal;

    public GrabbableHandDirection HandDirection => grabbableHandDirection;
}
