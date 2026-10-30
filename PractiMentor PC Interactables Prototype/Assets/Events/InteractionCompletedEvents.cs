using UnityEngine;

public enum InteractionType
{
    SliderCompleted,
    ObjectPlaced,
    ObjectPickedUp,
    TimedInteractionCompleted,
    SocketRotationCompleted,
    ClickablePressed
}
public readonly struct InteractionCompletedEvent
{
    public readonly InteractionType Type;
    public readonly GameObject PrimaryObject;
    public readonly GameObject SecondaryObject;

    public InteractionCompletedEvent(InteractionType type, GameObject primaryObject, GameObject secondaryObject = null)
    {
        Type = type;
        PrimaryObject = primaryObject;
        SecondaryObject = secondaryObject;
    }
}
