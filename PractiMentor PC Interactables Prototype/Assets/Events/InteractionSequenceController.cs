using System;
using UnityEngine;

public class InteractionSequenceController : MonoBehaviour
{
    public enum Step
    {
        FillPortafilter,
        TampCoffee,
        SetPortafilter,
        SetMug,
        BrewCoffee,
        //DrinkCoffee,(to be added later)
        Complete
    }

    [Header("Sequence")]
    [SerializeField] private Step currentStep = Step.FillPortafilter;

    [Header("Interaction Source")]
    [SerializeField] private RaycasterObj raycaster;

    public Step CurrentStep => currentStep;
    public event Action<Step> StepChanged;
    public event Action SequenceCompleted;

    private void OnEnable()
    {
        if (raycaster != null)
            raycaster.InteractionCompleted += OnInteractionCompleted;
    }

    private void OnDisable()
    {
        if (raycaster != null)
            raycaster.InteractionCompleted -= OnInteractionCompleted;
    }

    private void OnInteractionCompleted(InteractionCompletedEvent interactionEvent)
    {
        switch (interactionEvent.Type)
        {
            case InteractionType.SliderCompleted:
                HandleSliderCompleted(interactionEvent);
                break;

            case InteractionType.ObjectPlaced:
                HandleObjectPlaced(interactionEvent);
                break;

            case InteractionType.SocketRotationCompleted:
                HandleSocketRotationCompleted(interactionEvent);
                break;
        }
    }

    private void HandleSliderCompleted(InteractionCompletedEvent interactionEvent)
    {
        if (interactionEvent.PrimaryObject == null || interactionEvent.SecondaryObject == null)
        {
            return;
        }

        CoffeeTool primaryTool = interactionEvent.PrimaryObject.GetComponentInParent<CoffeeTool>();

        CoffeeTool secondaryTool = interactionEvent.SecondaryObject.GetComponentInParent<CoffeeTool>();

        if (primaryTool == null || secondaryTool == null)
            return;

        if (!IsValidForCurrentStep(primaryTool.ToolType,secondaryTool.ToolType))
        {
            Debug.Log("Slider interaction was not valid for the current step.");
            return;
        }

        AdvanceStep();
    }

    private void HandleObjectPlaced(InteractionCompletedEvent interactionEvent)
    {
        if (currentStep != Step.SetPortafilter)
            return;

        if (interactionEvent.PrimaryObject == null || interactionEvent.SecondaryObject == null)
        {
            return;
        }

        CoffeeTool tool = interactionEvent.PrimaryObject.GetComponentInParent<CoffeeTool>();

        Socket socket = interactionEvent.SecondaryObject.GetComponentInParent<Socket>();

        if (tool == null || socket == null)
            return;

        if (tool.ToolType != CoffeeToolType.Portafilter)
            return;

        Debug.Log(
            "Portafilter placed. Waiting for rotation.");
    }

    private void HandleSocketRotationCompleted(InteractionCompletedEvent interactionEvent)
    {
        if (currentStep != Step.SetPortafilter)
            return;

        if (interactionEvent.PrimaryObject == null || interactionEvent.SecondaryObject == null)
        {
            return;
        }

        CoffeeTool tool = interactionEvent.PrimaryObject.GetComponentInParent<CoffeeTool>();

        Socket socket = interactionEvent.SecondaryObject.GetComponentInParent<Socket>();

        if (tool == null || socket == null)
            return;

        if (tool.ToolType != CoffeeToolType.Portafilter)
        {
            Debug.Log("Socket rotation completed by the wrong tool.");

            return;
        }

        Debug.Log("Portafilter rotation completed.");

        AdvanceStep();
    }

    private bool IsValidForCurrentStep(CoffeeToolType first, CoffeeToolType second)
    {
        return currentStep switch
        {
            Step.FillPortafilter => IsCorrectPair(first, second, CoffeeToolType.CoffeeBag, CoffeeToolType.Portafilter),
            Step.TampCoffee => IsCorrectPair(first, second, CoffeeToolType.CoffeeTamper, CoffeeToolType.Portafilter),
            Step.Complete => false,
            _ => false
        };
    }
    private bool IsCorrectPair(CoffeeToolType first, CoffeeToolType second, CoffeeToolType requiredA, CoffeeToolType requiredB)
    {
        return (first == requiredA && second == requiredB) || (first == requiredB && second == requiredA);
    }
    private void AdvanceStep()
    {
        switch (currentStep)
        {
            case Step.FillPortafilter:
                SetStep(Step.TampCoffee);
                break;
            case Step.TampCoffee:
                SetStep(Step.SetPortafilter);
                break;
            case Step.SetPortafilter:
                SetStep(Step.SetMug);
                break;
            case Step.SetMug:
                SetStep(Step.BrewCoffee); 
                break;
            case Step.BrewCoffee:
                SetStep(Step.Complete);
                SequenceCompleted?.Invoke();
                break;
        }
    }

    private void SetStep(Step nextStep)
    {
        currentStep = nextStep;
        StepChanged?.Invoke(currentStep);

        Debug.Log($"Sequence advanced to: {currentStep}");
    }

    public bool CanStartInteraction(GameObject primaryObject, GameObject secondaryObject)
    {
        if(currentStep == Step.Complete)
            return false;

        if(primaryObject == null || secondaryObject == null) 
            return false;

        CoffeeTool primaryTool = primaryObject.GetComponentInParent<CoffeeTool>();
        CoffeeTool secondaryTool = secondaryObject.GetComponentInParent<CoffeeTool>();

        if (primaryTool == null || secondaryTool == null)
            return false;

        return IsValidForCurrentStep(primaryTool.ToolType, secondaryTool.ToolType);
    }
}
