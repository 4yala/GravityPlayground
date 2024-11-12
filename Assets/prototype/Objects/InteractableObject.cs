using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent(typeof(CustomGravity))]
[RequireComponent(typeof(SpringJoint))]
public class InteractableObject : MonoBehaviour
{
    #region Variables
    [SerializeField] public objectType objectTag;
    [SerializeField] public bool orbiting;
    [SerializeField] public Transform holdsterTarget;
    [SerializeField] public Transform launchPoint;
    [SerializeField] public GravityField myAttractor;
    [SerializeField] public CustomGravity gravity;
    [SerializeField] public bool launched;
    [SerializeField]  SpringJoint myJoint;
    
    #endregion

    // Start is called before the first frame update
    void Start()
    {
        gameObject.tag = "Interactable";
        gravity = GetComponent<CustomGravity>();
    }
    

    void FixedUpdate()
    {
        if (orbiting && holdsterTarget != null && launchPoint == null)
        {
            //fix later
            //gameObject.transform.position = Vector3.Lerp(gameObject.transform.position, holdsterTarget.position, Time.fixedDeltaTime);
        }
        else if (orbiting && holdsterTarget != null && launchPoint != null)
        {
            gameObject.transform.position = Vector3.Lerp(gameObject.transform.position, launchPoint.position, Time.fixedDeltaTime);
            Debug.Log("readying");
        }
    }

    #region InteractionEvents

    public void ToggleOrbit(bool enable , Transform incomingTarget)
    {
        if (enable)
        {
            orbiting = true;
            holdsterTarget = incomingTarget;
            myJoint.connectedBody = holdsterTarget.GetComponent<Rigidbody>();
            //incomingTarget.GetComponent<SpringJoint>().connectedBody = gravity.rb;
            //Debug.Log();
            gravity.SetZeroGravity(0f);
        }
        else
        {
            //myAttractor.RemoveItem(gameObject.GetComponent<InteractableObject>());
            orbiting = false;
            //incomingTarget.GetComponent<SpringJoint>().connectedBody = null;
            myJoint.connectedBody = null;
            holdsterTarget = null;
            gravity.RevertGravity(false,0f);
        }
    }
    public void ReadyObject(bool enable, Transform pointToGo)
    {
        if (enable)
        {
            launchPoint = pointToGo;
            holdsterTarget.GetComponent<SpringJoint>().connectedBody = null;
            myJoint.connectedBody = null;
        }
        else
        {
            launchPoint = null;
            //holdsterTarget.GetComponent<SpringJoint>().connectedBody = gravity.rb;
            if (holdsterTarget)
            {
                myJoint.connectedBody = holdsterTarget.GetComponent<Rigidbody>();
            }
            Debug.Log("missing function");
            //lerp position back to a designated place
        }
    }
    #endregion
}
