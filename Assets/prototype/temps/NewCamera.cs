using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class NewCamera : MonoBehaviour
{
    [SerializeField] Transform lookAt;
    [SerializeField] float distance;
    [SerializeField] float minDistance;
    [SerializeField] float xSpeed;
    [SerializeField] float ySpeed;
    [SerializeField] float yMinLimit;
    [SerializeField] float yMaxLimit;
    [SerializeField] Vector2 mouseInput;
    [SerializeField] Vector2 rotationAngles;
    [SerializeField] Vector3 targetPosition;
    [SerializeField] float clipEvasion;
    [SerializeField] Transform myOrientation;
    [SerializeField] float sensitivity;
    
    // Start is called before the first frame update
    void Start()
    {
        transform.LookAt(lookAt);
        //.x = transform.rotation.eulerAngles.x;
        //rotationAngles.y = transform.rotation.eulerAngles.y;
    }
    void LateUpdate()
    {
        
        rotationAngles.x += (mouseInput.x *sensitivity/* xSpeed * Time.deltaTime*/);
        rotationAngles.y -= (mouseInput.y *sensitivity/* ySpeed * Time.deltaTime*/);
        rotationAngles.y = Mathf.Clamp(rotationAngles.y, yMinLimit, yMaxLimit);
        Vector3 normalizedUp = myOrientation.transform.up.normalized;
        Debug.Log(rotationAngles);
        Vector3 right = Vector3.Cross(normalizedUp,  transform.forward);
        Quaternion rotation = Quaternion.AngleAxis(rotationAngles.x, normalizedUp) *  Quaternion.AngleAxis(rotationAngles.y, right);
        targetPosition = lookAt.position - (rotation * Vector3.forward * distance);
        
        RaycastHit hit;
        if(Physics.Raycast(lookAt.position, targetPosition, out hit, distance))
        {
            float newDistance = Vector3.Distance(hit.point, lookAt.position) - clipEvasion;
            newDistance = Mathf.Max(newDistance, minDistance);
            transform.position = targetPosition -(rotation * Vector3.forward * newDistance);
        }
        else
        {
            transform.position = targetPosition;
        }
        transform.LookAt(lookAt, myOrientation.up);
    }
    
    public void OnLook(InputAction.CallbackContext context)
    {
        mouseInput = context.ReadValue<Vector2>();
    }

    void CalculateMouseInput(Quaternion rotation)
    {
        Vector3 rightDirection = (rotation * Vector3.right);
        Vector3 forwardDirection = (rotation * Vector3.forward);
        
        targetPosition = lookAt.position + (rightDirection * distance * Mathf.Cos(rotationAngles.y * Mathf.Deg2Rad) + forwardDirection * distance * Mathf.Sin(rotationAngles.y * Mathf.Deg2Rad));
        
    }

    public void SetNewUp()
    {
        Vector3 offset = (targetPosition - lookAt.position).normalized * distance;
        rotationAngles.x = Vector3.SignedAngle(myOrientation.up, offset, Vector3.Cross(myOrientation.up, offset));
        rotationAngles.y = Vector3.SignedAngle(offset,  Vector3.ProjectOnPlane(offset,myOrientation.up), Vector3.Cross(myOrientation.up, offset));
    }
}
