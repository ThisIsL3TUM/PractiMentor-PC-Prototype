using Unity.VisualScripting;
using UnityEngine;

public class StringBehaviour : MonoBehaviour
{
    public enum StringState
    {
        Held,
        BeingPlaced,
        AtCheckpoint,
        Connected
    }

    [Header("References")]
    [SerializeField] private HandController handController;
    [SerializeField] private GameObject stringEdge;
    [SerializeField] private Transform spoolAnchor;
    [SerializeField] private Transform stringEdgeAnchor;
    [SerializeField] private LineRenderer lineRenderer;

    [Header("Visual Behaviour")]
    [SerializeField] private bool showLineWhenNotHeld = false;
    [SerializeField] private float anchorOffset = 0.005f;

    [Header("Checkpoint Behaviour")]
    [SerializeField] private StringState state;
    [SerializeField] private Transform currentCheckpoint;

    public bool IsPlaced => state == StringState.AtCheckpoint || state == StringState.Connected;

    private bool isBeingPlaced;
    private bool isConnectedToSocket;

    private Socket connectedSocket;

    public void PlaceAtCheckpoint(Transform checkpoint)
    {
        if (checkpoint == null)
            return;

        currentCheckpoint = checkpoint;
        state = StringState.AtCheckpoint;

        isBeingPlaced = false;
        isConnectedToSocket = false;
    }

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
        if (interactionEvent.Type == InteractionType.SliderCompleted)
        {
            if (interactionEvent.PrimaryObject != stringEdge)
                return;

            PlaceAtCheckpoint(currentCheckpoint);
            return;
        }

        if (interactionEvent.Type != InteractionType.ObjectPlaced)
            return;

        if (interactionEvent.PrimaryObject != stringEdge)
            return;

        if (interactionEvent.SecondaryObject == null)
            return;

        Socket socket = interactionEvent.SecondaryObject.GetComponent<Socket>();

        if (socket != null)
        {
            connectedSocket = socket;
            state = StringState.Connected;
            isConnectedToSocket = true;
        }
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

        state = StringState.Connected;
        isBeingPlaced = false;
        isConnectedToSocket = true;
    }

    public void CancelSocketPlacement()
    {
        isBeingPlaced = false;
    }

    public void BeginCheckpointPlacement(Transform checkpoint)
    {
        if (checkpoint == null)
            return;

        currentCheckpoint = checkpoint;
        state = StringState.BeingPlaced;
        isBeingPlaced = true;
    }

    public bool ReleaseFromSocket()
    {
        if (connectedSocket == null)
            return true;

        bool released = connectedSocket.ReleaseObject(stringEdge);

        if (!released)
            return false;

        connectedSocket = null;
        isConnectedToSocket = false;
        state = StringState.Held;

        return true;
    }
}
