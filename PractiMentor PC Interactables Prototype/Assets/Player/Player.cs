using System;
using UnityEngine;

public class Player : MonoBehaviour
{
    public enum PlayerMode
    {
        Navigate,
        SliderInteraction
    }

    [SerializeField] private PlayerMode mode = PlayerMode.Navigate;

    //camera movement
    [SerializeField] private float mouseSensitivity = 2f;
    private float mouseSensitivityInitial;
    
    private float verticalRotation = 0f;
    private Transform cameraTransform;

    //player movement properties
    private Rigidbody rb;
    
    [SerializeField] private float moveSpeed = 5f;
    private float moveSpeedInitial;

    private float moveHorizontal;
    private float moveForward;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        cameraTransform = Camera.main.transform;

        mouseSensitivityInitial = mouseSensitivity;
        moveSpeedInitial = moveSpeed;
    }


    void Update()
    {
        moveHorizontal = Input.GetAxisRaw("Horizontal");
        moveForward = Input.GetAxisRaw("Vertical");
        
        RotateCamera();

        SetPlayerState(mode);
    }

    void FixedUpdate()
    {
        MovePlayer();
    }

    void MovePlayer()
    {
        Vector3 movement = ((transform.right * moveHorizontal) + (transform.forward * moveForward)).normalized;
        Vector3 targetVelocity = movement * moveSpeed;

        Vector3 velocity = rb.linearVelocity;
        velocity.x = targetVelocity.x;
        velocity.z = targetVelocity.z;
        rb.linearVelocity = velocity;
    }

    void RotateCamera()
    {
        float horizontalRotation = Input.GetAxis("Mouse X") * mouseSensitivity;
        transform.Rotate(0, horizontalRotation, 0);

        verticalRotation -= Input.GetAxis("Mouse Y") * mouseSensitivity;
        verticalRotation = Mathf.Clamp(verticalRotation, -45f, 45f);

        cameraTransform.localRotation = Quaternion.Euler(verticalRotation, 0, 0);

    }

    void SetPlayerState(PlayerMode playerMode)
    {
        if (Input.GetKeyDown(KeyCode.Mouse1))
        {
            playerMode = PlayerMode.SliderInteraction;
            mouseSensitivity = 0f;
            moveSpeed = 0f;
        }
        
        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            playerMode = PlayerMode.Navigate;
            mouseSensitivity = mouseSensitivityInitial;
            moveSpeed = moveSpeedInitial;
        }
    }

}
