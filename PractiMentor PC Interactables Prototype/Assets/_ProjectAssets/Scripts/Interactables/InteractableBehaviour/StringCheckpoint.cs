using UnityEngine;

public class StringCheckpoint : MonoBehaviour
{
    [Header("Placement")]
    [SerializeField] private Transform placementPoint;

    [Header("References")]
    [SerializeField] private StringBehaviour stringBehaviour;

    public Transform PlacementPoint => placementPoint != null ? placementPoint : transform;

    public void BeginStringPlacement()
    {
        if (stringBehaviour == null)
        {
            Debug.LogWarning($"{name}: StringBehaviour reference is missing.",this);

            return;
        }

        stringBehaviour.BeginCheckpointPlacement(PlacementPoint);
    }

    private void OnTriggerEnter(Collider other)
    {
        StringBehaviour detectedString = other.GetComponentInParent<StringBehaviour>();

        if (detectedString == null)
            return;

        stringBehaviour = detectedString;
        BeginStringPlacement();
    }
}
