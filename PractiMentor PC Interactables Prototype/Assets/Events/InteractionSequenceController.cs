using System;
using UnityEngine;

public class InteractionSequenceController : MonoBehaviour
{
    public enum Step
    {
        FillPortafilter,
        TampCoffee,
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
        if (interactionEvent.Type != InteractionType.SliderCompleted)
            return;

        if (interactionEvent.PrimaryObject == null || interactionEvent.SecondaryObject == null)
            return;

        CoffeeTool primaryTool = interactionEvent.PrimaryObject.GetComponentInParent<CoffeeTool>();

        CoffeeTool secondaryTool = interactionEvent.SecondaryObject.GetComponentInParent<CoffeeTool>();

        if (primaryTool == null || secondaryTool == null)
            return;

        if(!IsValidForCurrentStep(primaryTool.ToolType, secondaryTool.ToolType))
        {
            Debug.Log("Interaction was not valid for the current step.");
            return;
        }

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
