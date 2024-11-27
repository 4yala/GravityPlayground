using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
using Unity.VisualScripting;
using UnityEngine.Events;
using UnityEngine.InputSystem;
[RequireComponent(typeof(CustomGravity))]
public class PlayerControllerDebug : MonoBehaviour
{
    #region Variables
    [Header ("Player components")]
    [Tooltip("Player physical body (Automated)")]
    [SerializeField] Rigidbody rb;
    [Tooltip("Character animator (Automated)")]
    [SerializeField] Animator playerAni;
    [Tooltip("Animation states (set up accordingly with animator")]
    [SerializeField] List<string> animationBools;
    [Tooltip("Custom gravity script (Automated)")]
    [SerializeField] CustomGravity gravity;
    [Tooltip("Field which interacts with objects (Should be externally placed in scene and referenced)")]
    [SerializeField] GravityField gravityField;
    
    [Header(("Camera components \n" +
             "(Should be externally placed in scene and referenced)"))]
    [Tooltip("The physical camera used to look at the player")]
    [SerializeField] public Camera myCamera;
    [Tooltip("The camera's camera's matching perspective")]
    [SerializeField] GameObject myCameraOrientation;
    [Tooltip("The camera's Cinemachine component (FreeLook only)")]
    [SerializeField] public CinemachineFreeLook myCameraCm;
    [Tooltip("The camera's Cinemachine component (Virtual only)")]
    [SerializeField] public CinemachineVirtualCamera freeCameraCm;
    [Tooltip("Camera target (no rotation)")]
    [SerializeField] Transform cameraTarget;
    [SerializeField] Transform cameraTarget2;

    [Header("Camera variables")]
    [Tooltip("Camera's default distance from the player")]
    [SerializeField] public float defaultDistance = 4f;
    [Tooltip("Camera's zoomed-in distance from the player")]
    [SerializeField] public float aimedDistance = 3f;

    [Header("Grounded Movement Variables")]
    [Tooltip("Grounded acceleration speed (gradual force)")]
    [SerializeField] float moveSpeed = 8f;
    [Tooltip("Maximum grounded speed")]
    [SerializeField] float maxSpeedWalk = 10f;
    [Tooltip("Lesser acceleration speed when aimed (gradual force)")]
    [Range(6.5f ,10)]
    [SerializeField] float moveSpeedAimed = 7f; //6.5 for some reason is the lowest possible before it stops moving completely
    [Tooltip("Maximum slowed speed for aimed in state")] 
    [SerializeField] float maxSpeedAimed = 3f;
    [Tooltip("Minimum angle for slope recognition")]
    [SerializeField] float minSlopeAngle = 10f;
    [Tooltip("Maximum acceptable angle for slope")]
    [SerializeField] float maxSlopeAngle = 45f;
    [Tooltip("The braking speed/drag for the player when no input is detected (gradual force)")]
    [SerializeField] float deceleration = 20f;
    [Tooltip("The braking speed/drag for the player when changing directions (gradual force)")]
    [SerializeField] float groundedDrag = 8f;
    [Tooltip("Character rotation speed for matching input direction (generally wanted high)")]
    [SerializeField] float rotationSpeed = 1000f;
    [Tooltip("Jump force (instant force)")]
    [SerializeField] float jumpForce = 5f;
    [Tooltip("Drag for braking movement when floating")]
    [SerializeField] float zeroGravDrag = 3;
    
    [Header("Aerial Movement Variables ")]
    [Tooltip("Aerial acceleration speed when relatively close to ground (gradual force)")]
    [SerializeField] float jumpForwardSpeed;
    [Tooltip("Lateral acceleration speed when diving (gradual force)")]
    [SerializeField] float lateralAirDiveSpeed;
    [Tooltip("Max lateral speed when diving")]
    [SerializeField] float maxLateralSpeed;
    [Tooltip("Air dive acceleration speed (gradual force)")]
    [SerializeField] float diveAcceleration;
    [Tooltip("Terminal velocity for air dive")]
    [SerializeField] float maxDiveSpeed;
    [Tooltip("Character rotation speed when diving")]
    [SerializeField] float diveRotationSpeed;
    [Tooltip("Required object distance until character dives (world unit)")]
    [SerializeField] float diveLength;
    [Tooltip("Required wait time until character dives (seconds)")]
    [SerializeField] float diveWaitTime;
    
