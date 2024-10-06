using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class NewPlayerController : MonoBehaviour
{
    [Header ("Player components")]
    [SerializeField] Rigidbody rb;
    [SerializeField] Animator playerAni;

    [Header("Movement Variables")]
    [SerializeField] ConstantForce myGravity;
    [SerializeField] float gravityForce;
    [SerializeField] float moveSpeed;
    [SerializeField] float deceleration;
    [SerializeField] float rotationSpeed;

    [Header("Debug elements to inspect")]
    [SerializeField] Vector2 moveInput;

    [Header("Assets required")]
    [SerializeField] Camera myCamera;

    [Header("Player States")]
    [SerializeField] bool zerograv;
    [SerializeField] bool immobile;
    [SerializeField] bool grounded;
    [SerializeField] bool diving;


    // Start is called before the first frame update
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    private void FixedUpdate()
    {
        if(!immobile)
        {
            //gather the input information.
            Vector3 inputDirection = new Vector3(moveInput.x, 0f, moveInput.y).normalized;

            //only if there's an input.
            if(inputDirection.magnitude > 0.1f)
            {
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

                //end with moving forward
                rb.AddForce(transform.forward * moveSpeed, ForceMode.Acceleration);
                playerAni.SetBool("Moving", true);
            }
            else
            {
                playerAni.SetBool("Moving", false);
            }
            //rb.AddForce(Movedirection * moveSpeed, ForceMode.Acceleration);
        }
    }
    //Here goes any events called by the input system
    #region InputEvents
    public void ToggleGrav()
    {
        if(!zerograv)
        {
            myGravity.force = new Vector3 (0, 0, 0);
            zerograv = true;
            immobile = true;
        }
        else
        {
            Vector3 cameraDirection = myCamera.transform.forward;
            Vector3 newGravity = cameraDirection.normalized * gravityForce;
            myGravity.force = newGravity;
            zerograv = false;
            immobile = false;
        }
    }
    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }
    #endregion
}
