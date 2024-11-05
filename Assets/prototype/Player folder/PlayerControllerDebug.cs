using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
using UnityEngine.InputSystem;
public class PlayerControllerDebug : MonoBehaviour
{
    #region Variables
    [Header ("Player components")]
    [SerializeField] Rigidbody rb;
    [SerializeField] Animator playerAni;
    [SerializeField] ConstantForce myGravity; 
    
    [Header(("Camera components"))]
    [SerializeField] Camera myCamera;
    [SerializeField] GameObject myCameraOrientation;
    [SerializeField] CinemachineFreeLook myCameraCm;

    [Header("Grounded Movement Variables")]
    [SerializeField] float gravityForce;
    [SerializeField] float moveSpeed;
    [SerializeField] float maxSpeedWalk;
    [SerializeField] float deceleration;
    [SerializeField] float rotationSpeed;
    [SerializeField] float jumpForce;
    [SerializeField] float groundedDrag;
    [SerializeField] float zeroGravDrag;
    [SerializeField] float freeFallDrag;
    
    [Header("Aerial Movement Variables ")]
    [SerializeField] float jumpForwardSpeed;
    [SerializeField] float airDiveSpeed;
    [SerializeField] float maxDiveSpeed;
    [SerializeField] float diveAcceleration;
    [SerializeField] float diveRotationSpeed;
    
    [Header("Debug elements to inspect")]
    [SerializeField] Vector2 moveInput;
    [SerializeField] Vector3 gravitationalDirection;
    [SerializeField] float rotateInput;
    [SerializeField] float diveLength;
    
