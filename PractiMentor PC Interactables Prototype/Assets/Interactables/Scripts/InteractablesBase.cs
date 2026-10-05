using DG.Tweening;
using UnityEngine;

[RequireComponent(typeof(Renderer))]

public class InteractablesBase : MonoBehaviour
{
    //checks name of variable from shader that results in the transition
    private const string highlightProperty = "_HighlightMaster";

    //adjustable valuables for material index and duration
    [SerializeField] private int indexOfMaterial = 0;
    [SerializeField] private float highlightDuration = 0.5f;

    private Renderer interactableRenderer;
    private Material highlightMaterial;
    private Tween highlightTween;
    private bool isHighlighted;

    private void Awake()
    {
        interactableRenderer = GetComponent<Renderer>();
        
        //checks if elements are set
        if (indexOfMaterial < 0 || indexOfMaterial >= interactableRenderer.materials.Length)
        {
            Debug.LogError($"{name} has an invalid material index: {indexOfMaterial}", this);

            enabled = false;
            return;
        }

        highlightMaterial = interactableRenderer.materials[indexOfMaterial];

        if (!highlightMaterial.HasProperty(highlightProperty))
        {
            Debug.LogError($"{name}'s material does not contain {highlightProperty}.", this);

            enabled = false;
        }
    }
    //function to do highlight transition
    public void SetHighlighted(bool highlighted)
    {
        if (isHighlighted == highlighted)
            return;

        isHighlighted = highlighted;
        highlightTween?.Kill();

        float targetValue = highlighted ? 1f : 0f;

        highlightTween = highlightMaterial.DOFloat(targetValue, highlightProperty, highlightDuration).SetEase(Ease.OutQuint);
    }

    //fuction to destroy instance of highlight
    private void OnDestroy()
    {
        highlightTween?.Kill();
    }

}
