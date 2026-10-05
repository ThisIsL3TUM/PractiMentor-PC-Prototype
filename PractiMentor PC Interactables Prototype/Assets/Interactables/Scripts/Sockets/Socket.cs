using UnityEngine;

//this is where socket logic happens
public class Socket : MonoBehaviour
{
    //Socket state categories
    public enum SocketStateType
    {
        Free = 0,
        Occupied = 1
    }

    //socket logic (its state + point where items can be put on
    [SerializeField] private SocketStateType socketBehaviour = SocketStateType.Free;
    [SerializeField] private Transform placementPoint;

    //function to put object in socket
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

