using UnityEngine;
using DG.Tweening;

public class InteractablesBase : MonoBehaviour
{
    public Material baseMaterial;
    public int indexOfMaterial = 0;

    private void Awake()
    {
        baseMaterial = GetComponent<Renderer>().materials[indexOfMaterial];
    }

    public void OnHoverIn(bool show)
    {
        baseMaterial.DOFloat(show ? 1 : 0, "_HighlightMaster", 1f).SetEase(Ease.OutQuint);
    }

}
