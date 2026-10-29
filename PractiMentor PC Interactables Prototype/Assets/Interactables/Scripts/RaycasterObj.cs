using DG.Tweening;
using UnityEngine;
using System.Collections;
using System;

//this handles too many things (raycast, hovering, picking objs, movement, socket stuff), needs to be broken down 
public class RaycasterObj : MonoBehaviour
{
    public event Action<InteractionCompletedEvent> InteractionCompleted;

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
    [SerializeField] private BezierSliderInteraction sliderController;
    [SerializeField, Min(0f)] private float delayBetweenSliderActions = 0.3f;

    [Header("Workflow")]
    [SerializeField] private InteractionSequenceController sequenceController;

    private bool sliderCompleted;

    private bool movingToSliderHands;

    private bool restoringSliderTarget;

    private bool sliderReady;

    private Coroutine timedSliderRoutine;
    private bool holdingTimedSliderTarget;

    private int completedSliderActions;
    private Grabbable activeSliderGrabbable;
    private bool restoringBetweenSliderActions;

    private bool completingSliderAction;

    private Socket activeRotationSocket;
    private GameObject activeSocketObject;
    private bool socketRotationMode;

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

        if(sliderController != null)
            sliderController.SetVisible(false);
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
            bool hitIsSocket = hit.transform.CompareTag("Socket");

            Socket hitSocket = hitIsSocket ? hit.transform.GetComponentInParent<Socket>() : null;

            bool socketCanAcceptHeldItem = false;

            if (hitSocket != null && TryFindHeldHand(out Hand heldHand))
            {
                GameObject heldObject = GetHeldObject(heldHand);
                GameObject grabbableRoot = GetGrabbableRoot(heldObject);

                socketCanAcceptHeldItem = hitSocket.CanAccept(grabbableRoot);
            }

            //highlighting logic
            bool canHighlightHit = hit.transform.CompareTag("Grabbable") || hit.transform.CompareTag("Clickable") || (hitIsSocket && socketCanAcceptHeldItem);

