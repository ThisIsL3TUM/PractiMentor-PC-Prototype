using System;
using UnityEngine;

//this is where socket logic happens
public class Socket : MonoBehaviour
{
    //Socket state categories
    public enum SocketStateType
    {
        Free,
        Occupied,
        RotationPending,
        Locked
    }

    //socket logic (its state + point where items can be put on
    [Header("General Set Up")]
    [SerializeField] private SocketStateType socketBehaviour = SocketStateType.Free;
    [SerializeField] private Transform placementPoint;
    [SerializeField] private Transform finalRotationPoint;

    [Header("Compatibility")]
    [SerializeField] private CoffeeToolType[] acceptedToolTypes;

    public Transform PlacementPoint => placementPoint;
    public Transform FinalRotationPoint => finalRotationPoint;

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

