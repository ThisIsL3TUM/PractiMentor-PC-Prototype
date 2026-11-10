using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;

public class HandController : MonoBehaviour
{
    public enum Hand
    {
        Left,
        Right
    }

    [Header("References")]
    [SerializeField] private GameObject player;
    [SerializeField] private Transform holdPosR;
    [SerializeField] private Transform holdPosL;
    [SerializeField] private Transform sliderHoldPosR;
    [SerializeField] private Transform sliderHoldPosL;

    [Header("Movement")]
    [SerializeField] private string holdLayerName = "Hold Layer";
    [SerializeField, Min(0f)] private float pickUpDuration = 0.5f;
    [SerializeField, Min(0f)] private float placementDuration = 0.5f;

    private GameObject heldObjR;
    private GameObject heldObjL;

    private Collider playerCollider;
    private int holdLayer;
    private bool movingFromSocket;

    public GameObject HeldRight => heldObjR;
    public GameObject HeldLeft => heldObjL;

    public bool AnyObjectHeld => heldObjR != null || heldObjL != null;

    private void Awake()
    {
        if (player == null)
            player = gameObject;

        playerCollider = player.GetComponent<Collider>();
        holdLayer = LayerMask.NameToLayer(holdLayerName);

        if (holdLayer == -1)
            Debug.LogError($"Layer '{holdLayerName}' does not exist.", this);

        if (holdPosR == null)
            Debug.LogError("Right-hand hold position is missing.", this);

        if (holdPosL == null)
            Debug.LogError("Left-hand hold position is missing.", this);
    }
    public GameObject GetHeldObject(Hand hand)
    {
        return hand == Hand.Right ? heldObjR : heldObjL;
    }

    public bool TryFindFreeHand(GameObject objectToPickUp, out Hand hand)
    {
        if (CanUseHand(objectToPickUp, Hand.Right) && heldObjR == null)
        {
            hand = Hand.Right;
            return true;
        }

        if(CanUseHand(objectToPickUp, Hand.Left) && heldObjL == null)
        {
            hand = Hand.Left;
            return true;
        }

        hand = default;
        return false;
    }
    public bool TryPickUp(GameObject objectToPickUp, Hand hand, bool sliderMode)
    {
        if (sliderMode)
            return false;

        if (objectToPickUp == null)
            return false;

        if (GetHeldObject(hand) != null)
            return false;

        GameObject grabbableRoot = GetGrabbableRoot(objectToPickUp);

        if (!CanUseHand(grabbableRoot, hand))
            return false;

        SetHeldObject(hand, grabbableRoot);

        if (holdLayer != -1)
            grabbableRoot.layer = holdLayer;

        IgnorePlayerCollision(grabbableRoot, true);

        Transform holdPosition = GetHoldPosition(hand, false);

        StartCoroutine(FinishPickupAnimation(grabbableRoot, holdPosition));

        return true;
    }
    public bool TryPlaceHeldObject(Hand hand, Transform placementPoint, Action<GameObject> onComplete = null)
    {
        GameObject heldObject = GetHeldObject(hand);

        if (heldObject == null || placementPoint == null)
            return false;

        IgnorePlayerCollision(heldObject, false);

        heldObject.layer = 0;
        SetHeldObject(hand, null);

        Sequence placementSequence = DOTween.Sequence();

        placementSequence.Join(heldObject.transform.DOMove(placementPoint.position, placementDuration).SetEase(Ease.InOutSine));

        placementSequence.Join(heldObject.transform.DORotateQuaternion(placementPoint.rotation,placementDuration).SetEase(Ease.InOutSine));

        placementSequence.OnComplete(() =>
        {
            onComplete?.Invoke(heldObject);
        });

        return true;
    }
    private void SetHeldObject(Hand hand, GameObject obj)
    {
        if (hand == Hand.Right)
            heldObjR = obj;
        else
            heldObjL = obj;
    }
    private Transform GetHoldPosition(Hand hand, bool sliderMode)
    {
        if (hand == Hand.Right)
            return sliderMode ? sliderHoldPosR : holdPosR;

        return sliderMode ? sliderHoldPosL : holdPosL;
    }
    public bool TryFindHeldHand(out Hand hand)
    {
        if (heldObjR != null)
        {
            hand = Hand.Right;
            return true;
        }

        if (heldObjL != null)
        {
            hand = Hand.Left;
            return true;
        }

        hand = default;
        return false;
    }
    private bool CanUseHand(GameObject obj, Hand hand)
    {
        if (obj == null)
            return false;

        Grabbable grabbable = obj.GetComponentInParent<Grabbable>();

        if (grabbable == null)
            return false;

        return grabbable.HandDirection switch
        {
            Grabbable.GrabbableHandDirection.Both => true,
            Grabbable.GrabbableHandDirection.Left => hand == Hand.Left,
            Grabbable.GrabbableHandDirection.Right => hand == Hand.Right,
            _ => false
        };
    }
    public void UpdateHeldObjects(bool sliderMode)
    {
        MoveObject(Hand.Right, sliderMode);
        MoveObject(Hand.Left, sliderMode);
    }
    private IEnumerator FinishPickupAnimation(GameObject grabbableRoot, Transform holdPosition)
    {
        if (grabbableRoot == null || holdPosition == null)
            yield break;

        movingFromSocket = true;

        Tween pickupTween = grabbableRoot.transform.DOMove(holdPosition.position, pickUpDuration).SetEase(Ease.InOutSine);

        yield return pickupTween.WaitForCompletion();

        movingFromSocket = false;
    }
    private void MoveObject(Hand hand, bool sliderMode)
    {
        if (movingFromSocket)
            return;

        GameObject heldObject = GetHeldObject(hand);

        if (heldObject == null)
            return;

        Transform holdPosition = GetHoldPosition(hand, sliderMode);

        if (holdPosition != null)
            heldObject.transform.position = holdPosition.position;
    }

    private GameObject GetGrabbableRoot(GameObject hitObject)
    {
        Grabbable grabbable = hitObject.GetComponentInParent<Grabbable>();

        return grabbable != null ? grabbable.gameObject : hitObject;
    }
    private void IgnorePlayerCollision(GameObject objectObject, bool ignoreCollision)
    {
        if (objectObject == null || playerCollider == null)
            return;

        Collider objectCollider = objectObject.GetComponent<Collider>();

        if (objectCollider != null)
        {
            Physics.IgnoreCollision(objectCollider, playerCollider, ignoreCollision);
        }
    }
}
