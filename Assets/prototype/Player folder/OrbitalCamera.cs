using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class OrbitalCamera : MonoBehaviour
{
    [SerializeField] Transform lookAt;
    [SerializeField] float distance;
    [SerializeField] float rotationSpeed;
    [SerializeField] float yMaxLimit, yMinLimit;
    [SerializeField] Vector2 mouseInput;
    [SerializeField] Transform myOrientation;
    [SerializeField] Transform centrePoint;
    [SerializeField] Vector3 cameraOffset;
    [SerializeField] Quaternion cameraRotation;

    [SerializeField] float angleX;
    [SerializeField] float angleY;
    
    // Start is called before the first frame update
    void Start()
    {
        cameraOffset = (transform.position - lookAt.position).normalized * distance;
        cameraRotation = Quaternion.LookRotation(-cameraOffset, myOrientation.up);
        transform.rotation = cameraRotation;
        
        Vector3 initialRotation = transform.eulerAngles;
        angleX = initialRotation.y;
        angleY = initialRotation.x;
    }

    void LateUpdate()
    {
        /*
        if (!lookAt)
        {
            return;
        }
        //calculate horizontal rotation
        Quaternion horizontalRotation = Quaternion.AngleAxis(mouseInput.x, myOrientation.up);
        cameraRotation = horizontalRotation * cameraRotation;
        
        //calculate vertical rotation
        Vector3 right = cameraRotation * Vector3.right;
        Quaternion verticalRotation = Quaternion.AngleAxis(-mouseInput.y, right);
        cameraRotation = verticalRotation * cameraRotation;
        
        ///clamp vertical rotation
        Vector3 forward = cameraRotation * Vector3.forward;
        float angleFromUp = Vector3.Angle(forward, myOrientation.up);
        //Debug.Log(angleFromUp);
        
        //fix angles at y axis
        if (angleFromUp > yMaxLimit /*&& mouseInput.y > 0/)
        {
            cameraRotation = Quaternion.AngleAxis(yMaxLimit, right) *
                             Quaternion.LookRotation(Vector3.ProjectOnPlane(forward, right), myOrientation.up);
            Debug.Log("max reached");
        }
        else if (angleFromUp < yMinLimit /*&& mouseInput.y > 0/)
        {
            cameraRotation =  Quaternion.AngleAxis(yMinLimit, right) *
                              Quaternion.LookRotation(Vector3.ProjectOnPlane(forward, right), myOrientation.up);
            Debug.Log("minimum reached");
        }
        */
        
        if (!lookAt)
        {
            return;
        }    
        
        //input angles
        angleX += mouseInput.x;
        angleY = Mathf.Clamp(angleY - mouseInput.y, yMinLimit, yMaxLimit);
        
        Vector3 dynamicRight = Vector3.Cross(myOrientation.up, Vector3.forward).normalized;
        
        //apply in quaternions
        Quaternion horizontalRotation = Quaternion.AngleAxis(angleX, myOrientation.up);
        Quaternion verticalRotation = Quaternion.AngleAxis(angleY, dynamicRight);
        
        cameraRotation = horizontalRotation * verticalRotation;
        
        
        transform.position = lookAt.position - (cameraRotation * Vector3.forward) * distance;
        transform.LookAt(lookAt, myOrientation.up);
    }
    public void OnLook(InputAction.CallbackContext context)
    {
        mouseInput = context.ReadValue<Vector2>();
    }
    void ReCentre()
    {
        transform.position = Vector3.MoveTowards(transform.position, centrePoint.position, distance * Time.deltaTime);
    }

    public void ReDefineUp()
    {
        Quaternion HorizontalRotation = Quaternion.AngleAxis(-angleY, myOrientation.up);
        Vector3 dynamicRight = HorizontalRotation * Vector3.right;
    }
}
