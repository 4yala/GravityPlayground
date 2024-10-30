using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class NewPlayerController : MonoBehaviour
{
    #region Variables
    [Header ("Player components")]
    [SerializeField] Rigidbody rb;
    [SerializeField] Animator playerAni;
    [SerializeField] ConstantForce myGravity; 
    [SerializeField] Camera myCamera;
    [SerializeField] GameObject myCameraOrientation;
    [SerializeField] CinemachineFreeLook myCameraCm;

    [Header("Movement Variables")]
    [SerializeField] float gravityForce;
    [SerializeField] float moveSpeed;
    [SerializeField] float maxSpeedWalk;
    [SerializeField] float jumpForwardSpeed;
    [SerializeField] float airDiveSpeed;
    [SerializeField] float maxDiveSpeed;
    [SerializeField] float diveAcceleration;
    [SerializeField] float deceleration;
    [SerializeField] float rotationSpeed;
    [SerializeField] float cameraRotationSpeed;
    [SerializeField] float jumpForce;
    [SerializeField] float groundedDrag;
    [SerializeField] float zeroGravDrag;
    [SerializeField] float diveRotationSpeed;
    
    
    [Header("Debug elements to inspect")]
    [SerializeField] Vector2 moveInput;
    [SerializeField] Vector3 gravitationalDirection;
    [SerializeField] Vector3 loggedGravitationalDirection;
    [SerializeField] Transform loggedCameraTransform;
    [SerializeField] float rotateInput;
     
    

    //[Header("Assets required")]


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
        myCamera = FindFirstObjectByType<Camera>();
        
        gravitationalDirection = -transform.up;
    }

    // Update is called once per frame
    void Update()
    {
        AnimationStates();
        if (!zerograv)
        {
            if (shiftDiving)
            {
                SmoothLanding();
            }
            IsGrounded();
            if (!grounded && !diving)
            {
                HighGround();
            }

        }
        //otherchecks!
        if(myCameraCm.m_YAxisRecentering.m_enabled && myCameraCm.m_YAxis.Value == 0.5f)
        {
            myCameraCm.m_YAxisRecentering.m_enabled = false;
        }
        Debug.DrawRay(myCameraOrientation.transform.position, myCameraOrientation.transform.up * 5f, Color.green);

    }

    // FixedUpdate for non frame dependent functions, I.e. physics
    void FixedUpdate()
    {
        if(!immobile && !diving)
        {
            //gather the input information.
            Vector3 inputDirection = new Vector3(moveInput.x, 0f, moveInput.y).normalized;

            //only if there's an input.
            if(inputDirection.magnitude > 0.1f)
            {
                //Calculate a new vector reflective of where the character's upwards direction MUST face
                Vector3 cameraForward = Vector3.ProjectOnPlane(myCamera.transform.forward, myCameraOrientation.transform.up);
                Vector3 cameraRight = Vector3.ProjectOnPlane(myCamera.transform.right, myCameraOrientation.transform.up);

                //translate it with inputs
                Vector3 targetDirection = (cameraForward * moveInput.y + cameraRight * moveInput.x).normalized;

                if (targetDirection.magnitude > 0.1f)
                {
                    //as the targetDirection has a declared up direction, it cannot travel in that direction as only forward and right is used.
                    Quaternion targetRotation = Quaternion.LookRotation(targetDirection,myCameraOrientation.transform.up);
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
                }
                //more to consider in the future! such as damping the Y value on speed. for now This is just a base.

                //end with moving forward from character perspective
                if (grounded)
                {
                    rb.AddForce(transform.forward * moveSpeed, ForceMode.Acceleration);
                }
                else
                {
                    rb.AddForce(transform.forward * jumpForwardSpeed, ForceMode.Acceleration);
                }
                if (rb.velocity.magnitude > maxSpeedWalk)
                {
                    rb.velocity = rb.velocity.normalized * maxSpeedWalk;
                }
            }
            else
            {

                //break the player when movement stops, only if it's touching the floor
                if(grounded)
                {
                    rb.AddForce(rb.velocity * -deceleration, ForceMode.Force);
                }
            }
        }
        else if (!immobile && diving)
        {
            if (transform.up == gravitationalDirection)
            {
                Debug.Log("Peak dive");
                
                if (!cameraTransitioned)
                {
                    CameraOrientationFix();
                }
                if (rotateInput != 0!)
                {
                    float rotationAmount = diveRotationSpeed * rotateInput * Time.deltaTime;
                    transform.Rotate(0, rotationAmount, 0);
                    cameraTransitioned = false;
                }
                //movement
                Vector3 targetMovement = -transform.forward * moveInput.y + transform.right * moveInput.x;

                if(targetMovement.magnitude > 0.1f)
                {
                    rb.AddForce(targetMovement * airDiveSpeed, ForceMode.Acceleration);
                    myCameraCm.m_YAxisRecentering.m_enabled = true;
                }
                else
                {
                    myCameraCm.m_YAxisRecentering.m_enabled = false;
                }
            }
            if(rb.velocity.magnitude < maxDiveSpeed)
            {
                //rb.AddForce(gravitationalDirection * diveAcceleration, ForceMode.Acceleration);
                //Debug.Log("Max Speed reached");
            }
            Vector3 gravNormalized = gravitationalDirection.normalized;
            Quaternion targetRotation = Quaternion.FromToRotation(transform.up, gravNormalized) * transform.rotation;
            transform.rotation = Quaternion.RotateTowards(transform.rotation,targetRotation,  50f* Time.fixedDeltaTime);
            

            

        }
    }
    //Here go unique events that are triggered sometimes
    #region ReactionEvents
    /*
    void OrientatePlayer(Vector3 gravityDirection)
    {
        Debug.Log("Updating");
        Quaternion targetRotation = Quaternion.FromToRotation(transform.up, -gravityDirection) * transform.rotation;
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

    }
    */
    void CameraOrientationFix()
    {
        loggedCameraTransform = myCamera.transform;
        myCameraCm.enabled = false;
        myCameraOrientation.transform.rotation = Quaternion.LookRotation(transform.up, -transform.forward);
        myCamera.transform.position = loggedCameraTransform.position;
        myCamera.transform.rotation = loggedCameraTransform.rotation;
        myCameraCm.enabled = true;
        myCameraCm.m_YAxisRecentering.m_enabled = true;
        cameraTransitioned = true;
        
    }
    void SmoothLanding()
    {
        RaycastHit hit;
        float rayLength = 1.1f;
        Debug.DrawRay(transform.position, /*transform.TransformDirection(Vector3.down)*/gravitationalDirection * rayLength, Color.blue);
        if (Physics.Raycast(transform.position,/*-transform.up */gravitationalDirection, out hit, rayLength))
        {
            shiftDiving = false;
            StartCoroutine(Landing(hit, .5f, true));
            // //character snapping to surface
            // Quaternion targetRotation = Quaternion.FromToRotation(transform.up, hit.normal) * transform.rotation;
            // transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            //
            // //gravity adjusting to surface
            // Vector3 newGravity = -transform.up.normalized * gravityForce;
            // myGravity.force = newGravity;
            //
            // //FIX LATER VV
            // gravitationalDirection = -transform.up;
            // //FIX LATER ^^
            //
            // //camera orientation
            // SmoothCamera();
            // grounded = true;
            // shiftDiving = false;
            // Debug.Log("fixed orientation");
        }
    }

    IEnumerator Landing(RaycastHit hit, float duration,bool smoothGravity)
    {
        float elapsedTime = 0f;
        Quaternion targetRotation = Quaternion.FromToRotation(transform.up, hit.normal) * transform.rotation;
        Quaternion startRotation = transform.rotation;
        

        while (elapsedTime < duration)
        {
            float t = Mathf.Clamp01(elapsedTime / duration);
            
            transform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);
            Debug.Log($"Transform Rotation: {transform.rotation}");

            elapsedTime += Time.deltaTime;
            
            yield return null;

        }
        transform.rotation = targetRotation;
        grounded = true;
        Debug.Log("starting cameralerp");
        StartCoroutine((CameraLerp(0.5f)));
        if (smoothGravity)
        {
            Vector3 newGravity = -transform.up.normalized * gravityForce;
            myGravity.force = newGravity;
        }
        //gravity adjusting to surface

        //
        // //FIX LATER VV
        // gravitationalDirection = -transform.up;
        // //FIX LATER ^^
        //
        //camera orientation
        //SmoothCamera();


        yield return null;
    }
    
    IEnumerator CameraLerp(float duration)
    {
        
        Debug.Log("camera lerp started");
        float elapsedTime = 0f;
        Quaternion startRotation = myCameraOrientation.transform.rotation;
        Quaternion targetCameraRotation = Quaternion.FromToRotation(myCameraOrientation.transform.up, transform.up) *
                                          myCameraOrientation.transform.rotation;


        while (elapsedTime < duration)
        {
            float t = Mathf.Clamp01(elapsedTime / duration);

            myCameraOrientation.transform.rotation = Quaternion.Slerp(startRotation, targetCameraRotation, t);
            Debug.Log($"Camera rotation: {myCameraOrientation.transform.localRotation}");

            elapsedTime += Time.deltaTime;
            Debug.Log(elapsedTime);
            yield return null;

        }
        
        Debug.Log(("ending camera lerp"));

        myCameraOrientation.transform.rotation = targetCameraRotation;
        myCameraCm.m_YAxisRecentering.m_enabled = true;
        myCameraCm.m_YAxisRecentering.RecenterNow();
        cameraTransitioned = false;
        yield return null;
    }
    
    void SmoothCamera()
    {
        //camera orientation
        Quaternion targetRotation = Quaternion.FromToRotation(myCameraOrientation.transform.up, transform.up) * myCameraOrientation.transform.rotation;
        myCameraOrientation.transform.rotation = Quaternion.Slerp(myCameraOrientation.transform.rotation, targetRotation, cameraRotationSpeed * Time.deltaTime);
        // myCameraCM.m_YAxisRecentering.m_enabled = true;
        // myCameraCM.m_YAxisRecentering.RecenterNow();
    }
    #endregion
    //Here goes recurring events that go in update
    #region StatusChecks
    void IsGrounded()
    {
        RaycastHit hit;
        float rayLength = 1.1f; // Adjust based on your character's size
        Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.down) * rayLength, Color.green);
        if (Physics.Raycast(transform.position, gravitationalDirection,/* -transform.up,*/ out hit, rayLength))
        {
            if (diving)
            {
                Debug.Log("Landed");
                diving = false;
                SetSingleAnimation("Grounded");
                StartCoroutine(Landing(hit, .5f, true));
            }
            rb.drag = groundedDrag;
            grounded = true;
        }
        else
        {
            grounded = false;
            if (!diving)
            {
                rb.drag = 0f;
            }
        }
        
    }
    void HighGround()
    {
        RaycastHit hit;
        float rayLength = 15f; // Adjust based on your character's size
        Debug.DrawRay(transform.position, gravitationalDirection * rayLength, Color.red);
        if (!Physics.Raycast(transform.position, gravitationalDirection /* -transform.up*/, out hit, rayLength))
        {
            diving = true;
            Debug.Log("Flying");
            playerAni.SetBool("Diving", true);
            rb.drag = 0f;
        }
        else
        {
            playerAni.SetBool("Diving", false);
            diving = false;
        }
    }
    void AnimationStates()
    {
        if (moveInput.magnitude > 0 && rb.velocity.magnitude > 0 && grounded)
        {
            playerAni.SetBool("Movement", true);
        }
        else if (moveInput.magnitude == 0 && grounded)
        {
            playerAni.SetBool("Movement", false);
        }
        playerAni.SetBool("Grounded", grounded);    

    }
    void SetSingleAnimation(string stateName)
    {
        foreach (string state in animationBools)
        {
            playerAni.SetBool(state, false);
        }
        playerAni.SetBool(stateName, true);
    }
    #endregion
    //Here goes any events called by the input system
    #region InputEvents
    public void ToggleGrav(InputAction.CallbackContext context)
    {
        if(context.started)
        {
            
            shifted = true;
            if (!zerograv)
            {
                myGravity.force = new Vector3(0, 0, 0);
                zerograv = true;
                immobile = true;
                grounded = false;
                diving = false;
                shiftDiving = false;
                rb.freezeRotation = false;
                rb.drag = zeroGravDrag;
                SetSingleAnimation("Zero Grav");
            }
            else
            {
                Vector3 cameraDirection = myCamera.transform.forward;
                //OrientatePlayer(cameraDirection);
                gravitationalDirection = myCamera.transform.forward;
                Vector3 newGravity = cameraDirection.normalized * gravityForce;
                myGravity.force = newGravity;
                zerograv = false;
                immobile = false;
                shiftDiving = true;
                rb.freezeRotation = true;
                rb.drag = 0f;
                playerAni.SetBool("Zero Grav", false);
            }
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
            gravitationalDirection = -transform.up;
            shifted = false;
            shiftDiving = false;
            myCameraOrientation.transform.rotation = Quaternion.Euler(0, 0, 0);
            playerAni.SetBool("Zero Grav", false);
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
            rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);
            
        }
    }

    public void AerialRotate(InputAction.CallbackContext context)
    {
        rotateInput = context.ReadValue<float>();
    }
    #endregion
}