    [Header("Cached values (Only for visualising)")]
    [SerializeField] Vector2 moveInput;
    [SerializeField] Vector2 cameraInput;
    [SerializeField] float rotateInput;
    [SerializeField] Vector3 cameraRight;
    [Tooltip("Checker that prevents character orientation fixing from overloading")]
    [SerializeField] bool landIntialised;

    [SerializeField] bool landOnce;
    
    [Header("Player States (Only for visualising)")]
    [SerializeField] public bool zerograv;
    [SerializeField] bool shifted;
    [SerializeField] bool immobile;
    [SerializeField] public bool grounded;
    [SerializeField] bool diving;
    [SerializeField] bool noGroundDetected;
    [SerializeField] bool shiftDiving; //to be used only when toggling out from zero gravity, in order to smooth landing.
    [SerializeField] bool cameraTransitioned;
    [SerializeField] bool fieldEnabled;
    [SerializeField] public bool aimedDownSights;
    #endregion
    
    // Start is called before the first frame update
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        //declare variables
        rb = GetComponent<Rigidbody>();
        gravity = GetComponent<CustomGravity>();
        playerAni = GetComponent<Animator>();
        
        //customise components for initial behaviour
        //myCameraCm.LookAt = transform;
        //myCameraCm.Follow = transform;
        gravityField.gameObject.SetActive(fieldEnabled);
        gravityField.owner = gameObject.GetComponent<PlayerControllerDebug>();
        myCameraCm.m_Orbits[1].m_Radius = defaultDistance;
    }

    // Update is called once per frame
    void Update()
    {
        cameraTarget.position = gameObject.transform.position;
        cameraTarget2.position = gameObject.transform.position;
        Debug.DrawRay(cameraTarget2.position, cameraTarget2.forward,Color.yellow);
        Debug.DrawRay(cameraTarget.position,cameraTarget.forward,Color.cyan);
        AnimationStates();
        //SmoothTranstitions();
        if (!zerograv)
        {
            //check grounded
            //CheckGrounded();
            CheckGroundedNew();
            //check if should begin diving, might need exceptions as a similar behaviour to dive should be attempted when gravity changes in low heights, etc
            if (!grounded && !diving)
            {
                CheckDive();
            }
        }
    }
    
    // Recurring but not frame dependent functions
    void FixedUpdate()
    {
        if (immobile)
        {
            return;
        }
        //relatively grounded movement (including jumping motion)
        if (!diving)
        {
            if (!aimedDownSights)
            {
                //remove later
                myCameraCm.m_YAxisRecentering.m_enabled = false;
            
                //gather input information
                Vector3 inputDirection = new Vector3(moveInput.x, 0, moveInput.y).normalized;
            
                //execute when input exists
                if (inputDirection.magnitude > 0.1f)
                {
                    //calculate directions based on definition of up by the perspective of the camera
                    Vector3 cameraForward = Vector3.ProjectOnPlane(myCamera.transform.forward, myCameraOrientation.transform.up);
                    Vector3 cameraRight = Vector3.ProjectOnPlane(myCamera.transform.right, myCameraOrientation.transform.up);
                
                    //translate a character direction based on input
                    //flat direction
                    Vector3 targetDirection = (cameraForward * moveInput.y  + cameraRight * moveInput.x).normalized;
                    
                    //an external local value stored for slopes
                    Vector3 finalDirection = targetDirection;
                    
                    // slope detection
                    RaycastHit hit;
                    if (Physics.Raycast(transform.position, -transform.up, out hit, 1.1f))
                    {
                        Vector3  slopeNormal = hit.normal;
                        float slopeAngle = Vector3.Angle(slopeNormal, myCameraOrientation.transform.up);
                        bool upwardSlope = Vector3.Dot(transform.forward, slopeNormal) < 0;
                        
                        //check it's only going up
                        /*
                        if (upwardSlope)
                        {
                            // angle range checker
                            if (slopeAngle > minSlopeAngle && slopeAngle <= maxSlopeAngle)
                            {
                                finalDirection = Vector3.ProjectOnPlane(targetDirection, slopeNormal).normalized;
                                Debug.Log("slope detected");
                            }
                            else if (slopeAngle > maxSlopeAngle)
                            {
                                finalDirection = Vector3.zero;
                                Debug.Log("too high");
                            }
                        }
                        */
                        if (slopeAngle > minSlopeAngle && slopeAngle <= maxSlopeAngle)
                        {
                            finalDirection = Vector3.ProjectOnPlane(targetDirection, slopeNormal).normalized;
                            Debug.Log("slope detected");
                        }
                        else if (slopeAngle > maxSlopeAngle && upwardSlope)
                        {
                            finalDirection = Vector3.zero;
                            Debug.Log("too high");
                        }
                        if (upwardSlope)
                        {
                            // angle range checker

                        }
                    }
                    
                    //calculate and make the player face the direction if the slope allows it to
                    if (finalDirection != Vector3.zero)
                    {
                        
                        Quaternion targetRotation = Quaternion.LookRotation(targetDirection, myCameraOrientation.transform.up);
                        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
                    }
                    
                    //make sure the character faces the direction that it's moving towards
                    float directionAligntment = Vector3.Dot(transform.forward, targetDirection);
                    bool aligned = directionAligntment >= 0.9f;
                    
                    //move off if character matches direction
                    if (aligned)
                    {
                        //finalise with character forward movement only, no strafing
                        if (grounded)
                        {
                            rb.AddForce(finalDirection * moveSpeed, ForceMode.Acceleration);
                        }
                        else
                        {
                            rb.AddForce(transform.forward * jumpForwardSpeed, ForceMode.Acceleration);
                        }
                    }
                    //calculate velocity 
                    Vector3 forwardVelocity = Vector3.Project(rb.velocity, transform.forward);
                    Vector3 nonForwardVelocity = rb.velocity - forwardVelocity;

                    Vector3 nonForwardDragForce = -nonForwardVelocity * groundedDrag; //custom drag
                    
                    //add opposite forces to player
                    if (grounded)
                    {
                        rb.AddForce(nonForwardDragForce, ForceMode.Acceleration);
                    }

                }
                //finally, if no input is made brake the player if it's grounded
                else
                {
                    if (grounded)
                    {
                        rb.AddForce(rb.velocity * -deceleration, ForceMode.Force);
                    }
                }
                // prevent from building infinite speed
                if (rb.velocity.magnitude > maxSpeedWalk && grounded)
                {
                    Debug.Log("Max walk speed reached");
                    rb.velocity = rb.velocity.normalized * maxSpeedWalk;
                }
            }
            else if(grounded && aimedDownSights)
            {
                //match the camera's forward direction on a horizontal plane
                Vector3 flattenedForward = Vector3.ProjectOnPlane(myCamera.transform.forward, transform.up);
                Quaternion targetRotation = Quaternion.LookRotation(flattenedForward, transform.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
                
                //movement in all directions (allow strafing)
                if (moveInput.magnitude > 0.1f)
                {
                    //Debug.Log("moving");
                    Vector3 targetMovement = transform.forward * moveInput.y + transform.right * moveInput.x;
                    Debug.Log("executing speed " + targetMovement * moveSpeedAimed);
                    rb.AddForce(targetMovement * moveSpeedAimed, ForceMode.Acceleration);
                    
                    //calculate velocity 
                    Vector3 forwardVelocity = Vector3.Project(rb.velocity, targetMovement);
                    Vector3 nonForwardVelocity = rb.velocity - forwardVelocity;

                    Vector3 nonForwardDragForce = -nonForwardVelocity * 3f; //custom drag
                    
                    //add opposite forces to player
                    rb.AddForce(nonForwardDragForce, ForceMode.Acceleration);
                }
                
                //add braking to the movement
                else if (grounded)
                {
                    rb.AddForce(rb.velocity * -deceleration, ForceMode.Force);
                }
                // prevent from building infinite speed
                if (rb.velocity.magnitude > maxSpeedAimed && grounded)
                {
                    Debug.Log("Max speed reached");
                    rb.velocity = rb.velocity.normalized * maxSpeedAimed;
                }
            }
        }
        //aerial movement (diving in the air)
        else
        {
            Debug.Log(transform.up);
            Debug.Log(gravity.gravitationalDirection);
            //movement is enabled when character is in proper position (head first)
            if (transform.up == gravity.gravitationalDirection)
            {
                //check if there's active input for character rotation
                if (rotateInput != 0)
                {
                    float rotationAmount = diveRotationSpeed * rotateInput * Time.deltaTime;
                    transform.Rotate(0, rotationAmount, 0);
                }
                
                //calculate movement
                Vector3 targetMovement = -transform.forward * moveInput.y + transform.right * moveInput.x;
                //drag must affect these directions
                Vector3 lateralVelocity = rb.velocity - Vector3.Project(rb.velocity, gravity.gravitationalDirection);
                if (targetMovement.magnitude > 0.1f)
                {
                    rb.AddForce(targetMovement * lateralAirDiveSpeed, ForceMode.Acceleration);

                    Vector3 inputDirection = -transform.forward * moveInput.y + transform.right * moveInput.x;
                    inputDirection = inputDirection.normalized;
                    
                    //brake the player if it doesn't match the input direction
                    float alignment = Vector3.Dot(lateralVelocity.normalized, inputDirection);
                    if(Math.Abs(alignment) < 0.9f)
                    {
                        rb.AddForce(-lateralVelocity.normalized * 3f, ForceMode.Acceleration);
                        Debug.Log("Braking the player");
                    }
                }
                
                //decelerate the player when no input
                else
                {
                    rb.AddForce(-lateralVelocity*3f, ForceMode.Acceleration);
                }
                //calculate the velocity along the lateral plane from the gravitational direction
                float lateralSpeed =  lateralVelocity.magnitude;
                if (lateralSpeed > maxLateralSpeed)
                {
                    //calculate a max lateral speed
                    Vector3 clampedLateralSpeed = lateralVelocity.normalized * maxLateralSpeed;
                    //grab the current velocity and just add the lateral speed as appropriate
                    rb.velocity = Vector3.Project(rb.velocity, gravity.gravitationalDirection) + clampedLateralSpeed;
                    Debug.Log("Maxing out lateral speed");  
                }
                

            }
            Quaternion targetRotation = Quaternion.FromToRotation(transform.up, gravity.gravitationalDirection) * transform.rotation;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, 50f * Time.fixedDeltaTime);
            
            //calculate velocity along the gravitational direction
            Vector3 fallingSpeed = Vector3.Project(rb.velocity, gravity.gravitationalDirection);
            Debug.Log(fallingSpeed);
            //if the falling speed does not exceed the maximum dive speed, increase by acceleration
            if (fallingSpeed.magnitude < maxDiveSpeed)
            {
                rb.AddForce(gravity.gravitationalDirection * diveAcceleration, ForceMode.Acceleration);
                Debug.Log("Increasing dive speed");
            }
                
            if (fallingSpeed.magnitude > maxDiveSpeed)
            {
                //grab current velocity
                Vector3 currentVelocity = rb.velocity;
                //calculate velocity proportionate to falling direction
                Vector3 velocityAlongFall = Vector3.Project(currentVelocity, gravity.gravitationalDirection);
                //hard set velocity to maximum falling speed + current velocity STRIPPED of the falling velocity, leaving any other forces (for example lateral velocity)
                rb.velocity = Vector3.Project(rb.velocity, gravity.gravitationalDirection).normalized * maxDiveSpeed + (currentVelocity - velocityAlongFall);
                Debug.Log("Terminal velocity reached");
            }
        }   
    }
    
    //Events that are called upon to make changes
    #region Reaction events
    //flips the player upright when landing after a free fall / dive / gravity shift
    IEnumerator LandPlayer(RaycastHit hit, float duration, bool smoothGravity)
    {
        landIntialised = true;
        float elapsedTime = 0;
        //set up to calculate the way the character will flip
        Quaternion targetRotation = Quaternion.identity;
        
        //if gravity smoothing is on, revert to normality
        if (Vector3.Dot(gravity.gravitationalDirection.normalized, Vector3.down) > .9f && smoothGravity)
        {
            gravity.SoftSetGravity(Vector3.down);
            cameraTarget.rotation = Quaternion.FromToRotation(transform.up, Vector3.up) * transform.rotation;
            shifted = false;
        }
        //smooth with surface
        else if (smoothGravity)
        {
            targetRotation = Quaternion.FromToRotation(transform.up, hit.normal) * transform.rotation;
            gravity.SoftSetGravity(-hit.normal);
        }
        //retain current direction
        else if(!smoothGravity)
        {
            targetRotation = Quaternion.FromToRotation(transform.up, -gravity.gravitationalDirection) * transform.rotation;
        }
        Quaternion startRotation = transform.rotation;
        
        // start slerp based on desired length
        while (elapsedTime < duration)
        {
            float t = Mathf.Clamp01(elapsedTime / duration);
            //transform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        gravity.rb.MoveRotation(targetRotation);
        //double checkers
        transform.rotation = targetRotation;
        //set status
        grounded = true;
        if (smoothGravity)
        {
            gravity.SetNewGravity(gravity.gravitationalDirection, true,0f);
            shiftDiving = false;
        }
        //now move on to do camera
        myCameraCm.m_Orbits[0].m_Radius = 1f;
        myCameraCm.m_Orbits[2].m_Radius = 1f;
        //cameraTarget.rotation = myCamera.transform.rotation;
        //myCameraOrientation.transform.up = transform.up;
        myCameraOrientation.transform.up = transform.up;
        LockCamera(true);
        //StartCoroutine(OrientateCamera(0.5f, transform.up, true));
        landIntialised = false;
        landOnce = true;
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

    IEnumerator TransitionCamera(float targetValue)
    {
        float duration = 1f;
        float elapsedTime = 0;
        float startPosition = myCameraCm.m_YAxis.Value;
        while (elapsedTime < duration)
        {
            float t = Mathf.Clamp01(elapsedTime / duration);
            myCameraCm.m_YAxis.Value = Mathf.Lerp(startPosition, targetValue, t);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        yield return null;
    }
    IEnumerator QueueDive()
    {
        noGroundDetected = true;
        yield return new WaitForSeconds(diveWaitTime);
        if (noGroundDetected)
        {
            diving = true;
            playerAni.SetBool("Diving", true);
            myCameraCm.m_Orbits[0].m_Radius = .1f;
            myCameraCm.m_Orbits[2].m_Radius = .1f;
            Debug.Log("Count down finished and diving");
        }
        LockCamera(false);
    }
    
    void LockCamera(bool locked)
    {
        if (locked)
        {
            SyncFreeLookCamera();
        }
        SmoothTransitions();
        myCameraCm.gameObject.SetActive(locked);
        freeCameraCm.gameObject.SetActive(!locked);
        
    }

    void SmoothTransitions()
    {
        if (myCameraCm.gameObject.activeInHierarchy)
        {
            cameraTarget2.rotation = myCamera.transform.rotation;
        }
        if (!myCameraCm.gameObject.activeInHierarchy)
        {
            //cameraTarget.rotation = myCamera.transform.rotation;
        }
    }

    void SyncFreeLookCamera()
    {
        cameraTarget2.transform.rotation = Quaternion.LookRotation(cameraTarget2.transform.forward, transform.up);
        //Debug.Break();
        float newYAxis = Vector3.Dot(gravity.gravitationalDirection, myCamera.transform.forward);
        newYAxis = (newYAxis - -1) / (1 - -1) * (1 - 0) + 0;
        Debug.Log(newYAxis);
        //StartCoroutine(TransitionCamera(newYAxis));
        //myCameraCm.m_YAxis.Value = newYAxis;
        //get direction from camera to target
        Vector3 direction = myCamera.transform.position - cameraTarget.position;
        
        //calculate x axis
        float xAxis = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
        //myCameraCm.m_XAxis.Value = xAxis;
        Debug.Log(xAxis);
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
        Debug.DrawRay(transform.position, gravity.gravitationalDirection * rayLength, Color.green);
        //when ground is found
        if (Physics.Raycast(transform.position, gravity.gravitationalDirection, out hit, rayLength))
        {
            //apply landing functions if player should be diving
            if (diving)
            {
                Debug.Log("Landed");
                diving = false;
                SetSingleAnimation("Grounded");
                StartCoroutine(LandPlayer(hit,.5f, shiftDiving));
            }
            else if (!diving && transform.up != -gravity.gravitationalDirection && !grounded)
            {
                StartCoroutine(LandPlayer(hit,.5f, shiftDiving));
                
            } 
            grounded = true;
        }
        //tick grounded off
        else
        {
            grounded = false;
            if (transform.up != -gravity.gravitationalDirection && !diving)
            {
                Debug.Log("Attempt to correct player!");
            }
            rb.drag = 0f;
        }
    }
    void CheckGroundedNew()
    {
        RaycastHit hit  = new RaycastHit();
        //default length
        float raylength = 1.1f;
        const float offsets = .5f;
        Vector3[] origins =
        {
            transform.position,
            transform.position + transform.right * offsets,
            transform.position - transform.right * offsets,
            transform.position + transform.forward * offsets,
            transform.position - transform.forward * offsets
        };
        bool result = false;
        foreach (Vector3 ray in origins)
        {
            Debug.DrawRay(ray, gravity.gravitationalDirection, Color.green);
            if (Physics.Raycast(ray, gravity.gravitationalDirection, out hit, raylength))
            {
                if (hit.collider.gameObject.tag == "Player" || hit.collider.gameObject.GetComponent<InteractableObject>() != null)  
                {
                    continue;
                }
                if (diving)
                {
                    Debug.Log("Landed");
                    diving = false;
                    SetSingleAnimation("Grounded");
                    StartCoroutine(LandPlayer(hit,.5f, shiftDiving));
                }
                else if (!diving && transform.up != -gravity.gravitationalDirection && !grounded)
                {
                    Debug.Log("correcting player");
                    StartCoroutine(LandPlayer(hit,.5f, shiftDiving));
                }
                else if (transform.up != -gravity.gravitationalDirection && !landIntialised && !landOnce)
                {
                    Debug.Log("something is not right");
                    StartCoroutine(LandPlayer(hit,.5f, shiftDiving));
                }
                result = true;
                noGroundDetected = false;
                //Debug.Log($"Ray hit object: {hit.collider.name} at {hit.point} with layer: {LayerMask.LayerToName(hit.collider.gameObject.layer)}");
                if (!myCameraCm.gameObject.activeInHierarchy)
                {
                    LockCamera(true);
                }
                break;
            }
        }

        if (!result)
        {
            if (aimedDownSights)
            {
                gravityField.TriggerAim(false);
            }
        }
        grounded = result;
    }
    void CheckDive()
    {
        RaycastHit hit;
        Debug.DrawRay(transform.position, gravity.gravitationalDirection * diveLength, Color.red);
        if (!Physics.Raycast(transform.position, gravity.gravitationalDirection, out hit, diveLength))
        {
            if (!noGroundDetected)
            {
                Debug.Log("Starting dive check");
                if (shiftDiving)
                {
                    diving = true;
                    playerAni.SetBool("Diving", true);
                    myCameraCm.m_Orbits[0].m_Radius = .1f;
                    myCameraCm.m_Orbits[2].m_Radius = .1f;
                    LockCamera(false);
                }
                else
                {
                    StartCoroutine(QueueDive());
                }
            }
        }
        else
        {
            //unnecessary??
            playerAni.SetBool("Diving", false);
            noGroundDetected = false;
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
        else if (moveInput.magnitude == 0 && grounded)
        {
            playerAni.SetBool("Movement", false);
        }
        //check grounded state
        //if there's no movement then state will revert to grounded by default on animation behaviour
        playerAni.SetBool("Grounded", grounded);
    }
    #endregion
    
    //All events called by the input system
    #region Input events
    //General movement
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
                rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);
                Debug.Log("jumping");
            }
        }

    }
    public void AerialRotate(InputAction.CallbackContext context)
    {
        rotateInput = context.ReadValue<float>();
    }
    public void CameraRotate(InputAction.CallbackContext context)
    {
        if (!myCameraCm.gameObject.activeInHierarchy)
        {
            cameraInput = context.ReadValue<Vector2>();
            cameraTarget2.Rotate( -cameraInput.y* 5f * Time.deltaTime, cameraInput.x *5f * Time.deltaTime, 0f);
        }
    }
    
    //Gravity functions
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
            gravity.RevertGravity(true,0f);
            
            zerograv = false;
            immobile = false;
            shifted = false;
            shiftDiving = false;
            playerAni.SetBool("Zero Grav", false);
            RaycastHit hit;
            if (Physics.Raycast(transform.position, Vector3.down, out hit, 1.1f))
            {
                if (hit.collider.gameObject.tag != "Player" &&
                    hit.collider.gameObject.GetComponent<InteractableObject>() != null)
                {
                    Debug.Log("Fix maybe");
                    StartCoroutine(LandPlayer(new RaycastHit(), .5f, false));
                }
            }
            if (grounded)
            {
                Debug.Log("Fix maybe");
                //StartCoroutine(LandPlayer(new RaycastHit(), .5f, false));   
            }
            //distinguish a way to recalculate orientation when falling?
            landOnce = false;
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
                //use new gravity component
                gravity.SetZeroGravity(zeroGravDrag);
                
                
                //immobilise player
                immobile = true;
                zerograv = true;
                grounded = false;
                diving = false;
                shiftDiving = false;
                noGroundDetected = false;

                SetSingleAnimation("Zero Grav");
                //fix camera with coroutine
                LockCamera(false);
            }
            else
            {
                //calculate new gravity
                Vector3 cameraDirection = myCamera.transform.forward;
                gravity.SetNewGravity(cameraDirection, true, 0f);
                
                //unlock player
                immobile = false;
                zerograv = false;
                shiftDiving = true;
                noGroundDetected = false;
                playerAni.SetBool("Zero Grav", false);
            }
        }
    }
    
    //Object interaction functions
    public void EnableField(InputAction.CallbackContext context)
    {
        //do only once
        if (context.started)
        {
            //two-way switch behaviour
            fieldEnabled = !fieldEnabled;
            //if the switch is off, disable the field
            if (!fieldEnabled)
            {
                gravityField.DisableField();
            }
            //enable and disable accordingly
            gravityField.gameObject.SetActive(fieldEnabled);
        }
    }
    public void AimObject(InputAction.CallbackContext context)
    {
        if (!grounded && !zerograv)
        {
            return;
        }
        //start aiming when initialised 
        //check that there are objects to shoot with
        if (context.started && fieldEnabled && gravityField.objectsInOrbit.Count > 0)
        {
            gravityField.TriggerAim(true);
        }
        
        //cancel when let go of input
        //only cancel if it has been aimed to avoid breakage
        else if (context.canceled && aimedDownSights)
        {
            gravityField.TriggerAim(false);
        }
    }
    public void ScrollObjects(InputAction.CallbackContext context)
    {
        //for some reason delta scroll is read as a vector 2, given that only the y-axis is used a float is preferred
        float inputScroll = context.ReadValue<Vector2>().y;
        if (context.started)
        {
            //only scroll through objects if they are aimed
            if (aimedDownSights)
            {
                gravityField.ScrollOrbit(inputScroll);
            }
        }
    }
    public void ShootObject(InputAction.CallbackContext context)
    {
        //do once 
        if (context.started)
        {
            if (aimedDownSights)
            {
                //simply change gravity in similar fashion to the player
                gravityField.ShootObject(myCamera.transform.forward);
            }
        }
    }
    #endregion
}
