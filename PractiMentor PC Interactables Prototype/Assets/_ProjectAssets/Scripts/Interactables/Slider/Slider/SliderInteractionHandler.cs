using DG.Tweening;
using UnityEngine;
using System.Collections;

public class SliderInteractionHandler : MonoBehaviour
{
    [SerializeField] private SliderInteractionController sliderController;
    [SerializeField, Min(0f)] private float delayBetweenSliderActions = 0.5f;

    [SerializeField] private Player playerController;
    [SerializeField] private HandController handController;
    [SerializeField] private InteractionSequenceController sequenceController;
    [SerializeField] private GameObject drinkMug;

    [SerializeField] private SocketInteractionHandler socketHandler;

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

    private Camera playerCamera;

    private void Awake()
    {
        playerCamera = Camera.main;
    }

    public void Tick()
    {
        if (socketHandler != null &&
        socketHandler.IsHandlingRotation)
        {
            return;
        }

        UpdateInteractionSlider();
    }

    private void UpdateInteractionSlider()
    {
        //if (socketRotationMode)
       // {
            //UpdateSocketRotationSlider();
            //return;
        //}

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

        bool canUseSlider = isDrinkStep
            ? hasValidDrinkMug && CanInteract(handController.HeldRight)
            : sequenceController != null && handController.HeldRight != null &&
            handController.HeldLeft != null && CanInteract(handController.HeldRight) &&
            CanInteract(handController.HeldLeft) && rightSliderTarget != null && sequenceController.CanStartInteraction(handController.HeldRight, handController.HeldLeft);

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

        bool showSlider = !sliderCompleted && sliderReady && !movingToSliderHands;

        if (sliderController != null)
            sliderController.SetVisible(showSlider);

        if (!showSlider || sliderController == null)
            return;

        sliderController.TargetTransformReached -= OnSliderTargetReached;
        sliderController.TargetTransformReached += OnSliderTargetReached;

        sliderController.SetTarget(rightSliderTarget);

        if (rightSliderTarget.Representation == SliderTarget.SliderRepresentation._3D)
            sliderController.FollowWorldTarget(rightSliderTarget.SliderAnchor);

        sliderController.FollowObjectOnScreen(handController.SliderHoldRight, playerCamera, new Vector2(120f, 0f));

    }
    private void OnSliderTargetReached()
    {
        if (completingSliderAction)
            return;

        completingSliderAction = true;

        Grabbable grabbable = activeSliderGrabbable;

        if (grabbable != null && grabbable.IsInteractionTimed)
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
            Debug.LogError("Slider completed, but activeSliderGrabbable is null.", this);
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

        if (target == null)
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
            sliderController.FollowObjectOnScreen(target.transform, playerCamera, new Vector2(120f, 0f));
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
               // if (socketRotationMode)
                   // CompleteSocketRotation();
              //  else
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
    private bool CanInteract(GameObject obj)
    {
        Grabbable grabbable = obj.GetComponentInParent<Grabbable>();
        return grabbable != null && grabbable.HasInteractableAction;
    }
    private Tween MoveManipulatedObjectTo(HandController.Hand hand, Transform anchor)
    {
        GameObject heldObject = handController.GetHeldObject(hand);
        Transform manipulatedObject = GetManipulatedTransform(hand);

        if (heldObject == null || manipulatedObject == null || anchor == null)
        {
            return null;
        }

        Vector3 childOffset = manipulatedObject.position - heldObject.transform.position;

        Vector3 targetPosition = anchor.position + childOffset;

        return manipulatedObject.DOMove(targetPosition, 0.5f).SetEase(Ease.InOutCirc);
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
}
