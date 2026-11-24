using UnityEngine;

public class StringBehaviour : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private HandController handController;
    [SerializeField] private GameObject stringEdge;
    [SerializeField] private Transform spoolAnchor;
    [SerializeField] private Transform stringEdgeAnchor;
    [SerializeField] private LineRenderer lineRenderer;

    [Header("Visual Behaviour")]
    [SerializeField] private bool showLineWhenNotHeld = false;
    [SerializeField] private float anchorOffset = 0.005f;

    private void Awake()
    {
        if (lineRenderer == null)
            lineRenderer = GetComponentInChildren<LineRenderer>();

        if (lineRenderer != null)
        {
            lineRenderer.useWorldSpace = true;
            lineRenderer.positionCount = 2;
            lineRenderer.enabled = showLineWhenNotHeld;
        }
    }

    private void LateUpdate()
    {
        if (lineRenderer == null || spoolAnchor == null || stringEdgeAnchor == null || handController == null || stringEdge == null)
            return;
        

        bool stringIsHeld = handController.HeldRight == stringEdge || handController.HeldLeft == stringEdge;

        lineRenderer.enabled = showLineWhenNotHeld || stringIsHeld;

        if (!lineRenderer.enabled)
            return;

        Vector3 start = spoolAnchor.position;
        Vector3 end = stringEdgeAnchor.position;

        Vector3 direction = (end - start).normalized;

        lineRenderer.SetPosition(0, start + direction * anchorOffset);
        lineRenderer.SetPosition(1, end - direction * anchorOffset);
    }
}
