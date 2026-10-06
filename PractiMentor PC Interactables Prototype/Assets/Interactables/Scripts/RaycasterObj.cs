using DG.Tweening;
using UnityEngine;

//this handles too many things (raycast, hovering, picking objs, movement, socket stuff), needs to be broken down 
public class RaycasterObj : MonoBehaviour
{
    private Camera playerCamera;
    private LayerMask layerMask;
    private int layerNumber;
    private InteractablesBase currentInteractable;

    //player
    [SerializeField] private GameObject player;

    //detects right & left hand positions to hold grabbables
    [SerializeField] private Transform holdPosR;//for right hand
    [SerializeField] private Transform holdPosL;//for left hand

    //slots for the held object
    private GameObject heldObjR;
    private GameObject heldObjL;

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
        return hand == Hand.Right ? holdPosR : holdPosL;
    }

    private void SetHeldObject(Hand hand, GameObject obj)
    {
        if (hand == Hand.Right)
            heldObjR = obj;
        else 
            heldObjL = obj;
    }

    //range of raycast
    [SerializeField] private float interactableRange = 15f;



    private void Awake()
    {
        //set up for layers
        layerMask = LayerMask.GetMask("Interactables");
        layerNumber = LayerMask.NameToLayer("Hold Layer");

        //reference validators (to check if things are in place
        if (player == null)
            Debug.LogError("Player reference is missing.", this);//player

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
    }
    private void Update()
    {
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

            
            if (TryGetPressedHand(out Hand pressedHand))
            {

                if (hit.transform.CompareTag("Grabbable"))
                {
                    //checks if things are grabbed or not
                    if (GetHeldObject(pressedHand) == null)
                    {
                        Debug.Log("You grabbed it!");
                        PickUpObject(hit.transform.gameObject, pressedHand);
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

                    GameObject heldObject = GetHeldObject(pressedHand);

                    if (heldObject == null)
                    {
                        Debug.Log("There is no item to place.");
                        return;
                    }

                    if (socket.TryOccupySocket(heldObject, out Transform placementPoint))
                    {
                        PlaceHeldObject(placementPoint, pressedHand);
                    }
                    else
                    {
                        Debug.Log("There is already something here...");
                    }
                }

            }

        }
        else
        {

            ClearCurrentInteractable();

        }

        if (AnyObjectHeld())
        {
            MoveObject(Hand.Right);
            MoveObject(Hand.Left);
        }

    }

    //function to pick up object
    void PickUpObject(GameObject pickUpObj, Hand hand)
    {
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
        GameObject heldObject = GetHeldObject(hand);

        if (heldObject == null)
            return;

        heldObject.transform.position = GetHoldPosition(hand).position;
        heldObject.transform.rotation = Quaternion.identity; //for obj rotation, need to double check it, sets up the initial rotation of the object 
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

    private bool TryGetPressedHand(out Hand hand)
    {
        if (Input.GetKeyDown(KeyCode.Mouse0))
        {
            hand = Hand.Right;
            return true;
        }

        if (Input.GetKeyDown(KeyCode.Mouse1))
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
}
