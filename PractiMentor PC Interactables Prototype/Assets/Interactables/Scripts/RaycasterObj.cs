using UnityEngine;
using DG.Tweening;

public class RaycasterObj : MonoBehaviour
{ 
    Camera playerCamera;
    LayerMask layerMask;
    private int layerNumber;
    InteractablesBase interactable;

    //player
    [SerializeField] private GameObject player;

    //detects right & left hand positions to hold grabbables
    [SerializeField] private Transform holdPosR;//for right hand
    [SerializeField] private Transform holdPosL;//for left hand (not yet used)

    //range of raycast
    [SerializeField] private float interactableRange = 15f;

    //when grabbable object is held
    private GameObject heldObj;


    private void Awake()
    {
        layerMask = LayerMask.GetMask("Interactables");
        layerNumber = LayerMask.NameToLayer("Hold Layer");
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
            switch (hit.transform.gameObject.tag)
            {
                case "Grabbable":
                case "Clickable":
                case "Socket" when heldObj != null:
                    {
                        if (hit.transform.TryGetComponent(out InteractablesBase interactable))
                        {
                            if (this.interactable != interactable)
                            {
                                this.interactable?.OnHoverIn(false);
                            }

                            this.interactable = interactable;
                            interactable.OnHoverIn(true);
                        }
                        else
                        {
                            this.interactable?.OnHoverIn(false);
                            this.interactable = default;
                        }

                        break;
                    }
            }

            if (Input.GetKeyDown(KeyCode.Mouse0))
            {

                if (hit.transform.CompareTag("Grabbable"))
                {

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

                    if (socket.TryOccupySocket())
                    {
                        PutBackObject(hit.transform.gameObject);
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
            interactable?.OnHoverIn(false);
            interactable = default;
        }

        if (heldObj != null)
        {
            MoveObject();
        }

    }

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

    void MoveObject()
    {
        heldObj.transform.position = holdPosR.transform.position;
        heldObj.transform.rotation = Quaternion.identity; //for obj rotation, need to double check it, sets up the initial rotation of the object 
    }

    void PutBackObject(GameObject objPlaced)
    {
        if (heldObj == null)
            return;

        Physics.IgnoreCollision(heldObj.GetComponent<Collider>(),player.GetComponent<Collider>(),true);
        heldObj.layer = 0;
        heldObj.transform.DOMove(objPlaced.transform.position, 0.5f).SetEase(Ease.InOutSine);
        heldObj = null;
    }
}
