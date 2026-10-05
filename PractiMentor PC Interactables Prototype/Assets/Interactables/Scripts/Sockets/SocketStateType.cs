using UnityEngine;

public class Socket : MonoBehaviour
{
    public enum SocketStateType
    {
        Free = 0,
        Occupied = 1
    }

    [SerializeField]
    private SocketStateType socketBehaviour = SocketStateType.Free;

    public bool isSocketFree => socketBehaviour == SocketStateType.Free;

    public bool TryOccupySocket()
    {
        if(!isSocketFree)
            return false;

        socketBehaviour = SocketStateType.Occupied;
        return true;
    }

}

