using UnityEngine;
using DG.Tweening;
using System.Collections;

public class SocketInteractionHandler : MonoBehaviour
{
    [SerializeField] private HandController handController;
    [SerializeField] private Player playerController;
    [SerializeField] private SliderInteractionController sliderController;
    public Socket MugSocket { get; private set; }

    private Socket activeRotationSocket;
    private GameObject activeSocketObject;
    private bool socketRotationMode;

    private Camera playerCamera;

    private int completedSocketActions;
    private Grabbable activeSocketGrabbable;
    private bool completingSocketAction;
    private bool restoringBetweenSocketActions;

    public bool IsHandlingRotation => socketRotationMode;

    private void Awake()
    {
        playerCamera = Camera.main;
    }

    public void Tick()
    {
        if (!socketRotationMode)
            return;

        UpdateSocketRotationSlider();
    }
    private void OnEnable()
    {
        if (sliderController != null)
            sliderController.TargetTransformReached += OnSliderTargetReached;
    }

    private void OnDisable()
    {
        if (sliderController != null)
            sliderController.TargetTransformReached -= OnSliderTargetReached;
    }

    public bool TryPlaceHeldObject(Socket socket, Transform placementPoint, HandController.Hand hand)
    {
        if (socket == null || placementPoint == null)
            return false;

        GameObject heldObject = handController.GetHeldObject(hand);

        if (heldObject == null)
            return false;

        return handController.TryPlaceHeldObject(hand, placementPoint, placedObject =>
        {
            handController.ReportInteractionCompleted(new InteractionCompletedEvent(InteractionType.ObjectPlaced, placedObject, socket.gameObject));
            CoffeeTool tool = placedObject.GetComponentInParent<CoffeeTool>();

            if (tool != null && tool.ToolType == CoffeeToolType.Portafilter)
            {
                Grabbable grabbable = placedObject.GetComponentInParent<Grabbable>() ?? placedObject.GetComponentInChildren<Grabbable>();

                if (grabbable != null && grabbable.HasInteractableAction)
                {
                    BeginSocketRotation(socket, placedObject);
                }
            }

            if (tool != null && tool.ToolType == CoffeeToolType.Mug)
            {
                MugSocket = socket;
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

        Grabbable grabbable = placedObject.GetComponentInParent<Grabbable>() ?? placedObject.GetComponentInChildren<Grabbable>();

        if (grabbable == null || !grabbable.HasInteractableAction)
        {
            Debug.Log("Portafilter has no interactable action.");
            return;
        }

        activeSocketGrabbable = grabbable;

        activeRotationSocket = socket;
        activeSocketObject = placedObject;
        socketRotationMode = true;

        SliderTarget sliderTarget = placedObject.GetComponentInChildren<SliderTarget>();

        if (sliderTarget == null)
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

        if (playerController != null)
        {
            playerController.SetPlayerState(Player.PlayerMode.SliderInteraction);
        }

        sliderController.SetTarget(sliderTarget);
        sliderController.SetVisible(true);

        completedSocketActions = 0;
        completingSocketAction = false;
        restoringBetweenSocketActions = false;
    }
    private void UpdateSocketRotationSlider()
    {
        if (activeSocketObject == null || activeRotationSocket == null || sliderController == null)
        {
            CancelSocketRotation();
            return;
        }

        SliderTarget target = activeSocketObject.GetComponentInChildren<SliderTarget>();

        if (target == null)
        {
            Debug.LogWarning("Socket rotation object has no SliderTarget.");

            CancelSocketRotation();
            return;
        }

        if (playerController != null && playerController.CurrentMode != Player.PlayerMode.SliderInteraction)
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
            sliderController.FollowObjectOnScreen(target.transform, playerCamera, new Vector2(120f, 0f));
        }
    }
    private void CompleteSocketRotation()
    {
        if (activeSocketObject == null || activeRotationSocket == null)
        {
            CancelSocketRotation();
            return;
        }

        Transform finalPose = activeRotationSocket.FinalRotationPoint;

        if (finalPose != null)
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
    private void OnSliderTargetReached()
    {
        if (!socketRotationMode || completingSocketAction || restoringBetweenSocketActions)
            return;

        completingSocketAction = true;

        Grabbable grabbable = GetActiveGrabbable();

        if (grabbable != null && grabbable.IsInteractionTimed)
        {
            StartCoroutine(HoldSocketRotation(grabbable.TimeAmount));
            return;
        }

        CompleteSocketAction();
    }
    private IEnumerator HoldSocketRotation(float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (sliderController == null || !sliderController.IsPointerHeld || !sliderController.IsTargetReached)
            {
                sliderController?.ResetTimedProgress();
                completingSocketAction = false;
                yield break;
            }

            elapsed += Time.deltaTime;

            sliderController.SetTimedProgress(Mathf.Clamp01(elapsed / duration));

            yield return null;
        }

        sliderController?.ResetTimedProgress();
        CompleteSocketAction();
    }
    private Grabbable GetActiveGrabbable()
    {
        if (activeSocketObject == null)
            return null;

        Grabbable grabbable =
            activeSocketObject.GetComponentInParent<Grabbable>();

        if (grabbable == null)
            grabbable =
                activeSocketObject.GetComponentInChildren<Grabbable>();

        return grabbable;
    }
    private void CompleteSocketAction()
    {
        if (activeSocketGrabbable == null)
        {
            CompleteSocketRotation();
            return;
        }

        completedSocketActions++;

        bool needsAnotherAction = activeSocketGrabbable.HasInteractableMoreAction && completedSocketActions < activeSocketGrabbable.ActionCount;

        if (needsAnotherAction)
        {
            StartCoroutine(PrepareNextSocketAction());
        }
        else
        {
            CompleteSocketRotation();
        }
    }
    private IEnumerator PrepareNextSocketAction()
    {
        while (sliderController != null && sliderController.IsPointerHeld)
        {
            yield return null;
        }

        float delay = activeSocketGrabbable != null
            ? activeSocketGrabbable.DelayBetweenSliderAction
            : 0f;

        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        restoringBetweenSocketActions = true;

        Tween restoreTween = sliderController.RestoreStartingTransform();

        sliderController.ClearTarget();
        sliderController.SetVisible(false);

        if (restoreTween != null)
            yield return restoreTween.WaitForCompletion();

        SliderTarget target = activeSocketObject.GetComponentInChildren<SliderTarget>();

        if (target == null)
        {
            CompleteSocketRotation();
            yield break;
        }

        sliderController.RearmTarget(target);
        sliderController.SetAutomaticResetEnabled(false);

        if (target.Representation == SliderTarget.SliderRepresentation._3D)
            sliderController.FollowWorldTarget(target.SliderAnchor);

        sliderController.SetVisible(true);

        restoringBetweenSocketActions = false;
        completingSocketAction = false;
    }
}
