using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
[RequireComponent(typeof(CustomGravity))]
public class PlayerControllerDebug : MonoBehaviour
{
    #region Variables

    public PlayableCharacterProfile myProfile;
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
    [SerializeField] public GravityField gravityField;
    
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

    [Header("Gravity Values")] 
    [SerializeField] public Image gravityMeter;
    [SerializeField] public float currentGravityMeter;
    [SerializeField] float currentUpdate;
    [SerializeField] bool regenCooldown;
    [SerializeField] float cooldownTimer;
    
    [Header("Cached values (Only for visualising)")]
    [SerializeField] Vector2 moveInput;
    [SerializeField] Vector2 cameraInput;
    [SerializeField] float rotateInput;
    [SerializeField] Vector3 cameraRight;
    [Tooltip("Checker that prevents character orientation fixing from overloading")]
    [SerializeField] bool landIntialised;
    
    Coroutine jumpingCoroutine;
    Coroutine diveCheckCoroutine;

    [SerializeField] bool landOnce;
    [SerializeField] int jumpIncrement;
    [Header("Player States (Only for visualising)")]
    [SerializeField] public bool zerograv;
    [SerializeField] bool shifted;
    [SerializeField] bool immobile;
    [SerializeField] public bool grounded;
    [SerializeField] public bool diving;
    [SerializeField] bool noGroundDetected;
    [SerializeField] bool shiftDiving; //to be used only when toggling out from zero gravity, in order to smooth landing.
    [SerializeField] bool cameraTransitioned;
    [SerializeField] bool fieldEnabled;
    [SerializeField] public bool aimedDownSights;
    [SerializeField] bool cameraCentered;
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
        myCameraCm.m_Orbits[1].m_Radius = myProfile.defaultDistance;
        currentGravityMeter = myProfile.maxGravityMeter;
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
        UpdateGravityMeter();
        
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
                        
