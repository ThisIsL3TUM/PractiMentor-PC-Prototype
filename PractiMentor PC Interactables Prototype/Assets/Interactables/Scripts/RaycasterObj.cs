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
    [SerializeField] private Transform holdPosL;//for left hand, to be implemented later

    //range of raycast
    [SerializeField] private float interactableRange = 15f;

    //when grabbable object is held
    private GameObject heldObj;


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
                case "Socket" when heldObj != null:
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

            if (!hit.transform.CompareTag("Grabbable") && !hit.transform.CompareTag("Clickable") && !(hit.transform.CompareTag("Socket") && heldObj != null))
            {
                ClearCurrentInteractable();
            }

            if (Input.GetKeyDown(KeyCode.Mouse0))
            {

                if (hit.transform.CompareTag("Grabbable"))
                {
                    //checks if things are grabbed or not
                    if (heldObj == null)
                    {
                        Debug.Log("You grabbed it!");
                        PickUpObject(hit.transform.gameObject);
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

                    if (heldObj == null)
                    {
                        Debug.Log("There is no item to place.");
                        return;
                    }

                    if (socket.TryOccupySocket(heldObj, out Transform placementPoint))
                    {
                        PlaceHeldObject(placementPoint);
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

        if (heldObj != null)
        {
            MoveObject();
        }

    }

    //function to pick up object
    void PickUpObject(GameObject pickUpObj)
    {
        if (pickUpObj)
        {
            heldObj = pickUpObj;

            heldObj.transform.DOMove(endValue: holdPosR.transform.position, 0.5f).SetEase(Ease.InOutSine);
            heldObj.layer = layerNumber;

            Physics.IgnoreCollision(heldObj.GetComponent<Collider>(), player.GetComponent<Collider>(), true);
        }
    }

    //fuction to move held grabbable
    void MoveObject()
    {
        heldObj.transform.position = holdPosR.transform.position;
        heldObj.transform.rotation = Quaternion.identity; //for obj rotation, need to double check it, sets up the initial rotation of the object 
    }

    //function to place held grabbable (with the animation, assisted by Socket.cs (will be up for modification)
    void PlaceHeldObject(Transform placementPoint)
    {
        if (heldObj == null)
            return;

        Physics.IgnoreCollision(heldObj.GetComponent<Collider>(), player.GetComponent<Collider>(), true);
        heldObj.layer = 0;
        heldObj.transform.DOMove(placementPoint.position, 0.5f).SetEase(Ease.InOutSine);
        heldObj = null;
    }

    //function to clear the highlighter properly from interactable objects
    private void ClearCurrentInteractable()
    {
        if (currentInteractable == null)
            return;

        currentInteractable.SetHighlighted(false);
        currentInteractable = null;
    }
}
