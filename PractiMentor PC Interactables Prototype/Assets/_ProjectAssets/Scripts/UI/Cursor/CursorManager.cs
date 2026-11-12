using UnityEngine;

//THIS WHOLE SCRIPT NEEDS TO BE REWORKED TO ACT AS INTENDED!!!
public class CursorManager : MonoBehaviour
{
    public static CursorManager Instance { get; private set; }

    //cursor textures
    [SerializeField] private Texture2D cursorTextureDefault;
    [SerializeField] private Texture2D cursorTextureClick;

    [SerializeField] private Vector2 clickPos = Vector2.zero;

    private Player player;

    private void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        SetInteractionMode(false);
    }

    public void SetToMode(ModeOfCursor modeOfCursor)
    {
        switch (modeOfCursor)
        {
            case ModeOfCursor.Default:
                Cursor.SetCursor(cursorTextureDefault, clickPos, CursorMode.Auto);
                break;
            case ModeOfCursor.Click:
                Cursor.SetCursor(cursorTextureClick, clickPos, CursorMode.Auto);
                break;
        }
    }

    public void SetInteractionMode(bool isInteracting)
    {
        if (isInteracting)
        {
            SetToMode(ModeOfCursor.Click);
            Cursor.lockState = CursorLockMode.Confined;
            Cursor.visible = true;
        }
        else
        {
            SetToMode(ModeOfCursor.Default);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    public enum ModeOfCursor
    {
        Default,
        Click
    }
}
