using UnityEngine;

//THIS WHOLE SCRIPT NEEDS TO BE REWORKED TO ACT AS INTENDED!!!
public class CursorManager : MonoBehaviour
{
    public static CursorManager Instance { get; private set; }

    //cursor textures
    [SerializeField] private Texture2D cursorTextureDefault;
    [SerializeField] private Texture2D cursorTextureClick;

    [SerializeField] private Vector2 clickPos = Vector2.zero;

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
       Cursor.SetCursor(cursorTextureDefault, clickPos, CursorMode.Auto);
       Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        Cursor.visible = false;
    }

    public void SetToMode(ModeOfCursor modeOfCursor)
    {
        switch (modeOfCursor)
        {
            case ModeOfCursor.Default:
                Cursor.SetCursor(cursorTextureClick, clickPos, CursorMode.Auto);
                break;
            case ModeOfCursor.Click:
                Cursor.SetCursor(cursorTextureClick, clickPos, CursorMode.Auto);
                break;
        }
    }

    public enum ModeOfCursor
    {
        Default,
        Click
    }
}
