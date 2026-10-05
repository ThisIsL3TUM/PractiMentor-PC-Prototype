using UnityEngine;

public class Socket : MonoBehaviour
{
    public enum SocketStateType
    {
        Free = 0,
        Occupied = 1
    }

    [SerializeField] private SocketStateType socketBehaviour = SocketStateType.Free;
    [SerializeField] private Transform placementPoint;

    public bool TryOccupySocket(GameObject item, out Transform target)
    {
        target = null;

        if(socketBehaviour != SocketStateType.Free || item == null)
            return false;
        

        socketBehaviour = SocketStateType.Occupied;
        target = placementPoint != null ? placementPoint : transform;
        return true;
    }

}

