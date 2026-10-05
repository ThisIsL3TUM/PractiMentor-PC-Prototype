using UnityEngine;
using UnityEngine.InputSystem;

public class CursorUI : MonoBehaviour
{
    [SerializeField] private InputActionReference pointerPosAction;

    private RectTransform cursorTransform;
    private Canvas parentCanvas;
    private RectTransform canvasRectTransform;
    private Camera canvasCamera;

    private void Awake()
    {
        cursorTransform = GetComponent<RectTransform>();
        parentCanvas = GetComponent<Canvas>();

        if (parentCanvas != null)
        {
            canvasRectTransform = GetComponent<RectTransform>();
            canvasCamera = parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : parentCanvas.worldCamera;
        }
    }

    private void OnEnable()
    {
        Cursor.visible = false;
        pointerPosAction.action.performed += OnPointerPositionChanged;
    }

    private void OnDisable()
    {
        Cursor.visible = true;
        pointerPosAction.action.performed -= OnPointerPositionChanged;
    }

    private void OnPointerPositionChanged(InputAction.CallbackContext ctx)
    {
        if (cursorTransform != null || canvasRectTransform == null)
        {
            var mousePosition= ctx.ReadValue<Vector2>();

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRectTransform, mousePosition, canvasCamera, out var localPoint))
            {
                cursorTransform.anchoredPosition = localPoint;
            }
        }
    }
}
