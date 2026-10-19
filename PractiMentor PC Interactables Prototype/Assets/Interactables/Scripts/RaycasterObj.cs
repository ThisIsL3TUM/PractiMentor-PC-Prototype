using DG.Tweening;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

//this handles too many things (raycast, hovering, picking objs, movement, socket stuff), needs to be broken down 
public class RaycasterObj : MonoBehaviour
{
    private Camera playerCamera;
    private LayerMask layerMask;
    private int layerNumber;
    private InteractablesBase currentInteractable;

    //player
    [Header ("General Set Up")]
    [SerializeField] private GameObject player;
    [SerializeField] private Player playerController;

    //detects right & left hand positions to hold grabbables
    [Header("Hand Position Set Up")]
    [SerializeField] private Transform holdPosR;//for right hand
    [SerializeField] private Transform holdPosL;//for left hand

    [SerializeField] private Transform sliderHoldPosR;
    [SerializeField] private Transform sliderHoldPosL;

    //slots for the held object
    private GameObject heldObjR;
    private GameObject heldObjL;

    //slider reference
    [Header("Slider Set Up")]
    [SerializeField] private Slider interactionSlider;
    [SerializeField] private SliderTESTER sliderController;

    private bool sliderCompleted;

    private bool movingToSliderHands;


    private enum Hand
    {
        Left,
        Right
    }

    private GameObject GetHeldObject(Hand hand)
    {
        return hand == Hand.Right ? heldObjR : heldObjL;
    }

    private Transform GetHoldPosition(Hand hand)
    {
        Transform normalTarget = hand == Hand.Right ? holdPosR : holdPosL;

        Transform sliderTarget = hand == Hand.Right ? sliderHoldPosR : sliderHoldPosL;

        bool isSliderMode = playerController != null && playerController.CurrentMode == Player.PlayerMode.SliderInteraction;

        return isSliderMode ? sliderTarget : normalTarget;
    }

    private void SetHeldObject(Hand hand, GameObject obj)
    {
        if (hand == Hand.Right)
            heldObjR = obj;
        else 
            heldObjL = obj;
    }

    //range of raycast
    [Header ("Raycast")]
    [SerializeField] private float interactableRange = 15f;



    private void Awake()
    {
        if (playerController == null && player != null)
            playerController = player.GetComponent<Player>();

        //set up for layers
        layerMask = LayerMask.GetMask("Interactables");
        layerNumber = LayerMask.NameToLayer("Hold Layer");

        //reference validators (to check if things are in place
        //if (player == null)
            //Debug.LogError("Player reference is missing.", this);//player

        if (holdPosR == null)
            Debug.LogError("Right-hand hold position is missing.", this);//right hand

        if (holdPosL == null)
            Debug.LogError("Left-hand hold position is missing.", this);//left hand

        if (layerNumber == -1)
            Debug.LogError("The 'Hold Layer' doesn't exist!", this);//hold layer
    }

