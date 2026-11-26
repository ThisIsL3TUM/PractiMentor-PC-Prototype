using Unity.VisualScripting;
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

    private bool isBeingPlaced;
    private bool isConnectedToSocket;

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

        lineRenderer.enabled = showLineWhenNotHeld || stringIsHeld || isBeingPlaced || isConnectedToSocket;

        if (!lineRenderer.enabled)
            return;

        Vector3 start = spoolAnchor.position;
        Vector3 end = stringEdgeAnchor.position;

        Vector3 direction = (end - start).normalized;

        lineRenderer.SetPosition(0, start + direction * anchorOffset);
        lineRenderer.SetPosition(1, end - direction * anchorOffset);
    }

    private void OnEnable()
    {
        if (handController != null)
            handController.InteractionCompleted += OnInteractionCompleted;
    }

    private void OnDisable()
    {
        if (handController != null)
            handController.InteractionCompleted -= OnInteractionCompleted;
    }

    private void OnInteractionCompleted(InteractionCompletedEvent interactionEvent)
    {
        if (interactionEvent.Type != InteractionType.ObjectPlaced)
            return;

        if (interactionEvent.PrimaryObject != stringEdge)
            return;

        if (interactionEvent.SecondaryObject == null)
            return;

        Socket socket =
            interactionEvent.SecondaryObject.GetComponent<Socket>();

        if (socket != null)
            isConnectedToSocket = true;
    }

    public void BeginSocketPlacement(GameObject objectBeingPlaced)
    {
        if (objectBeingPlaced == stringEdge)
            isBeingPlaced = true;
    }

    public void ConfirmSocketConnection(GameObject objectBeingPlaced)
    {
        if (objectBeingPlaced != stringEdge)
            return;

        isBeingPlaced = false;
        isConnectedToSocket = true;
    }

    public void CancelSocketPlacement()
    {
        isBeingPlaced = false;
    }
}
