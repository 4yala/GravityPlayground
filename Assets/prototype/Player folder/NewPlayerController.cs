using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

public class NewPlayerController : MonoBehaviour
{
    #region Variables
    [Header ("Player components")]
    [SerializeField] Rigidbody rb;
    [SerializeField] Animator playerAni;

    [Header("Movement Variables")]
    [SerializeField] ConstantForce myGravity;
    [Tooltip("The total gravitational pull that the player will have in any direction")]
    [SerializeField] float gravityForce;
    [SerializeField] float moveSpeed;
    [SerializeField] float maxSpeedWalk;
    [SerializeField] float deceleration;
    [SerializeField] float rotationSpeed;
    [SerializeField] float jumpForce;
    [SerializeField] float groundedDrag;
    [SerializeField] float zeroGravDrag;
    
    [Header("Debug elements to inspect")]
    [SerializeField] Vector2 moveInput;
    [SerializeField] Vector3 gravitationalRotation;

    [Header("Assets required")]
    [SerializeField] Camera myCamera;

    [Header("Player States")]
    [SerializeField] bool zerograv;
    [SerializeField] bool shifted;
    [SerializeField] bool immobile;
    [SerializeField] bool grounded;
    [SerializeField] bool diving;

    #endregion

    // Start is called before the first frame update
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        myCamera = FindFirstObjectByType<Camera>();
    }

    // Update is called once per frame
    void Update()
    {
        IsGrounded();
        if(!grounded && !diving)
        {
            HighGround();
        }
    }

    // FixedUpdate for non frame dependent functions, I.e. physics
    void FixedUpdate()
    {
        if(!immobile)
        {
            //gather the input information.
            Vector3 inputDirection = new Vector3(moveInput.x, 0f, moveInput.y).normalized;

            //only if there's an input.
            if(inputDirection.magnitude > 0.1f)
            {
                //flatten y as we are not using that axis.
                Vector3 cameraForward = (myCamera.transform.forward);
                cameraForward.y = 0f;
                cameraForward.Normalize();
                Vector3 cameraRight = (myCamera.transform.right);
                cameraRight.y = 0f;
                cameraRight.Normalize();

                Vector3 targetDirection = (cameraForward * moveInput.y + cameraRight * moveInput.x).normalized;

                if (targetDirection.magnitude > 0.1f)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
                }
                //more to consider in the future! such as damping the Y value on speed. for now This is just a base.
                if (rb.velocity.magnitude > maxSpeedWalk)
                {
                    rb.velocity = rb.velocity.normalized * maxSpeedWalk;
                }
                //end with moving forward from character perspective
                rb.AddForce(transform.forward * moveSpeed, ForceMode.Acceleration);
                playerAni.SetBool("Moving", true);
            }
            else
            {
                playerAni.SetBool("Moving", false);

                //break the player when movement stops, only if its touching the floor
                if(grounded)
                {
                    rb.AddForce(rb.velocity * -deceleration, ForceMode.Force);
                }
            }
        }
    }

    #region StatusChecks
    void IsGrounded()
    {
        RaycastHit hit;
        float rayLength = 1.1f; // Adjust based on your character's size
        Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.down) * rayLength, Color.green);
        if (Physics.Raycast(transform.position, Vector3.down, out hit, rayLength))
        {
            if (diving)
            {
                Debug.Log("Landed");
                diving = false;
                playerAni.SetTrigger("Resume");
            }
            rb.drag = groundedDrag;
            grounded = true;
            return;
        }
        else
        {
            grounded = false;
            if (!diving)
            {
                rb.drag = 1f;
            }
        }
        
    }
    void HighGround()
    {
        RaycastHit hit;
        float rayLength = 15f; // Adjust based on your character's size
        Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.down) * rayLength, Color.red);
        if (!Physics.Raycast(transform.position, Vector3.down, out hit, rayLength))
        {
            diving = true;
            Debug.Log("Flying");
            playerAni.SetTrigger("Diving");
            rb.drag = 0f;
        }
        else
        {
            diving = false;
        }
    }
    #endregion
    //Here goes any events called by the input system
    #region InputEvents
    public void ToggleGrav(InputAction.CallbackContext context)
    {
        shifted = true;
        if(!zerograv)
        {
            myGravity.force = new Vector3 (0, 0, 0);
            zerograv = true;
            immobile = true;
            rb.freezeRotation = false;
            rb.drag = zeroGravDrag;
        }
        else
        {
            Vector3 cameraDirection = myCamera.transform.forward;
            Vector3 newGravity = cameraDirection.normalized * gravityForce;
            myGravity.force = newGravity;
            zerograv = false;
            immobile = false;
            grounded = false;
            rb.freezeRotation = true;
            rb.drag = 0f;
        }
    }
    public void Revert(InputAction.CallbackContext context)
    {
        if (shifted)
        {
            myGravity.force = new Vector3 (0,-gravityForce,0);
            zerograv = false;
            immobile = false;
            rb.freezeRotation = true;
            gameObject.transform.rotation = Quaternion.Euler(0,gameObject.transform.rotation.y,0);
            shifted = false;
        }
    }
    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }
    public void OnJump(InputAction.CallbackContext context)
    {
        if (grounded)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }
    }
    #endregion
}
