using DG.Tweening;
using UnityEngine;
using System.Collections;
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

    //slider reference
    [Header("Slider Set Up")]
    [SerializeField] private SliderInteractionController sliderController;
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

    private Socket mugSocket;

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

        if(sliderController != null)
            sliderController.SetVisible(false);
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
                Debug.Log(
        $"Clicked object: {hit.transform.name}",
        this);

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

    //function to place held grabbable (with the animation, assisted by Socket.cs (will be up for modification)
    void PlaceHeldObject(Socket socket, Transform placementPoint, HandController.Hand hand)
    {
        GameObject heldObject = handController.GetHeldObject(hand);

        if (heldObject == null)
            return;

        handController.TryPlaceHeldObject(hand, placementPoint, placedObject =>
        {
            handController.ReportInteractionCompleted(new InteractionCompletedEvent(InteractionType.ObjectPlaced, placedObject, socket.gameObject));

            CoffeeTool tool = placedObject.GetComponentInParent<CoffeeTool>();

            if (tool != null && tool.ToolType == CoffeeToolType.Mug)
            {
                mugSocket = socket;
            }

            if (tool != null && tool.ToolType == CoffeeToolType.Portafilter)
            {
                BeginSocketRotation(socket, placedObject);
            }
        });
    }

    private void BeginSocketRotation(Socket socket, GameObject placedObject)
    {
        CoffeeTool tool = placedObject.GetComponentInParent<CoffeeTool>();

        if (tool == null || tool.ToolType != CoffeeToolType.Portafilter)
        {
            Debug.LogWarning("Socket rotation requested for a non-portafilter.");
            return;
        }

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

        activeSliderGrabbable = sliderTarget.GetComponentInParent<Grabbable>();

        if (activeSliderGrabbable == null)
        {
            activeSliderGrabbable = placedObject.GetComponentInParent<Grabbable>();
        }

        if (activeSliderGrabbable == null)
        {
            activeSliderGrabbable = placedObject.GetComponentInChildren<Grabbable>();
        }

        if (sliderController == null)
        {
            Debug.LogError("SliderController is missing.");
            CancelSocketRotation();
            return;
        }

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

        SliderTarget rightSliderTarget = GetSliderTarget(handController.HeldRight);

        bool isDrinkStep = sequenceController != null && sequenceController.CurrentStep == InteractionSequenceController.Step.DrinkCoffee;

        bool hasValidDrinkMug = handController.HeldRight != null && handController.HeldRight.GetComponentInParent<CoffeeTool>()?.ToolType == CoffeeToolType.Mug && GetSliderTarget(handController.HeldRight) != null;

        bool rightHeld = handController.HeldRight != null;

        bool leftHeld = handController.HeldLeft != null;

        bool rightCanInteract = rightHeld && CanInteract(handController.HeldRight);

        bool leftCanInteract = leftHeld && CanInteract(handController.HeldLeft);

        bool validSequencePair = sequenceController != null && rightHeld && leftHeld && sequenceController.CanStartInteraction(handController.HeldRight, handController.HeldLeft);

        Debug.Log(
            $"Interaction gate | " +
            $"RightHeld={rightHeld}, " +
            $"LeftHeld={leftHeld}, " +
            $"RightAction={rightCanInteract}, " +
            $"LeftAction={leftCanInteract}, " +
            $"RightSliderTarget={rightSliderTarget != null}, " +
            $"ValidPair={validSequencePair}",
            this);

        bool canUseSlider = isDrinkStep
            ? hasValidDrinkMug && CanInteract(handController.HeldRight)
            : sequenceController != null && handController.HeldRight != null &&
            handController.HeldLeft != null && CanInteract(handController.HeldRight) &&
            CanInteract(handController.HeldLeft) && rightSliderTarget != null && sequenceController.CanStartInteraction(handController.HeldRight,handController.HeldLeft);

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
            activeSliderGrabbable = GetGrabbableForSliderTarget(rightSliderTarget);

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

        if(rightSliderTarget.Representation == SliderTarget.SliderRepresentation._3D)
            sliderController.FollowWorldTarget(rightSliderTarget.SliderAnchor);

        sliderController.FollowObjectOnScreen(handController.SliderHoldRight, playerCamera,new Vector2(120f, 0f));

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


        if (target.Representation == SliderTarget.SliderRepresentation._3D)
        {
            sliderController.FollowWorldTarget(target.SliderAnchor);
        }
        else
        {
            sliderController.FollowObjectOnScreen(target.transform,playerCamera,new Vector2(120f, 0f));
        }
    }
    private void OnSliderTargetReached()
    {
        if (completingSliderAction)
            return;

        completingSliderAction = true;

        Grabbable grabbable = activeSliderGrabbable;

        if(grabbable != null && grabbable.IsInteractionTimed)
        {
            completingSliderAction = true;
            holdingTimedSliderTarget = true;
            timedSliderRoutine = StartCoroutine(HoldSliderTarget(grabbable.TimeAmount));
            return;
        }

        CompleteSliderAction();
       // if (socketRotationMode)
       // {
            //CompleteSocketRotation();
           // return;
       // }
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

        handController.ReportInteractionCompleted(new InteractionCompletedEvent(InteractionType.SocketRotationCompleted, activeSocketObject, activeRotationSocket.gameObject));

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

        Debug.Log(
        $"CompleteSliderAction: grabbable={grabbable}, " +
        $"multiple={grabbable?.HasInteractableMoreAction}, " +
        $"completed={completedSliderActions}, " +
        $"count={grabbable?.ActionCount}, " +
        $"pointerHeld={sliderController?.IsPointerHeld}"
    );

        if (grabbable == null)
        {
            Debug.LogError("Slider completed, but activeSliderGrabbable is null.",this);
            FinishSliderInteraction();
            return;
        }

        completedSliderActions++;

        bool needsAnotherAction = grabbable.HasInteractableMoreAction && completedSliderActions < grabbable.ActionCount;

        Debug.Log($"Needs another action: {needsAnotherAction}");

        if (needsAnotherAction)
        {
            StartCoroutine(WaitForPointerReleaseThenRestore());
        }
        else
        {
            handController.ReportInteractionCompleted(new InteractionCompletedEvent(InteractionType.SliderCompleted, handController.HeldRight, handController.HeldLeft));
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
        Debug.Log("Waiting for 3D slider pointer release.");

        while (sliderController != null && sliderController.IsPointerHeld)
            yield return null;

        Debug.Log("Pointer released; restoring slider.");

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

        if (handController.HeldRight == null)
        {
            FinishSliderInteraction();
            return;
        }

        SliderTarget target = GetSliderTarget(handController.HeldRight);

        if(target == null)
        {
            FinishSliderInteraction();
            return;
        }

        restoringBetweenSliderActions = false;
        sliderCompleted = false;
        sliderReady = true;
        completingSliderAction = false;

        sliderController.SetAutomaticResetEnabled(false);
        sliderController.RearmTarget(target);

        if (target.Representation == SliderTarget.SliderRepresentation._3D)
        {
            sliderController.FollowWorldTarget(target.SliderAnchor);
        }
        else
        {
            sliderController.FollowObjectOnScreen(target.transform, playerCamera,new Vector2(120f, 0f));
        }

        sliderController.SetVisible(true);
    }
    private void FinishSliderInteraction()
    {
        completingSliderAction = false;

        if (sliderController != null)
            sliderController.SetAutomaticResetEnabled(true);

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

        GameObject rightObject = handController.GetHeldObject(HandController.Hand.Right);
        GameObject leftObject = handController.GetHeldObject(HandController.Hand.Left);

        if (rightObject != null && handController.NormalHoldRight != null)
            //sequence.Join(MoveManipulatedObjectTo(Hand.Right, holdPosR));
            sequence.Join(rightObject.transform.DOMove(handController.NormalHoldRight.position, 0.5f).SetEase(Ease.InOutCirc));

        if (leftObject != null && handController.NormalHoldLeft != null)
            //sequence.Join(MoveManipulatedObjectTo(Hand.Left, holdPosL));
            sequence.Join(leftObject.transform.DOMove(handController.NormalHoldLeft.position, 0.5f).SetEase(Ease.InOutCirc));

        sequence.OnComplete(() =>
        {
            restoringSliderTarget = false;
        });
    }

    private bool CanInteract (GameObject obj)
    {
        Grabbable grabbable = obj.GetComponentInParent<Grabbable>();
        return grabbable != null && grabbable.HasInteractableAction;
    }


    private bool IsSliderKnobStillHeld()
    {
        return handController.HeldRight != null && GetSliderTarget(handController.HeldRight) != null;
    }
    private void MoveHeldObjectsToSliderHands(SliderTarget sliderTarget)
    {
        movingToSliderHands = true;
        sliderReady = false;

        DG.Tweening.Sequence sequence = DG.Tweening.DOTween.Sequence();

        Transform rightObject = GetManipulatedTransform(HandController.Hand.Right);
        Transform leftObject = GetManipulatedTransform(HandController.Hand.Left);

        rightObject?.DOKill();
        leftObject?.DOKill();

        if (rightObject != null && handController.SliderHoldRight != null)
        {
            sequence.Join(MoveManipulatedObjectTo(HandController.Hand.Right, handController.SliderHoldRight));
        }

        if (leftObject != null && handController.SliderHoldLeft != null)
        {
            sequence.Join(MoveManipulatedObjectTo(HandController.Hand.Left, handController.SliderHoldLeft));
        }

        sequence.OnComplete(() =>
        {
            movingToSliderHands = false;

            if (sliderController != null && sliderTarget != null)
                sliderController.SetTarget(sliderTarget);

            sliderReady = true;
        });
    }

    private Transform GetManipulatedTransform(HandController.Hand hand)
    {
        GameObject heldObject = handController.GetHeldObject(hand);

        if (heldObject == null)
            return null;

        bool isDrinkMug = hand == HandController.Hand.Right && heldObject == drinkMug && heldObject.GetComponent<CoffeeTool>()?.ToolType == CoffeeToolType.Mug;

        if (isDrinkMug)
            return heldObject.transform;

        if (hand == HandController.Hand.Right)
        {
            SliderTarget sliderTarget = GetSliderTarget(handController.HeldRight);

            if (sliderTarget != null)
                return sliderTarget.transform;
        }

        return heldObject.transform;
    }

    private Tween MoveManipulatedObjectTo(HandController.Hand hand, Transform anchor)
    {
        GameObject heldObject = handController.GetHeldObject(hand);
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

    private void PickUpMugForDrinking(GameObject mug)
    {
        if (mugSocket == null || mug == null)
        {
            Debug.LogWarning("Can't pick up the drinking mug, socket or mug is missing.");
            return;
        }

        if(handController.HeldRight != null)
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
    private IEnumerator HoldSliderTarget(float duration)
    {
        float elapsed = 0f;

        while (true)
        {
           // if (!IsSliderKnobStillHeld() || sliderController == null)
           // {
               // sliderController?.ResetTimedProgress();
               // FinishSliderInteraction();
               // yield break;
           // }

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
                if (socketRotationMode)
                    CompleteSocketRotation();
                else
                    CompleteSliderAction();

                yield break;
            }

            yield return null;
        }
    }
    private Grabbable GetGrabbableForSliderTarget(SliderTarget sliderTarget)
    {
        if (sliderTarget == null)
            return null;

        Grabbable grabbable = sliderTarget.GetComponentInParent<Grabbable>();

        if (grabbable != null)
            return grabbable;

        return handController.HeldRight?.GetComponentInParent<Grabbable>();
    }
    private SliderTarget GetSliderTarget(GameObject obj)
    {
        if (obj == null)
            return null;

        SliderTarget target = obj.GetComponent<SliderTarget>();

        if (target == null)
            target = obj.GetComponentInChildren<SliderTarget>();

        if (target == null)
            target = obj.GetComponentInParent<SliderTarget>();

        return target;
    }
}
