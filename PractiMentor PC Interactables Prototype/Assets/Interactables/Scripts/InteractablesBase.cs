using DG.Tweening;
using UnityEditor.TerrainTools;
using UnityEngine;

[RequireComponent(typeof(Renderer))]

public class InteractablesBase : MonoBehaviour
{
    private const string highlightProperty = "_HighlightMaster";

    //[SerializeField] private Material baseMaterial;

    [SerializeField] private int indexOfMaterial = 0;
    [SerializeField] private float highlightDuration = 0.5f;

    private Renderer interactableRenderer;
    private Material highlightMaterial;
    private Tween highlightTween;
    private bool isHighlighted;

    private void Awake()
    {
        interactableRenderer = GetComponent<Renderer>();

        if (indexOfMaterial < 0 || indexOfMaterial >= interactableRenderer.materials.Length)
        {
            Debug.LogError($"{name} has an invalid material index: {indexOfMaterial}", this);

            enabled = false;
            return;
        }

        highlightMaterial = interactableRenderer.materials[indexOfMaterial];

        if (!highlightMaterial.HasProperty(highlightProperty))
        {
            Debug.LogError($"{name}'s material does not contain {highlightProperty}.",this);

            enabled = false;
        }
    }
    public void SetHighlighted(bool highlighted)
    {
        if (isHighlighted == highlighted)
            return;

        isHighlighted = highlighted;
        highlightTween?.Kill();

        highlightTween = highlightMaterial.DOFloat(highlighted ? 1f : 0f,highlightProperty,highlightDuration).SetEase(Ease.OutQuint);
    }

    private void OnDestroy()
    {
        highlightTween?.Kill();
    }

}
