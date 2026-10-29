using System;
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
    [Header("General Set Up")]
    [SerializeField] private SocketStateType socketBehaviour = SocketStateType.Free;
    [SerializeField] private Transform placementPoint;

    [Header("Compatibility")]
    [SerializeField] private CoffeeToolType[] acceptedToolTypes;

    public bool CanAccept(GameObject item)
    {
        if (socketBehaviour != SocketStateType.Free || item == null)
            return false;

        CoffeeTool tool = item.GetComponentInParent<CoffeeTool>();

        if (tool == null) 
            return false;

        return Array.IndexOf(acceptedToolTypes, tool.ToolType) >= 0;
    }
    //function to put object in socket
    public bool TryOccupySocket(GameObject item, out Transform target)
    {
        target = null;

        if(!CanAccept(item))
            return false;
        

        socketBehaviour = SocketStateType.Occupied;
        target = placementPoint != null ? placementPoint : transform;
        return true;
    }

}

