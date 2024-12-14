using UnityEngine;

[RequireComponent(typeof(ConstantForce))]
[RequireComponent(typeof(Rigidbody))]

public class CustomGravity : MonoBehaviour
{
    #region Variables
    [Header("Variables")]
    [Tooltip("The direction that the gravity pulls towards (Only for visualising)")]
    [SerializeField] public Vector3 gravitationalDirection;
    [Tooltip("The total force that the object totals to")]
    [SerializeField] public float gravityForceUnit = 9.81f;
    
    [Header("Components (Automated)")]
    [Tooltip("The gravitational force executor (Automated)")]
    [SerializeField] public ConstantForce myGravitationalForce;
    [Tooltip("The rigidbody of the object (Automated)")]
    [SerializeField] public Rigidbody rb;
    #endregion

    // Start is called before the first frame update
    void Start()
    {
        //connect components
        myGravitationalForce = GetComponent<ConstantForce>();
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        
        //initialise with gravity pulling down
        myGravitationalForce.force = Vector3.down * gravityForceUnit;
        gravitationalDirection = Vector3.down;
    }

    #region Set Events
    //set direction for reference
    public void SoftSetGravity(Vector3 direction)
    {
        gravitationalDirection = direction.normalized;
    }
    //set direction with gravity
    public void SetNewGravity(Vector3 direction, bool noRotation, float drag)
    {
        gravitationalDirection = direction.normalized;
        myGravitationalForce.force = gravitationalDirection* gravityForceUnit;
        rb.freezeRotation = noRotation;
        rb.drag = drag;
    }
    //start zero gravity behaviour
    public void SetZeroGravity(float zeroGravDrag)
    {
        myGravitationalForce.force = Vector3.zero;
        gravitationalDirection *= 0f;
        rb.freezeRotation = false;
        rb.drag = zeroGravDrag;
    }
    //reset gravity to normal
    public void RevertGravity(bool noRotation, float drag)
    {
        gravitationalDirection = Vector3.down;
        myGravitationalForce.force = gravitationalDirection * gravityForceUnit;
        rb.freezeRotation = noRotation;
        rb.drag = drag;
    }
    #endregion

}