                        if (upwardSlope)
                        {
                            if (slopeAngle > myProfile.minSlopeAngle && slopeAngle <= myProfile.maxSlopeAngleAscent)
                            {
                                finalDirection = Vector3.ProjectOnPlane(targetDirection, slopeNormal).normalized;
                            }
                            else if (slopeAngle > myProfile.maxSlopeAngleAscent && upwardSlope)
                            {
                                finalDirection = Vector3.zero;
                            }
                        }
                        else
                        {
                            if (slopeAngle > myProfile.minSlopeAngle && slopeAngle <= myProfile.maxSlopeAngleDescent)
                            {
                                finalDirection = Vector3.ProjectOnPlane(targetDirection, slopeNormal).normalized;
                            }
                        }
                    }
                    //calculate and make the player face the direction if the slope allows it to
                    if (finalDirection != Vector3.zero)
                    {
                        
                        Quaternion targetRotation = Quaternion.LookRotation(targetDirection, myCameraOrientation.transform.up);
                        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, myProfile.rotationSpeed * Time.fixedDeltaTime);
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
                            rb.AddForce(finalDirection * myProfile.walkAccelerationSpeed, ForceMode.Acceleration);
                        }
                        else
                        {
                            rb.AddForce(transform.forward * myProfile.jumpForwardAccelerationSpeed, ForceMode.Acceleration);
                        }
                    }
                    //calculate velocity 
                    Vector3 forwardVelocity = Vector3.Project(rb.velocity, transform.forward);
                    Vector3 nonForwardVelocity = rb.velocity - forwardVelocity;

                    Vector3 nonForwardDragForce = -nonForwardVelocity * myProfile.groundedDrag; //custom drag

                    
                    Vector3 upwardVelocity = Vector3.Project(rb.velocity, -gravity.gravitationalDirection);
                    Vector3 nonForwardVelocityAerial = nonForwardVelocity - upwardVelocity;
                    Vector3 nonForwardDragForceAir = -nonForwardVelocityAerial* myProfile.groundedDrag / 2;
                    
                    //add opposite forces to player
                    if (grounded)
                    {
                        rb.AddForce(nonForwardDragForce, ForceMode.Acceleration);
                    }
                    else
                    {
                        rb.AddForce(nonForwardDragForceAir, ForceMode.Acceleration) ;
                    }

                }
                //finally, if no input is made brake the player if it's grounded
                else
                {
                    if (grounded)
                    {
                        rb.AddForce(rb.velocity * -myProfile.decelerationSpeed, ForceMode.Force);
                    }
                }
                // prevent from building infinite speed
                if (rb.velocity.magnitude > myProfile.maxSpeedWalk && grounded)
                {
                    rb.velocity = rb.velocity.normalized * myProfile.maxSpeedWalk;
                }
                else if (rb.velocity.magnitude > myProfile.maxSpeedJump && !grounded)
                {
                    rb.velocity = rb.velocity.normalized * myProfile.maxSpeedJump;
                }
            }
            else if(grounded && aimedDownSights)
            {
                //match the camera's forward direction on a horizontal plane
                Vector3 flattenedForward = Vector3.ProjectOnPlane(myCamera.transform.forward, transform.up);
                Quaternion targetRotation = Quaternion.LookRotation(flattenedForward, transform.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, myProfile.rotationSpeed * Time.fixedDeltaTime);
                
                //movement in all directions (allow strafing)
                if (moveInput.magnitude > 0.1f)
                {
                    //Debug.Log("moving");
                    Vector3 targetMovement = transform.forward * moveInput.y + transform.right * moveInput.x;
                    rb.AddForce(targetMovement * myProfile.aimedAccelerationSpeed, ForceMode.Acceleration);
                    
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
                    rb.AddForce(rb.velocity * -myProfile.decelerationSpeed, ForceMode.Force);
                }
                // prevent from building infinite speed
                if (rb.velocity.magnitude > myProfile.maxSpeedAim && grounded)
                {
                    Debug.Log("Max speed reached");
                    rb.velocity = rb.velocity.normalized * myProfile.maxSpeedAim;
                }
            }
        }
        //aerial movement (diving in the air)
        else
        {
            //movement is enabled when character is in proper position (head first)
            //there may be inaccuracies, so distance is calculated instead of a precise comparison
            if (Vector3.Distance(transform.up, gravity.gravitationalDirection) < 0.01f)
            {
                //check if there's active input for character rotation
                if (rotateInput != 0)
                {
                    float rotationAmount = myProfile.diveRotationSpeed * rotateInput * Time.deltaTime;
                    transform.Rotate(0, rotationAmount, 0);
                }

                if (cameraCentered)
                {
                    Quaternion atargetRotation = Quaternion.LookRotation(transform.up, -transform.forward);
                    cameraTarget2.rotation = Quaternion.RotateTowards(cameraTarget2.rotation, atargetRotation, 60f * Time.fixedDeltaTime);
                }
                //calculate movement
                Vector3 targetMovement = -transform.forward * moveInput.y + transform.right * moveInput.x;
                //drag must affect these directions
                Vector3 lateralVelocity = rb.velocity - Vector3.Project(rb.velocity, gravity.gravitationalDirection);
                if (targetMovement.magnitude > 0.1f)
                {
                    rb.AddForce(targetMovement * myProfile.lateralAirDiveSpeed, ForceMode.Acceleration);

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
                if (lateralSpeed > myProfile.maxLateralSpeed)
                {
                    //calculate a max lateral speed
                    Vector3 clampedLateralSpeed = lateralVelocity.normalized * myProfile.maxLateralSpeed;
                    //grab the current velocity and just add the lateral speed as appropriate
                    rb.velocity = Vector3.Project(rb.velocity, gravity.gravitationalDirection) + clampedLateralSpeed;
                    Debug.Log("Maxing out lateral speed");  
                }
            }
            
            Quaternion targetRotation = Quaternion.FromToRotation(transform.up, gravity.gravitationalDirection) * transform.rotation;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, 50f * Time.fixedDeltaTime);
            
            
            //calculate velocity along the gravitational direction
            Vector3 fallingSpeed = Vector3.Project(rb.velocity, gravity.gravitationalDirection);
            //if the falling speed does not exceed the maximum dive speed, increase by acceleration
            if (fallingSpeed.magnitude < myProfile.terminalVelocity)
            {
                rb.AddForce(gravity.gravitationalDirection * myProfile.diveAccelerationSpeed, ForceMode.Acceleration);
            }
                
            if (fallingSpeed.magnitude > myProfile.terminalVelocity)
            {
                //grab current velocity
                Vector3 currentVelocity = rb.velocity;
                //calculate velocity proportionate to falling direction
                Vector3 velocityAlongFall = Vector3.Project(currentVelocity, gravity.gravitationalDirection);
                //hard set velocity to maximum falling speed + current velocity STRIPPED of the falling velocity, leaving any other forces (for example lateral velocity)
                rb.velocity = Vector3.Project(rb.velocity, gravity.gravitationalDirection).normalized * myProfile.terminalVelocity + (currentVelocity - velocityAlongFall);
            }
            
        }   
    }
    
    //Events that are called upon to make changes
    #region Reaction events

    void UpdatePlayerSkin(Material newMaterial)
    {
        FindChildren(transform,newMaterial);
    }

    void FindChildren(Transform part, Material chosenMaterial)
    {
        foreach (Transform child in part)
        {
            Renderer partRenderer = child.GetComponent<Renderer>();

            if (partRenderer != null)
            {
                partRenderer.material = chosenMaterial;
                Debug.Log("changing!");
            }
            FindChildren(child, chosenMaterial);
        }
    }
    void CancelGravity()
    {
        if (!shifted)
        {
            return;
        }
        //interrupt jump if in progress
        if (jumpingCoroutine != null)
        {
            StopCoroutine(jumpingCoroutine);
            jumpingCoroutine = null;
        }
        UpdatePlayerSkin(myProfile.defaultSkin);  
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
    //flips the player upright when landing after a free fall / dive / gravity shift
    IEnumerator LandPlayer(RaycastHit hit, float duration, bool smoothGravity)
    {
        landIntialised = true;
        float elapsedTime = 0;
        //set up to calculate the way the character will flip
        Quaternion targetRotation = Quaternion.identity;
        bool closeToNormalDown = (Vector3.Dot(gravity.gravitationalDirection.normalized, Vector3.down) > .9f || 
                                  Vector3.Dot(hit.normal.normalized, Vector3.up) > .9f);
        //if gravity smoothing is on, revert to normality
        if (closeToNormalDown && smoothGravity)
        {
            gravity.SoftSetGravity(Vector3.down);
            //cameraTarget.rotation = Quaternion.FromToRotation(transform.up, Vector3.up) * transform.rotation;
            shifted = false;
            UpdatePlayerSkin(myProfile.defaultSkin);
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
            transform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);
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
        
        LockCamera(true);
        myCameraOrientation.transform.up = transform.up;

        //StartCoroutine(OrientateCamera(0.5f, transform.up, true));
        landIntialised = false;
        landOnce = true;
        yield return null;
    }
    IEnumerator QueueDive()
    {
        Debug.Log("coroutine started");
        noGroundDetected = true;
        yield return new WaitForSeconds(myProfile.diveWaitTime);
        if (noGroundDetected)
        {
            diving = true;
            playerAni.SetBool("Diving", true);
            myCameraCm.m_Orbits[0].m_Radius = .1f;
            myCameraCm.m_Orbits[2].m_Radius = .1f;
            Debug.Log("Count down finished and diving");
        }
        LockCamera(false);
        diveCheckCoroutine = null;
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
        float newYAxis = Vector3.Dot(gravity.gravitationalDirection, myCamera.transform.forward);
        newYAxis = (newYAxis - -1) / (1 - -1) * (1 - 0) + 0;
        myCameraCm.m_YAxis.Value = newYAxis;
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
                if (hit.collider.gameObject.tag == "Player" || hit.collider.gameObject.GetComponent<InteractableObject>() != null || hit.collider.gameObject.tag == "Non-Steppable")  
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
        Debug.DrawRay(transform.position, gravity.gravitationalDirection * myProfile.diveLength, Color.red);
        if (!Physics.Raycast(transform.position, gravity.gravitationalDirection, out hit, myProfile.diveLength))
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
                else if(diveCheckCoroutine == null)
                {
                    diveCheckCoroutine = StartCoroutine(QueueDive());
                }
            }
        }
        else
        {
            //unnecessary??
            playerAni.SetBool("Diving", false);
            if (noGroundDetected)
            {
                StopCoroutine(diveCheckCoroutine);
                noGroundDetected = false;
            }
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
    
    //check gravity meter
    void UpdateGravityMeter()
    {
        float cachedUse = Mathf.Sign(currentUpdate);
        //set states
        if (shifted)
        {
            if (zerograv)
            {
                currentUpdate = myProfile.zeroGravRate;
            }
            else if (shiftDiving || shifted && diving)
            {
                currentUpdate = myProfile.shiftDiveRate;
            }
            else if (grounded)
            {
                currentUpdate = myProfile.groundedRate;
            }   
            //just in case, change to negative otherwise leave normal
            if (currentUpdate > 0)
            {
                currentUpdate *= -1f;
            }
        }
        else
        {
            currentUpdate = myProfile.regenRate;
        }

        if (cachedUse == -1 && currentUpdate > 0)
        {
            regenCooldown = true;
            cooldownTimer = myProfile.regenCooldownLength;
        }
        //update accordingly!
        //if change from positive or negative, call a cool down
        if (!regenCooldown && currentUpdate > 0 || currentUpdate < 0)
        {
            currentGravityMeter += currentUpdate * Time.deltaTime;
            if (currentGravityMeter > myProfile.maxGravityMeter)
            {
                currentGravityMeter = myProfile.maxGravityMeter;
            }
            else if (currentGravityMeter < 0)
            {
                currentGravityMeter = 0;
                CancelGravity();
            }
        }

        if (regenCooldown)
        {
            cooldownTimer -= Time.deltaTime;
            if (cooldownTimer <= 0)
            {
                regenCooldown = false;
            }
        }
        gravityMeter.fillAmount = currentGravityMeter / myProfile.maxGravityMeter;
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
            jumpIncrement++;
            if (jumpIncrement == 1)
            {
                if (grounded && jumpingCoroutine == null)
                {
                    jumpingCoroutine = StartCoroutine(SmoothJump());
                    //rb.AddForce(transform.up * myProfile.jumpForce, ForceMode.Impulse);
                    Debug.Log("jumping");
                }
                else
                {
                    jumpIncrement--;
                }
            }
            else
            {
                jumpIncrement--;
                Debug.Log("overload! decreasing");
            }
        }
    }

    IEnumerator SmoothJump()
    {
        bool liftoff = false;
        float jumpDuration = .1f;
        float elapsedTime = 0;

        while (elapsedTime < jumpDuration)
        {
            if (!liftoff && !grounded)
            {
                liftoff = true;
            }
            if (liftoff && grounded)
            {
                jumpIncrement--;
                yield break;
            }
            float forcePerframe = myProfile.jumpForce / jumpDuration * Time.fixedDeltaTime;
            
            rb.AddForce(transform.up * forcePerframe, ForceMode.Acceleration);
            
            elapsedTime += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }
        jumpIncrement--;
        jumpingCoroutine = null;
    }
    public void AerialRotate(InputAction.CallbackContext context)
    {
        rotateInput = context.ReadValue<float>();
    }
    public void CameraRotate(InputAction.CallbackContext context)
    {
        if (!myCameraCm.gameObject.activeInHierarchy && !cameraCentered)
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
            CancelGravity();
        }
    }
    
    public void ToggleGravity(InputAction.CallbackContext context)
    {
        //do only once
        if (context.started)
        {
            if (!shifted)
            {
                UpdatePlayerSkin(myProfile.shiftedSkin);
            }
            shifted = true;
            if (!zerograv)
            {
                //disable gravitational forces
                //use new gravity component
                gravity.SetZeroGravity(myProfile.zeroGravDrag);
                
                
                //immobilise player
                immobile = true;
                zerograv = true;
                grounded = false;
                diving = false;
                shiftDiving = false;
                noGroundDetected = false;
                cameraCentered = false;

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

            if (jumpingCoroutine != null)
            {
                StopCoroutine(jumpingCoroutine);
                jumpingCoroutine = null;
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
        if (!grounded && !zerograv && !diving)
        {
            return;
        }
        else if (diving)
        {
            if (context.started)
            {
                cameraCentered = true;
            }
            else if (context.canceled)
            {
                cameraCentered = false;
            }
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
