using System;
using UnityEngine;

public class Clickable : MonoBehaviour
{
    [Header("General Set Up")]
    [SerializeField] private bool isActivatingWaiting = false;
    [SerializeField, Min(0f)] private float waitingTime = 15f;

    public bool IsActivatingWaiting => isActivatingWaiting;
    public float WaitingTime => waitingTime;
}