            if (canHighlightHit)
            {
                if(hit.transform.TryGetComponent(out InteractablesBase nextInteractable))
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
                else if (hitIsSocket && socketCanAcceptHeldItem)
                {
                    Socket socket = hitSocket;

                    //checks socket state 
                    if (socket == null)
                    {
                        Debug.LogWarning("The socket object has no Socket component.");
                        return;
                    }

                    if (TryFindHeldHand(out Hand selectedHand))
                    {
                        GameObject heldObject = GetHeldObject(selectedHand);
                        GameObject grabbableRoot = GetGrabbableRoot(heldObject);

                        if (socket.TryOccupySocket(heldObject, out Transform placementPoint))
                        {
                            PlaceHeldObject(socket, placementPoint, selectedHand);
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

    //function to pick up object
    void PickUpObject(GameObject pickUpObj, Hand hand)
    {
        if(playerController != null && playerController.CurrentMode == Player.PlayerMode.SliderInteraction)
            return;

        if (pickUpObj == null || GetHeldObject(hand) != null)
            return;

        GameObject grabbableRoot = GetGrabbableRoot(pickUpObj);

        if (!CanUseHand(grabbableRoot, hand)) 
            return;

        SetHeldObject(hand, grabbableRoot);

        Transform holdPosition = GetHoldPosition(hand);
        grabbableRoot.transform.DOMove(endValue: holdPosition.position, 0.5f).SetEase(Ease.InOutSine);
        grabbableRoot.layer = layerNumber;

        Collider objectCollider = grabbableRoot.GetComponent<Collider>();
        Collider playerCollider = player.GetComponent<Collider>();

        if (objectCollider != null && playerCollider != null)
            Physics.IgnoreCollision(objectCollider, playerCollider,true);

        
    }

    //fuction to move held grabbable
    void MoveObject(Hand hand)
    {
        if (movingToSliderHands || restoringSliderTarget || restoringBetweenSliderActions)
            return;

        if (playerController != null && playerController.CurrentMode == Player.PlayerMode.SliderInteraction)
            return;

        GameObject heldObject = GetHeldObject(hand);

        if (heldObject == null)
            return;

        heldObject.transform.position = GetHoldPosition(hand).position;
    }

    //function to place held grabbable (with the animation, assisted by Socket.cs (will be up for modification)
    void PlaceHeldObject(Socket socket, Transform placementPoint, Hand hand)
    {
        GameObject heldObject = GetHeldObject(hand);

        if (heldObject == null)
            return;

        Collider objectCollider = heldObject.GetComponent<Collider>();
        Collider playerCollider = player.GetComponent<Collider>();

        if (objectCollider != null && playerCollider != null)
            Physics.IgnoreCollision(objectCollider, playerCollider, false);

        heldObject.layer = 0;

        Sequence placementSequence = DOTween.Sequence();

        placementSequence.Join(heldObject.transform.DOMove(placementPoint.position, 0.5f).SetEase(Ease.InOutSine));

        placementSequence.Join(heldObject.transform.DORotateQuaternion(placementPoint.rotation, 0.5f).SetEase(Ease.InOutSine));

        SetHeldObject(hand, null);

        placementSequence.OnComplete(() =>
        {
            InteractionCompleted?.Invoke(new InteractionCompletedEvent(InteractionType.ObjectPlaced,heldObject,socket.gameObject));

            BeginSocketRotation(socket, heldObject);
        });
    }

    private void BeginSocketRotation(Socket socket, GameObject placedObject)
    {
        activeRotationSocket = socket;
        activeSocketObject = placedObject;
        socketRotationMode = true;

        SliderTarget sliderTarget = placedObject.GetComponentInChildren<SliderTarget>();

        if(sliderTarget == null)
        {
            Debug.LogWarning("Placed portafilter has no SliderTarget!");
            CancelSocketRotation();
            return;
        }

        if (sliderController == null)
        {
            Debug.LogError("SliderController is missing.");
            CancelSocketRotation();
            return;
        }

        activeRotationSocket = socket;
        activeSocketObject = placedObject;
        socketRotationMode = true;

        if (playerController != null)
        {
            playerController.SetPlayerState(Player.PlayerMode.SliderInteraction);
        }

        sliderController.SetTarget(sliderTarget);
        sliderController.SetVisible(true);
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
        if (socketRotationMode)
        {
            UpdateSocketRotationSlider();
            return;
        }

        if (restoringBetweenSliderActions)
        {
            return;
        }

        SliderTarget rightSliderTarget = heldObjR?.GetComponentInParent<SliderTarget>();

        bool canUseSlider = heldObjR != null && heldObjL != null 
            && CanInteract(heldObjR) && CanInteract(heldObjL) 
            && rightSliderTarget != null && sequenceController != null 
            && sequenceController.CanStartInteraction(heldObjR, heldObjL);

        if (!canUseSlider)
        {
            sliderCompleted = false;
            sliderReady = false;

            if (sliderController != null)
            {
                sliderController.TargetTransformReached -= OnSliderTargetReached;
                sliderController.ClearTarget();
                sliderController.SetVisible(false);
            }

            if (playerController != null)
                playerController.SetPlayerState(Player.PlayerMode.Navigate);

            return;
        }

        bool enteringSliderMode = !sliderCompleted && !movingToSliderHands && playerController != null && playerController.CurrentMode != Player.PlayerMode.SliderInteraction;

        if (enteringSliderMode)
        {
            Grabbable grabbable = heldObjR.GetComponentInParent<Grabbable>();

            activeSliderGrabbable = grabbable;
            completedSliderActions = 0;
            sliderCompleted = false;
            sliderReady = false;

            playerController.SetPlayerState(Player.PlayerMode.SliderInteraction);

            MoveHeldObjectsToSliderHands(rightSliderTarget);
        }

        bool showSlider =!sliderCompleted && sliderReady && !movingToSliderHands;

        if (sliderController != null)
            sliderController.SetVisible(showSlider);

        if (!showSlider || sliderController == null)
            return;

        sliderController.TargetTransformReached -= OnSliderTargetReached;
        sliderController.TargetTransformReached += OnSliderTargetReached;

        sliderController.SetTarget(rightSliderTarget);

        sliderController.FollowObjectOnScreen(sliderHoldPosR,playerCamera,new Vector2(120f, 0f));

    }

    private void UpdateSocketRotationSlider()
    {
        if(activeSocketObject == null || activeRotationSocket == null || sliderController == null)
        {
            CancelSocketRotation();
            return;
        }

        SliderTarget target = activeSocketObject.GetComponentInChildren<SliderTarget>();

        if(target == null)
        {
            Debug.LogWarning("Socket rotation object has no SliderTarget.");

            CancelSocketRotation();
            return;
        }

        if(playerController != null && playerController.CurrentMode != Player.PlayerMode.SliderInteraction)
        {
            playerController.SetPlayerState(Player.PlayerMode.SliderInteraction);
        }

        sliderController.TargetTransformReached -= OnSliderTargetReached;
        sliderController.TargetTransformReached += OnSliderTargetReached;

        sliderController.SetVisible(true);

        sliderController.FollowObjectOnScreen(target.transform, playerCamera,new Vector2(120f, 0f));
    }
    private void OnSliderTargetReached()
    {
        if (socketRotationMode)
        {
            CompleteSocketRotation();
            return;
        }

        if (completingSliderAction)
            return;

        completingSliderAction = true;

        Grabbable grabbable = heldObjR?.GetComponentInParent<Grabbable>();

        if(grabbable != null && grabbable.IsInteractionTimed)
        {
            holdingTimedSliderTarget = true;
            timedSliderRoutine = StartCoroutine(HoldSliderTarget(grabbable.TimeAmount));
            return;
        }

        CompleteSliderAction();
    }

    private void CompleteSocketRotation()
    {
        if(activeSocketObject == null || activeRotationSocket == null)
        {
            CancelSocketRotation();
            return;
        }

        Transform finalPose = activeRotationSocket.FinalRotationPoint;

        if(finalPose != null)
        {
            activeSocketObject.transform.SetPositionAndRotation(finalPose.position, finalPose.rotation);
        }

        sliderController.TargetTransformReached -= OnSliderTargetReached;

        sliderController.ClearTarget();
        sliderController.SetVisible(false);

        InteractionCompleted?.Invoke(new InteractionCompletedEvent(InteractionType.SocketRotationCompleted,activeSocketObject,activeRotationSocket.gameObject));

        socketRotationMode = false;
        activeSocketObject = null;
        activeRotationSocket = null;

        if (playerController != null)
        {
            playerController.SetPlayerState(Player.PlayerMode.Navigate);
        }
    }
    private void CompleteSliderAction()
    {
        Grabbable grabbable = activeSliderGrabbable;

        if (grabbable == null)
        {
            FinishSliderInteraction();
            return;
        }

        completedSliderActions++;

        bool needsAnotherAction = grabbable.CanActionMultiple && completedSliderActions < grabbable.ActionCount;

        if (needsAnotherAction)
        {
            StartCoroutine(WaitForPointerReleaseThenRestore());
        }
        else
        {
            InteractionCompleted?.Invoke(new InteractionCompletedEvent(InteractionType.SliderCompleted,heldObjR,heldObjL));
            FinishSliderInteraction();
        }
    }

    private void CancelSocketRotation()
    {
        if (sliderController != null)
        {
            sliderController.TargetTransformReached -= OnSliderTargetReached;

            sliderController.ClearTarget();
            sliderController.SetVisible(false);
        }

        socketRotationMode = false;
        activeSocketObject = null;
        activeRotationSocket = null;

        if (playerController != null)
        {
            playerController.SetPlayerState(Player.PlayerMode.Navigate);
        }
    }
    private IEnumerator WaitForPointerReleaseThenRestore()
    {
        while (sliderController != null && sliderController.IsPointerHeld)
            yield return null;

        float delay = activeSliderGrabbable != null
        ? activeSliderGrabbable.DelayBetweenSliderAction
        : 0f;

        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        RestoreAndPrepareNextAction();
    }
    private void RestoreAndPrepareNextAction()
    {
        if (sliderController == null)
        {
            FinishSliderInteraction();
            return;
        }

        restoringBetweenSliderActions = true;
        sliderReady = false;

        Tween restoreTween = sliderController.RestoreStartingTransform();

        sliderController.ClearTarget();
        sliderController.ResetTimedProgress();
        sliderController.SetVisible(false);

        if (restoreTween != null)
        {
            restoreTween.OnComplete(() =>
            {
                restoringBetweenSliderActions = false;
                PrepareNextSliderAction();
            });
        }
        else
        {
            restoringBetweenSliderActions = false;
            PrepareNextSliderAction();
        }
    }

    private void PrepareNextSliderAction()
    {
        if (heldObjR == null)
            return;

        SliderTarget target = heldObjR.GetComponentInParent<SliderTarget>();

        if(target == null)
        {
            FinishSliderInteraction();
            return;
        }

        sliderController.RearmTarget(target);

        sliderCompleted = false;
        sliderReady = true;
        completingSliderAction = false;

        sliderController.SetVisible(true);
    }
    private void FinishSliderInteraction()
    {
        completingSliderAction = false;

        if (timedSliderRoutine != null)
        {
            StopCoroutine(timedSliderRoutine);
            timedSliderRoutine = null;
        }

        holdingTimedSliderTarget = false;
        sliderCompleted = true;

        if (sliderController != null)
        {
            //sliderController.SetAutomaticResetEnabled(true);
            Tween restoreTween = sliderController.RestoreStartingTransform();

            sliderController.ClearTarget();
            sliderController.ResetTimedProgress();
            sliderController.SetVisible(false);

            if (restoreTween != null)
            {
                restoringSliderTarget = true;

                restoreTween.OnComplete(() =>
                {
                    MoveHeldObjectsBackToNormalHands();

                    if (playerController != null)
                        playerController.SetPlayerState(Player.PlayerMode.Navigate);
                });

                return;
            }
        }

        MoveHeldObjectsBackToNormalHands();

        if (playerController != null)
            playerController.SetPlayerState(Player.PlayerMode.Navigate);
    }

    private void MoveHeldObjectsBackToNormalHands()
    {
        DG.Tweening.Sequence sequence = DOTween.Sequence();

        Transform rightObject = GetManipulatedTransform(Hand.Right);
        Transform leftObject = GetManipulatedTransform(Hand.Left);

        if (holdPosR != null)
            sequence.Join(MoveManipulatedObjectTo(Hand.Right, holdPosR));

        if (holdPosL != null)
            sequence.Join(MoveManipulatedObjectTo(Hand.Left, holdPosL));

        sequence.OnComplete(() =>
        {
            restoringSliderTarget = false;
        });
    }

    private bool CanInteract (GameObject obj)
    {
        Grabbable grabbable = obj.GetComponentInParent<Grabbable>();
        return grabbable != null && grabbable.CanThisInteract;
    }


    private bool IsSliderKnobStillHeld()
    {
        return heldObjR!= null && heldObjR.GetComponentInParent<SliderTarget>() != null;
    }
    private void MoveHeldObjectsToSliderHands(SliderTarget sliderTarget)
    {
        movingToSliderHands = true;
        sliderReady = false;

        DG.Tweening.Sequence sequence = DG.Tweening.DOTween.Sequence();

        Transform rightObject = GetManipulatedTransform(Hand.Right);
        Transform leftObject = GetManipulatedTransform(Hand.Left);

        rightObject?.DOKill();
        leftObject?.DOKill();

        if (rightObject != null && sliderHoldPosR != null)
        {
            sequence.Join(MoveManipulatedObjectTo(Hand.Right, sliderHoldPosR));
        }

        if (leftObject != null && sliderHoldPosL != null)
        {
            sequence.Join(MoveManipulatedObjectTo(Hand.Left, sliderHoldPosL));
        }

        sequence.OnComplete(() =>
        {
            movingToSliderHands = false;

            if (sliderController != null && sliderTarget != null)
                sliderController.SetTarget(sliderTarget);

            sliderReady = true;
        });
    }

    private Transform GetManipulatedTransform(Hand hand)
    {
        GameObject heldObject = GetHeldObject(hand);

        if (heldObject == null)
            return null;

        if (hand == Hand.Right)
        {
            SliderTarget sliderTarget =
                heldObject.GetComponentInParent<SliderTarget>();

            if (sliderTarget != null)
                return sliderTarget.transform;
        }

        return heldObject.transform;
    }

    private Tween MoveManipulatedObjectTo(Hand hand, Transform anchor)
    {
        GameObject heldObject = GetHeldObject(hand);
        Transform manipulatedObject = GetManipulatedTransform(hand);

        if (heldObject == null ||manipulatedObject == null ||anchor == null)
        {
            return null;
        }

        Vector3 childOffset = manipulatedObject.position - heldObject.transform.position;

        Vector3 targetPosition = anchor.position + childOffset;

        return manipulatedObject.DOMove(targetPosition, 0.5f).SetEase(Ease.InOutCirc);
    }

    private GameObject GetGrabbableRoot(GameObject hitObject)
    {
        Grabbable grabbable =
            hitObject.GetComponentInParent<Grabbable>();

        return grabbable != null? grabbable.gameObject: hitObject;
    }

    private IEnumerator HoldSliderTarget(float duration)
    {
        float elapsed = 0f;

        while (true)
        {
            if (!IsSliderKnobStillHeld() || sliderController == null)
            {
                sliderController?.ResetTimedProgress();
                FinishSliderInteraction();
                yield break;
            }

            if (!sliderController.IsPointerHeld || !sliderController.IsTargetReached)
            {
                elapsed = 0f;
                sliderController.ResetTimedProgress();

                yield return null;
                continue;
            }

            elapsed += Time.deltaTime;

            float progress = Mathf.Clamp01(elapsed / duration);
            sliderController.SetTimedProgress(progress);

            if (elapsed >= duration)
            {
                CompleteSliderAction();
                yield break;
            }

            yield return null;
        }
    }
}