    private void Start()
    {
        playerCamera = Camera.main;

        if(interactionSlider != null)
            interactionSlider.gameObject.SetActive(false);
    }
    private void Update()
    {
        if (AnyObjectHeld())
        {
            MoveObject(Hand.Right);
            MoveObject(Hand.Left);
        }

        UpdateInteractionSlider();

        if(playerController != null && playerController.CurrentMode == Player.PlayerMode.SliderInteraction)
        {
            ClearCurrentInteractable();
            return;
        }

        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactableRange, layerMask))
        {
            //highlighting logic
            switch (hit.transform.gameObject.tag)
            {
                case "Grabbable":
                case "Clickable":
                case "Socket" when AnyObjectHeld():
                    {
                        if (hit.transform.TryGetComponent(out InteractablesBase nextInteractable))
                        {
                            if (currentInteractable != nextInteractable)
                            {
                                currentInteractable?.SetHighlighted(false);

                                currentInteractable = nextInteractable;
                                currentInteractable.SetHighlighted(true);
                            }
                        }

                        break;
                    }
            }

            if (!hit.transform.CompareTag("Grabbable") && !hit.transform.CompareTag("Clickable") && !(hit.transform.CompareTag("Socket") && AnyObjectHeld()))
            {
                ClearCurrentInteractable();
            }

            
            if (Input.GetKeyDown(KeyCode.Mouse0))
            {

                if (hit.transform.CompareTag("Grabbable"))
                {
                    //checks if things are grabbed or not
                    if (TryFindFreeHand(hit.transform.gameObject, out Hand selectedHand))
                    {
                        Debug.Log("You grabbed it!");
                        PickUpObject(hit.transform.gameObject, selectedHand);

                    }
                    else
                    {
                        Debug.LogWarning("You already grabbed something :]");
                    }

                }
                else if (hit.transform.CompareTag("Clickable"))
                {
                    Debug.Log("You clicked something!");
                }
                else if (hit.transform.CompareTag("Socket"))
                {
                    Socket socket = hit.transform.GetComponentInParent<Socket>();

                    //checks socket state 
                    if (socket == null)
                    {
                        Debug.LogWarning("The socket object has no Socket component.");
                        return;
                    }

                    if (TryFindHeldHand(out Hand selectedHand))
                    {
                        GameObject heldObject = GetHeldObject(selectedHand);

                        if (socket.TryOccupySocket(heldObject, out Transform placementPoint))
                        {
                            PlaceHeldObject(placementPoint, selectedHand);
                        }
                        else
                        {
                            Debug.Log("There is already something here...");
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

    //function to pick up object
    void PickUpObject(GameObject pickUpObj, Hand hand)
    {
        if(playerController != null && playerController.CurrentMode == Player.PlayerMode.SliderInteraction)
            return;

        if (pickUpObj == null)
            return;

        if (GetHeldObject(hand) != null)
            return;

        if(!CanUseHand(pickUpObj, hand)) 
            return;

        SetHeldObject(hand, pickUpObj);

        Transform holdPosition = GetHoldPosition(hand);
        pickUpObj.transform.DOMove(endValue: holdPosition.position, 0.5f).SetEase(Ease.InOutSine);
        pickUpObj.layer = layerNumber;

        Collider objectCollider = pickUpObj.GetComponent<Collider>();
        Collider playerCollider = player.GetComponent<Collider>();

        if (objectCollider != null && playerCollider != null)
            Physics.IgnoreCollision(objectCollider, playerCollider,true);

        
    }

    //fuction to move held grabbable
    void MoveObject(Hand hand)
    {
        if (movingToSliderHands)
            return;

        GameObject heldObject = GetHeldObject(hand);

        if (heldObject == null)
            return;

        heldObject.transform.position = GetHoldPosition(hand).position; 
    }

    //function to place held grabbable (with the animation, assisted by Socket.cs (will be up for modification)
    void PlaceHeldObject(Transform placementPoint, Hand hand)
    {
        GameObject heldObject = GetHeldObject(hand);

        if (heldObject == null)
            return;

        Collider objectCollider = heldObject.GetComponent<Collider>();
        Collider playerCollider = player.GetComponent<Collider>();

        if (objectCollider != null && playerCollider != null)
            Physics.IgnoreCollision(objectCollider, playerCollider, false);

        heldObject.layer = 0;
        heldObject.transform.DOMove(placementPoint.position, 0.5f).SetEase(Ease.InOutSine);

        SetHeldObject(hand, null);
    }

    //function to clear the highlighter properly from interactable objects
    private void ClearCurrentInteractable()
    {
        if (currentInteractable == null)
            return;

        currentInteractable.SetHighlighted(false);
        currentInteractable = null;
    }

    private bool TryFindHeldHand(out Hand hand)
    {
        if (heldObjR != null)
        {
            hand = Hand.Right;
            return true;
        }

        if (heldObjL != null)
        {
            hand = Hand.Left;
            return true;
        }

        hand = default;
        return false;
    }

    private bool TryFindFreeHand(GameObject objectToPickUp, out Hand hand)
    {
        if(CanUseHand(objectToPickUp, Hand.Right) && GetHeldObject(Hand.Right) == null)
        {
            hand = Hand.Right;
            return true;
        }

        if(CanUseHand(objectToPickUp,Hand.Left) && GetHeldObject(Hand.Left) == null)
        {
            hand = Hand.Left;
            return true;
        }

        hand = default;
        return false;
    }


    private bool CanUseHand(GameObject obj, Hand hand)
    {
        Grabbable grabbable = obj.GetComponentInParent<Grabbable>();

        if(grabbable == null) 
            return false;

        return grabbable.HandDirection switch
        {
            Grabbable.GrabbableHandDirection.Both => true,
            Grabbable.GrabbableHandDirection.Left => hand == Hand.Left,
            Grabbable.GrabbableHandDirection.Right => hand == Hand.Right,
            _ => false
        };
    }

    private bool AnyObjectHeld()
    {
        return heldObjR != null || heldObjL != null;
    }

    private void UpdateInteractionSlider()
    {
        SliderTarget rightSliderTarget = heldObjR?.GetComponentInParent<SliderTarget>();

        bool canUseSlider = heldObjR != null && heldObjL != null && CanInteract(heldObjR) && CanInteract(heldObjL) && rightSliderTarget != null;

        if (!canUseSlider)
        {
            sliderCompleted = false;

            sliderController.TargetTransformReached -= OnSliderTargetReached;
            sliderController.ClearTarget();
        }

        bool showSlider = canUseSlider && !sliderCompleted;

        if (interactionSlider != null)
            interactionSlider.gameObject.SetActive(showSlider);

        if (showSlider)
        {
            sliderController.TargetTransformReached -= OnSliderTargetReached;
            sliderController.TargetTransformReached += OnSliderTargetReached;

           SliderTarget sliderTarget = heldObjR.GetComponentInParent<SliderTarget>();

            if(sliderTarget != null)
                sliderController.SetTarget(sliderTarget);
        }

        bool enteringSliderMode = showSlider && playerController != null && playerController.CurrentMode != Player.PlayerMode.SliderInteraction;

        if (playerController != null)
        {
            playerController.SetPlayerState(
                showSlider
                ? Player.PlayerMode.SliderInteraction
                : Player.PlayerMode.Navigate
                );
        }

        if (enteringSliderMode)
            MoveHeldObjectsToSliderHands();

    }

    private void OnSliderTargetReached()
    {
        sliderCompleted = true;

        if(interactionSlider != null)
            interactionSlider.gameObject.SetActive(false);

        sliderController.ClearTarget();

        if(playerController != null)
            playerController.SetPlayerState(Player.PlayerMode.Navigate);
    }

    private bool CanInteract (GameObject obj)
    {
        Grabbable grabbable = obj.GetComponentInParent<Grabbable>();
        return grabbable != null && grabbable.CanThisInteract;
    }

    private void MoveHeldObjectsToSliderHands()
    {
        movingToSliderHands = true;

        DG.Tweening.Sequence sequence = DG.Tweening.DOTween.Sequence();

        if (heldObjR != null && sliderHoldPosR != null)
        {
            sequence.Join(heldObjR.transform.DOMove(sliderHoldPosR.position, 0.5f));
        }

        if (heldObjL != null && sliderHoldPosL != null)
        {
            sequence.Join(heldObjL.transform.DOMove(sliderHoldPosL.position, 0.5f));

        }
        
        sequence.SetEase(Ease.InOutSine);
        sequence.OnComplete(() => movingToSliderHands = false);
    }
}
