using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Player components")]
    public Rigidbody rb;
    public float speed, sensitivity, maxForce, jumpForce,rotationSpeed;
    public Vector2 move, look;
    private float lookRotation;
    public GameObject myCamera;
    [SerializeField] Animator playerAni;
    [SerializeField] GameObject pauseCanvas;
    [SerializeField] ConstantForce myGravity;
    [SerializeField] Camera myCam;

    [Header("Player states")]
    public bool zerograv;
    public int loggedDirection;
    public bool grounded;
    public bool diving;
    public bool paused;
    
    // Start is called before the first frame update
    void Start()
    {
        pauseCanvas.SetActive(false);
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    public void OnZeroGrav(InputAction.CallbackContext context)
    {
        if (!zerograv)
        {
            myGravity.force = new Vector3(0f, 0f, 0f);
            zerograv = true;
            rb.velocity = new Vector3(0f,0f,0f);
            speed = 0f;
        }
        else
        {
            /*
            if(loggedDirection == 0)
            {
                myGravity.force = new Vector3(0f, 9.81f, 0f);
                loggedDirection = 1;
            }
            else
            {
                myGravity.force = new Vector3(0f, -9.81f, 0f);
                loggedDirection = 0;
            }
            */
            Vector3 cameraDirection = myCam.transform.forward;
            Vector3 newGravity = cameraDirection.normalized * 9.81f;
            myGravity.force = newGravity;
            zerograv = false;
            speed = 4f;
        }
    }
    public void OnMove(InputAction.CallbackContext context)
    {
        if (!zerograv)
        {
            move = context.ReadValue<Vector2>();
        }
    }
    public void TwistCharacter(float finalReference)
    {
        gameObject.transform.rotation = Quaternion.Euler(gameObject.transform.rotation.x, gameObject.transform.rotation.y + finalReference, gameObject.transform.rotation.z);
    }
    public void OnLook(InputAction.CallbackContext context)
    {
        look = context.ReadValue<Vector2>();
    }
    public void OnJump(InputAction.CallbackContext context)
    {
        if (!zerograv)
        {
            Jump();
        }
        
    }
    private void FixedUpdate()
    {
        Move();
    }
    void Jump()
    {
        Vector3 jumpForces = Vector3.zero;

        if (grounded)
        {
            jumpForces = Vector3.up * jumpForce;
        }

        rb.AddForce(jumpForces,ForceMode.VelocityChange);
    }
    void Move()
    {
        //Rotation twisters
        Vector2 trueMovement = move;
        float forwardMovement = 0;
        if(move.y != 0 && move.x == 0)
        {
            if(move.y > 0)
            {
                gameObject.transform.rotation = Quaternion.Euler(gameObject.transform.rotation.x, myCamera.transform.rotation.eulerAngles.y, gameObject.transform.rotation.z);
                Debug.Log("Forward");
                forwardMovement = 1;
            }
            else
            {
                Vector3 targetPosition = new Vector3(myCamera.transform.position.x, gameObject.transform.position.y, myCamera.transform.position.z);
                gameObject.transform.LookAt(targetPosition);
                trueMovement.y*=-1;
                forwardMovement = 1;
                Debug.Log("Backward");
            }
        }
        else if (move.x != 0 && move.y == 0)
        {
            if(move.x > 0)
            {
                gameObject.transform.rotation = Quaternion.Euler(gameObject.transform.rotation.x, myCamera.transform.rotation.eulerAngles.y+90, gameObject.transform.rotation.z);
                forwardMovement = 1;
                Debug.Log("Right"); 
            }
            else
            {
                gameObject.transform.rotation = Quaternion.Euler(gameObject.transform.rotation.x, myCamera.transform.rotation.eulerAngles.y - 90, gameObject.transform.rotation.z);
                forwardMovement = 1;
                Debug.Log("Left");
            }
        }
        else if (move.x != 0 && move.y != 0)
        {
            float trueDestination = 0;
            switch (move.x, move.y)
            {
                case (<0, >0):
                    Debug.Log("Forward Left");
                    trueDestination = myCamera.transform.rotation.eulerAngles.y - 45;
                    break;
                case (>0, >0):
                    Debug.Log("Forward Right");
                    trueDestination = myCamera.transform.rotation.eulerAngles.y + 45;
                    break;
                case (<0, <0):
                    Debug.Log("Backward Left");
                    trueDestination = myCamera.transform.rotation.eulerAngles.y - 180 + 45;
                    break;
                case (>0, <0):
                    Debug.Log("Backward Right");
                    trueDestination = myCamera.transform.rotation.eulerAngles.y - 180 - 45;
                    break;
            }
            gameObject.transform.rotation = Quaternion.Euler(gameObject.transform.rotation.x, trueDestination, gameObject.transform.rotation.z);
            forwardMovement = 1;
        }
        
        //Velocity functions
        Vector3 currentVelocity = rb.velocity;

        //Vector3 targetVelocity = new Vector3(trueMovement.x, 0, trueMovement.y);

        Vector3 targetVelocity = new Vector3(0,0, forwardMovement);
        targetVelocity *= speed;

        targetVelocity = transform.TransformDirection(targetVelocity);

        Vector3 velocityChange = (targetVelocity - currentVelocity);
        velocityChange = new Vector3(velocityChange.x, 0, velocityChange.z);

        Vector3.ClampMagnitude(velocityChange, maxForce);
        Debug.Log(velocityChange);
        rb.AddForce(velocityChange, ForceMode.VelocityChange);
    }
    // Update is called once per frame
    void Update()
    {
        grounded = IsGrounded();
        if (!grounded && !diving)
        {
            HighGround();
        }
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        Vector3 direction = new Vector3(horizontal, 0f, vertical).normalized;
        if (direction.magnitude >= 0.1f && !zerograv)
        {
            playerAni.SetBool("Moving", true);
        }
        else
        {
            playerAni.SetBool("Moving", false);
        }

    }
    public bool IsGrounded()
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
            return true;
        }
        return false;
    }
    public void HighGround()
    {
        RaycastHit hit;
        float rayLength = 15f; // Adjust based on your character's size
        Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.down) * rayLength, Color.red);
        if (!Physics.Raycast(transform.position, Vector3.down, out hit, rayLength))
        {
            diving = true;
            Debug.Log("Flying");
            playerAni.SetTrigger("Diving");
        }
        else
        {
            diving = false;
        }
    }
    private void LateUpdate()
    {
        transform.Rotate(Vector3.up * look.x * sensitivity);

        /*
         * lookRotation += (-look.y * sensitivity);
         */
    }
    public void SetGrounded(bool state)
    {
        grounded = state;
    }
    /*
    public void Functiondev()
    {
        //character move
        float cameraValue = myCamera.transform.rotation.eulerAngles.y;
        float bodyValue = gameObject.transform.rotation.eulerAngles.y;
        float difference = cameraValue - bodyValue;
        //twist limits.
        if (difference > 0)
        {
            //difference = cameraValue - bodyValue;
            if (difference > twistLimit)
            {
                TwistCharacter(twistLimit);
            }
            else
            {
                TwistCharacter(difference);
            }
        }
        else if (difference < 0)
        {
            //difference = bodyValue - cameraValue;
            if (difference > -twistLimit)
            {
                TwistCharacter(-twistLimit);
            }
            else
            {
                TwistCharacter(difference);
            }
        }
    }
    */
    public void PauseMenu()
    {
        paused = !paused;
        if (paused)
        {
            Time.timeScale = 0f;
            pauseCanvas.SetActive(true);
        }
        else
        {
            Time.timeScale = 1f;
            pauseCanvas.SetActive(false);
        }
    }
}