    [Header("Player States")]
    [SerializeField] bool zerograv;
    [SerializeField] bool shifted;
    [SerializeField] bool immobile;
    [SerializeField] bool grounded;
    [SerializeField] bool diving;
    [SerializeField] bool shiftDiving; //to be used only when toggling out from zero gravity, in order to smooth landing.
    [SerializeField] bool cameraTransitioned;
    [SerializeField] List<string> animationBools;
    #endregion
    // Start is called before the first frame update
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        gravitationalDirection = Vector3.down;
    }

    // Update is called once per frame
    void Update()
    {
        AnimationStates();
        if (!zerograv)
        {
            //check grounded
            CheckGrounded();
            //check if should begin diving, might need exceptions as a similar behaviour to dive should be attempted when gravity changes in low heights, etc
            if (!grounded && !diving)
            {
                CheckDive();
            }
        }
        //camera checks??
    }
    
    // Recurring but not frame dependent functions
    void FixedUpdate()
    {
        //relatively grounded movement (including jumping motion)
        if (!immobile && !diving)
        {
            //gather input information
            Vector3 inputDirection = new Vector3(moveInput.x, 0, moveInput.y).normalized;
            
            //execute when input exists
            if (inputDirection.magnitude > 0.1f)
            {
                //calculate directions based on definition of up by the perspective of the camera
                Vector3 cameraForward = Vector3.ProjectOnPlane(myCamera.transform.forward, myCameraOrientation.transform.up);
                Vector3 cameraRight = Vector3.ProjectOnPlane(myCamera.transform.right, myCameraOrientation.transform.up);
                
                //translate a character direction based on input
                Vector3 targetDirection = (cameraForward * moveInput.y  + cameraRight * moveInput.y).normalized;
                
                //calculate and make the player face the direction
                Quaternion targetRotation = Quaternion.LookRotation(targetDirection, myCameraOrientation.transform.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
                
                //finalise with character forward movement only, no strafing
                if (grounded)
                {
                    rb.AddForce(transform.forward * moveSpeed, ForceMode.Acceleration);
                }
                else
                {
                    rb.AddForce(transform.forward * jumpForwardSpeed, ForceMode.Acceleration);
                }
            }
            //finally, if no input is made break the player if it's grounded
            else
            {
                if (grounded)
                {
                    rb.AddForce(rb.velocity * -deceleration, ForceMode.Force);
                }
            }
        }
        //aerial movement (diving in the air)
        else if (!immobile && diving)
        {
            //movement is enabled when character is in proper position (head first)
            if (transform.up == gravitationalDirection)
            {
                Debug.Log("missing function");
                //fix camera
                //myorientationrotatetowardsmaybe
                //check if there's active input for character rotation
                if (rotateInput != 0)
                {
                    float rotationAmount = diveRotationSpeed * rotateInput * Time.deltaTime;
                    transform.Rotate(0, rotationAmount, 0);
                    //fix camera when it rotates??
                }
                
                //calculate movement
                Vector3 targetMovement = -transform.forward * moveInput.y + transform.right * moveInput.x;

                if (targetMovement.magnitude > 0.1f)
                {
                    rb.AddForce(targetMovement * airDiveSpeed, ForceMode.Acceleration);
                    //attempt to lock camera when movement
                }
                else
                {
                    //release camera, no function yet
                }

                //properly implement terminal velocity later
                //if up velocity > max dive speed.....
                
                //double checker, not sure if this is necessary
                //gravitational direction is already between 1- and 1, but normalize rounds it up to 2 decimals?
                Vector3 gravNormalized = gravitationalDirection.normalized;
                Quaternion targetRotation = Quaternion.FromToRotation(transform.up, gravNormalized) * transform.rotation;
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, 50f * Time.fixedDeltaTime);
            }
            
        }
    }
    
    //Events that are called upon to make changes
    #region Reaction events
    //flips the player upright when landing after a free fall / dive / gravity shift
    IEnumerator LandPlayer(RaycastHit hit, float duration, bool smoothGravity)
    {
        float elapsedTime = 0;
        //set up to calculate the way the character will flip
        Quaternion targetRotation = Quaternion.identity;
        //smooth with surface
        if (smoothGravity)
        {
            targetRotation = Quaternion.FromToRotation(transform.up, hit.normal) * transform.rotation;
            gravitationalDirection = -hit.normal;
        }
        //retain current direction
        else
        {
            targetRotation = Quaternion.FromToRotation(transform.up, -gravitationalDirection.normalized) * transform.rotation;
        }
        
        Quaternion startRotation = transform.rotation;
        
        // start slerp based on desired length
        while (elapsedTime < duration)
        {
            float t = Mathf.Clamp01(elapsedTime / duration);
            transform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        //double checkers
        rb.freezeRotation = true;
        transform.rotation = targetRotation;
        //set status
        grounded = true;
        if (smoothGravity)
        {
            Vector3 newGravity = gravitationalDirection.normalized * gravityForce;
            myGravity.force = newGravity;
        }
        //now move on to do camera
        StartCoroutine(OrientateCamera(0.5f, transform.up, true));
        
        yield return null;
    }
    //slerp the camera to its new orientation smoothly
    IEnumerator OrientateCamera(float duration, Vector3 desiredOrientation, bool requireCentering)
    {
        float elapsedTime = 0;
        Quaternion startRotation = myCameraOrientation.transform.rotation;
        Quaternion targetRotation = Quaternion.FromToRotation(myCameraOrientation.transform.up, desiredOrientation) * startRotation;
        if (requireCentering)
        {
            //implement behaviour for recentering, will look into later!
        }
        while (elapsedTime < duration)
        {
            float t = Mathf.Clamp01(elapsedTime / duration);
            myCameraOrientation.transform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        myCameraOrientation.transform.rotation = targetRotation;
        yield return null;
    }
    
    //fix animation controller when necessary
    void SetSingleAnimation(string stateName)
    {
        foreach (string state in animationBools)
        {
            playerAni.SetBool(state, false);
        }
        playerAni.SetBool(stateName, true);
    }
    #endregion

    //Recurring events that expect changes
    #region Status checks
    //check aerial states
    void CheckGrounded()
    {
        RaycastHit hit;
        float rayLength = 1.1f;
        Debug.DrawRay(transform.position, gravitationalDirection * rayLength, Color.green);
        //when ground is found
        if (Physics.Raycast(transform.position, gravitationalDirection, out hit, rayLength))
        {
            //apply landing functions if player should be diving
            if (diving)
            {
                Debug.Log("Landed");
                diving = false;
                SetSingleAnimation("Grounded");
                StartCoroutine(LandPlayer(hit,.5f, shiftDiving));
            }
            else if (!diving && transform.up != -gravitationalDirection)
            {
                StartCoroutine(LandPlayer(hit,.5f, shiftDiving));
            }
            rb.drag = groundedDrag;
            grounded = true;
        }
        //tick grounded off
        else
        {
            grounded = false;
            if (transform.up != -gravitationalDirection)
            {
                Debug.Log("Attempt to correct player!");
            }
            rb.drag = 0f;
        }
    }
    void CheckDive()
    {
        RaycastHit hit;
        Debug.DrawRay(transform.position, gravitationalDirection * diveLength, Color.red);
        if (!Physics.Raycast(transform.position, gravitationalDirection, out hit, diveLength))
        {
            diving = true;
            Debug.Log("Triggering dive");
            playerAni.SetBool("Diving", true);
            rb.drag = freeFallDrag;
        }
        else
        {
            diving = false;
        }
    }
    
    //check corresponding animation states
    void AnimationStates()
    {
        //check movement state
        if (moveInput.magnitude > 0 && rb.velocity.magnitude > 0 && grounded)
        {
            playerAni.SetBool("Movement", true);
        }
        else if (moveInput.magnitude > 0 && grounded)
        {
            playerAni.SetBool("Movement", false);
        }
        //check grounded state
        //if there's no movement then state will revert to grounded by default on animation behaviour
        if (grounded)
        {
            playerAni.SetBool("Grounded", grounded);
        }
    }
    #endregion
    
    //All events called by the input system
    #region Input events
    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }
    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            if (grounded)
            {
                rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            }
        }
    }
    public void RevertGravity(InputAction.CallbackContext context)
    {
        //do only once
        if (context.started)
        {
            if (!shifted)
            {
                return;
            }
        
            //configure states to correct player status no matter what scenario
            rb.freezeRotation = true;
            myGravity.force = Vector3.down * gravityForce;
            zerograv = false;
            immobile = false;
            shifted = false;
            shiftDiving = false;
            playerAni.SetBool("Zero Grav", false);
        
            //distinguish a way to recalculate orientation when falling?
            rb.drag = freeFallDrag;
        }
    }
    public void ToggleGravity(InputAction.CallbackContext context)
    {
        //do only once
        if (context.started)
        {
            shifted = true;
            if (!zerograv)
            {
                //disable gravitational forces
                myGravity.force *= 0f;
                
                //immobilise player
                immobile = true;
                zerograv = true;
                grounded = false;
                diving = false;
                shiftDiving = false;
                rb.freezeRotation = false;
                rb.drag = zeroGravDrag;
                Debug.Log("Missing function!");
                //SetSingleAnimation("Zero Grav");
                //fix camera with coroutine
            }
            else
            {
                //calculate new gravity
                Vector3 cameraDirection = myCamera.transform.forward;
                gravitationalDirection = cameraDirection;
                Vector3 newGravity = cameraDirection.normalized * gravityForce;
                myGravity.force = newGravity;
                
                //unlock player
                immobile = false;
                zerograv = false;
                shiftDiving = true;
                rb.freezeRotation = true;
                rb.drag = freeFallDrag;
                playerAni.SetBool("Zero Grav", false);
            }
        }
    }
    public void AerialRotate(InputAction.CallbackContext context)
    {
        rotateInput = context.ReadValue<float>();
    }
    #endregion
}
