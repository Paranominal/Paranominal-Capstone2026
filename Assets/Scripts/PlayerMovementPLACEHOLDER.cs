using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    //public GameSettings sens;


    public Camera playerCamera;
    public float walkSpeed = 6f;
    public float runSpeed = 12f;
    public float jumpPower = 7f;
    public float gravity = 10f;
    public float lookSpeed = 100f;
    public float lookXLimit = 60;
    public float defaultHeight = 2f;
    public float crouchHeight = 1f;
    public float crouchSpeed = 3f;

    private Vector3 moveDirection = Vector3.zero;
    private float rotationX = 0;
    private CharacterController characterController;

    private bool canMove = true;

    private Vector3 SpawnLocation;
    public AudioClip Woosh;

    public List<GameObject> RespawnObjects;

    void Start()
    {
        //lookSpeed = sens.sens;

        
        characterController = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        SpawnLocation = transform.position;


        //RespawnObjects = GameObject.FindGameObjectsWithTag("CanPickUp").ToList();
        //RespawnObjects.Add(GameObject.FindGameObjectWithTag("Key"));
    }

    void Update()
    {
        Vector3 forward = transform.TransformDirection(Vector3.forward);
        Vector3 right = transform.TransformDirection(Vector3.right);

        // bool isRunning = Input.GetKey(KeyCode.LeftShift);
        float curSpeedX = canMove ? walkSpeed * Input.GetAxis("Vertical") : 0;
        float curSpeedY = canMove ? walkSpeed * Input.GetAxis("Horizontal") : 0;
        float movementDirectionY = moveDirection.y;
        moveDirection = (forward * curSpeedX) + (right * curSpeedY);

        // if (Input.GetButton("Jump") && canMove && characterController.isGrounded)
        // {
        //     moveDirection.y = jumpPower;
        // }
        // else
        // {
        //     moveDirection.y = movementDirectionY;
        // }

        // if (!characterController.isGrounded)
        // {
        //     moveDirection.y -= gravity * Time.deltaTime;
        // }


        // if (Input.GetKey(KeyCode.R) && canMove)
        // {
        //     characterController.height = crouchHeight;
        //     walkSpeed = crouchSpeed;
        //     runSpeed = crouchSpeed;

        // }
        // else
        // {
        //     characterController.height = defaultHeight;
        //     walkSpeed = 6f;
        //     runSpeed = 12f;
        // }

        characterController.Move(moveDirection * Time.deltaTime);
        

        if (canMove)
        {
            rotationX += -Input.GetAxis("Mouse Y");
            rotationX = Mathf.Clamp(rotationX, -lookXLimit, lookXLimit);
            playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0, 0);
            transform.rotation *= Quaternion.Euler(0, Input.GetAxis("Mouse X"), 0);
        }

    }

    // public void Respawn()
    // {
    //     characterController.enabled = false;
    //     transform.position = SpawnLocation;
    //     characterController.enabled = true;

    //     moveDirection.y =0;

    //     GetComponent<AudioSource>().PlayOneShot(Woosh);

    //     //also reset all objects or can be softlocked

    //     foreach(GameObject Obj in RespawnObjects)
    //     {
    //         Obj.GetComponent<Spawning>().Respawn();
    //     }
        

        
    // }
}