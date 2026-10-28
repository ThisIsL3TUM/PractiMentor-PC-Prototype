using UnityEngine;

public enum CoffeeToolType
{
    Portafilter,
    CoffeeBag,
    CoffeeTamper
}
public class CoffeeTool : MonoBehaviour
{
    [SerializeField] private CoffeeToolType toolType;
    public CoffeeToolType ToolType => toolType;
}
