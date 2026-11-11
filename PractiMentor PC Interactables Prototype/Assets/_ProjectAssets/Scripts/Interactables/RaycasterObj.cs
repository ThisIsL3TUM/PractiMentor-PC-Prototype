using UnityEngine;
using System;
using UnityEngine.InputSystem;

//this handles too many things (raycast, hovering, picking objs, movement, socket stuff), needs to be broken down 
public class RaycasterObj : MonoBehaviour
{
    public event Action<InteractionCompletedEvent> InteractionCompleted;

    private Camera playerCamera;
    private LayerMask layerMask;
    private int layerNumber;
    private InteractablesHighlighter currentInteractable;

    //player
    [Header ("General Set Up")]
    [SerializeField] private GameObject player;
    [SerializeField] private Player playerController;
    [SerializeField] private HandController handController;
    [SerializeField] private SliderInteractionHandler sliderInteractionHandler;
    [SerializeField] private SocketInteractionHandler socketInteractionHandler;

    [Header("Workflow")]
    [SerializeField] private InteractionSequenceController sequenceController;

    //range of raycast
    [Header ("Raycast")]
    [SerializeField] private float interactableRange = 15f;

    [Header("Mug")]
    [SerializeField] private GameObject drinkMug;

    private void Awake()
    {
        if (playerController == null && player != null)
            playerController = player.GetComponent<Player>();

        //set up for layers
        layerMask = LayerMask.GetMask("Interactables");
        layerNumber = LayerMask.NameToLayer("Hold Layer");

        if (handController == null && player != null)
        {
            handController = player.GetComponent<HandController>();
        }

        if (handController == null)
        {
            Debug.LogError("HandController reference is missing.", this);
        }
    }

    private void Start()
    {
        Debug.Log("RaycasterObj started.", this);

        playerCamera = Camera.main;
    }
    private void Update()
    {
        //Debug.Log("RaycasterObj is updating.", this);

        if (sequenceController != null && sequenceController.IsInteractionLocked)
        {
            ClearCurrentInteractable();
            return;
        }

        if (handController != null && handController.AnyObjectHeld)
        {
            bool sliderMode = playerController != null && playerController.CurrentMode == Player.PlayerMode.SliderInteraction;

            handController.UpdateHeldObjects(sliderMode);
        }

        sliderInteractionHandler?.Tick();
        socketInteractionHandler?.Tick();

        if (playerController != null && playerController.CurrentMode == Player.PlayerMode.SliderInteraction)
        {
            ClearCurrentInteractable();
            return;
        }

        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactableRange, layerMask))
        {
            bool hitIsSocket = hit.transform.CompareTag("Socket");

            Socket hitSocket = hitIsSocket ? hit.transform.GetComponentInParent<Socket>() : null;

            bool socketCanAcceptHeldItem = false;

            if (hitSocket != null && handController.TryFindHeldHand(out HandController.Hand heldHand))
            {
                GameObject heldObject = handController.GetHeldObject(heldHand);
                GameObject grabbableRoot = GetGrabbableRoot(heldObject);

                socketCanAcceptHeldItem = hitSocket.CanAccept(grabbableRoot);
            }

            //highlighting logic
            bool canHighlightHit = hit.transform.CompareTag("Grabbable") || hit.transform.CompareTag("Clickable") || (hitIsSocket && socketCanAcceptHeldItem);

            if (canHighlightHit)
            {
                if(hit.transform.TryGetComponent(out InteractablesHighlighter nextInteractable))
                {
                    if(currentInteractable != nextInteractable)
                    {
                        currentInteractable?.SetHighlighted(false);

                        currentInteractable = nextInteractable;
                        currentInteractable.SetHighlighted(true);
                    }
                }
            }

            if (!canHighlightHit)
            {
                ClearCurrentInteractable();
            }

            bool leftMousePressed = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
            if (leftMousePressed)
            {

                if (hit.transform.CompareTag("Grabbable"))
                {
                    GameObject clickedRoot = GetGrabbableRoot(hit.transform.gameObject);

                    bool isDrinkMug = sequenceController != null && sequenceController.CurrentStep == InteractionSequenceController.Step.DrinkCoffee && clickedRoot == drinkMug;

                    if (isDrinkMug)
                    {
                        PickUpMugForDrinking(clickedRoot);
                    }
                    //checks if things are grabbed or not
                    else if (handController.TryFindFreeHand(hit.transform.gameObject, out HandController.Hand selectedHand))
                    {
                        bool sliderMode = playerController != null && playerController.CurrentMode == Player.PlayerMode.SliderInteraction;

                        handController.TryPickUp(hit.transform.gameObject, selectedHand, sliderMode);

                    }
                    else
                    {
                        Debug.LogWarning("You already grabbed something :]");
                    }

                }
                else if (hit.transform.CompareTag("Clickable"))
                {
                    Clickable clickable = hit.transform.GetComponentInParent<Clickable>();

                    if (clickable != null)
                    {
                        handController.ReportInteractionCompleted(new InteractionCompletedEvent(InteractionType.ClickablePressed, clickable.gameObject));
                    }
                }
                else if (hitIsSocket && socketCanAcceptHeldItem)
                {
                    Socket socket = hitSocket;

                    //checks socket state 
                    if (socket == null)
                    {
                        Debug.LogWarning("The socket object has no Socket component.");
                        return;
                    }

                    if (handController.TryFindHeldHand(out HandController.Hand selectedHand))
                    {
                        GameObject heldObject = handController.GetHeldObject(selectedHand);

                        if (socket.TryOccupySocket(heldObject, out Transform placementPoint))
                        {
                            socketInteractionHandler.TryPlaceHeldObject(socket, placementPoint, selectedHand);
                        }
                        else
                        {
                            Debug.Log("This socket can't accept this item :[");
                        }
                    }

                }

            }

        }
        else
        {

            ClearCurrentInteractable();

        }


    }
    //function to clear the highlighter properly from interactable objects
    private void ClearCurrentInteractable()
    {
        if (currentInteractable == null)
            return;

        currentInteractable.SetHighlighted(false);
        currentInteractable = null;
    }
    private GameObject GetGrabbableRoot(GameObject hitObject)
    {
        Grabbable grabbable =
            hitObject.GetComponentInParent<Grabbable>();

        return grabbable != null? grabbable.gameObject: hitObject;
    }

    private void PickUpMugForDrinking(GameObject mug)
    {
        if (socketInteractionHandler == null || socketInteractionHandler.MugSocket == null || mug == null)
        {
            Debug.LogWarning("Can't pick up the drinking mug, socket or mug is missing.");
            return;
        }

        Socket mugSocket = socketInteractionHandler.MugSocket;

        if (handController.HeldRight != null)
        {
            Debug.LogWarning("Right hand is already occupied.");
            return;
        }

        if (!mugSocket.ReleaseObject(mug))
        {
            Debug.LogWarning("The mug can't be released from its socket.");
            return;
        }

        handController.TryPickUp(mug,HandController.Hand.Right,sliderMode: false);
    }
}