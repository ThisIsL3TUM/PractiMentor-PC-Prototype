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

    [Header("Multiple Interaction Set Up")]
    [SerializeField] private bool canObjDoActionMore = false;
    [SerializeField, Range(1, 10)] private int amountAction = 3;
    [SerializeField, Min(0f)] private float delayBetweenSliderAction = 0.3f;

    [Header("Timed Interaction Elements")]
    [SerializeField] private bool isInteractionTimed = false;
    [SerializeField, Min(0f)] private float timedVal = 3f;

    //general
    public bool CanThisInteract => canObjInteract;
    
    //multiple action
    public bool CanActionMultiple => canObjDoActionMore;
    public int ActionCount => Mathf.Max(1, amountAction);
    public float DelayBetweenSliderAction => delayBetweenSliderAction;

    //timed action
    public bool IsInteractionTimed => isInteractionTimed;
    public float TimeAmount => timedVal;

    //hand direction
    public GrabbableHandDirection HandDirection => grabbableHandDirection;
}
