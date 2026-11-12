using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class BrewProgressUI : MonoBehaviour
{
    [SerializeField] private InteractionSequenceController sequenceController;

    private ProgressBar brewBar;
    private VisualElement root;

    private void OnEnable()
    {
        root = GetComponent<UIDocument>().rootVisualElement;
        brewBar = root.Q<ProgressBar>("brew-bar");

        brewBar.lowValue = 0f;
        brewBar.highValue = 100f;
        brewBar.value = 0f;

        root.style.display = DisplayStyle.None;

        sequenceController.BrewStarted += Show;
        sequenceController.BrewProgressChanged += UpdateProgress;
        sequenceController.BrewCompleted += Hide;
    }

    private void OnDisable()
    {
        if (sequenceController == null)
            return;

        sequenceController.BrewStarted -= Show;
        sequenceController.BrewProgressChanged -= UpdateProgress;
        sequenceController.BrewCompleted -= Hide;
    }

    private void Show()
    {
        brewBar.value = 0f;
        root.style.display = DisplayStyle.Flex;
    }

    private void UpdateProgress(float progress)
    {
        brewBar.value = progress * 100f;
    }

    private void Hide()
    {
        brewBar.value = 100f;
        root.style.display = DisplayStyle.None;
    } 
}
