using UnityEngine;
using System.Collections.Generic;

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

    private readonly List<Vector3> checkpointPositions = new List<Vector3>();

    public void PlaceAtCheckpoint(Transform checkpoint)
    {
        if (checkpoint == null)
            return;

        Vector3 checkpointPosition = checkpoint.position;

        bool isDuplicate = checkpointPositions.Count > 0 && Vector3.Distance(checkpointPositions[checkpointPositions.Count - 1], checkpointPosition) < 0.01f;

        if (!isDuplicate)
            checkpointPositions.Add(checkpointPosition);

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

        lineRenderer.enabled = showLineWhenNotHeld || stringIsHeld || isBeingPlaced || IsPlaced;

        if (!lineRenderer.enabled)
            return;

        UpdateStringLine();
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
        Debug.Log($"String received event: {interactionEvent.Type}, " + $"primary: {interactionEvent.PrimaryObject?.name}, " + $"secondary: {interactionEvent.SecondaryObject?.name}", this);

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

        Socket socket = interactionEvent.SecondaryObject.GetComponentInParent<Socket>();

        if (socket == null)
        {
            Debug.LogWarning($"Could not find Socket on {interactionEvent.SecondaryObject.name}", this);

            return;
        }

        if (socket != null)
        {
            connectedSocket = socket;

            Transform checkpoint = socket.PlacementPoint != null ? socket.PlacementPoint : socket.transform;

            Debug.Log($"Recording socket checkpoint: {checkpoint.name} at {checkpoint.position}", this);

            PlaceAtCheckpoint(checkpoint);

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

    private void UpdateStringLine()
    {
        if (lineRenderer == null || spoolAnchor == null || stringEdgeAnchor == null)
            return;

        int checkpointCount = checkpointPositions.Count;

        lineRenderer.positionCount = checkpointCount + 2;

        lineRenderer.SetPosition(0, spoolAnchor.position);

        for (int i = 0; i < checkpointCount; i++)
        {
            lineRenderer.SetPosition(i + 1, checkpointPositions[i]);
        }

        lineRenderer.SetPosition(checkpointCount + 1, stringEdgeAnchor.position);
    }

    public void ClearCheckpointTrail()
    {
        checkpointPositions.Clear();
        currentCheckpoint = null;
        state = StringState.Held;
        isBeingPlaced = false;
        isConnectedToSocket = false;
    }
}
