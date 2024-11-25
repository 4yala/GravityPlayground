using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GravityField : MonoBehaviour
{
    #region Variables
    [Header("Inventory references")]
    [Tooltip("Empty transform positions (Must be added as a child and then be manually added)")]
    [SerializeField] List<Transform> targetLocks;
    [Tooltip("Secondary list that matches the target locks (Only for visualising)")]
    [SerializeField] public List<bool> slotAvailability;
    [Tooltip("Another empty transform position which is where objects are launched from (Must be added as a child and manually added)")]
    [SerializeField] Transform shootPoint;
    
    [Header("Inventory")]
    [Tooltip("Object held at launch point")]
    [SerializeField] InteractableObject objectToShoot;
    [Tooltip("The length that gravitational force is applied for once launched (seconds)")]
    [SerializeField] float projectileLife;
    [Tooltip("Objets attracted the the field (Only for visualising)")]
    [SerializeField] public List<InteractableObject> objectsInOrbit;
    [Tooltip("Objects influenced by launch and out of field (Only for visualising)")]
    [SerializeField] public List<InteractableObject> objectsOutOfOrbit;

    [Header("Movement Values")] 
    [Tooltip("The speed of which the field rotates to match directions")]
    [SerializeField] float rotationSpeed;
    
    [Header("External references")]
    [Tooltip("Player reference (Automated)")]
    [SerializeField] public PlayerControllerDebug owner;
    
    #endregion

    void Update()
    {
        //follow the players position
        transform.position = owner.transform.position;
        //rotation to follow camera direction across a horizontal plane
        if (!objectToShoot)
        {
            Vector3 flattenedForward = Vector3.ProjectOnPlane(owner.myCamera.transform.forward, owner.transform.up);
            Quaternion targetRotation = Quaternion.LookRotation(flattenedForward, owner.transform.up);
            if (transform.rotation != targetRotation)
            {
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed*Time.deltaTime);
            }
        }
        
        //rotation to follow the characters direction on a vertical plane
        else
        {
            Vector3 flattenedForward = Vector3.ProjectOnPlane(owner.transform.forward, owner.myCamera.transform.up);
            Quaternion targetRotation = Quaternion.LookRotation(flattenedForward, owner.myCamera.transform.up);
            if (transform.rotation != targetRotation)
            {
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed*Time.deltaTime);
            }
        }
    }

    #region Inventory changes
    
    //add items when it comes into contact with field
    void OnTriggerEnter(Collider other)
    {
        //check if item can be picked up
        if (other.gameObject.tag == "Interactable")
        {
            //check if the object exists in the lists already
            if(objectsInOrbit.Contains(other.gameObject.GetComponent<InteractableObject>()) || objectsOutOfOrbit.Contains(other.gameObject.GetComponent<InteractableObject>()))
            {
                return;
            }
            Debug.Log("Item entering");
            //check if there is space available
            if (objectsInOrbit.Count < targetLocks.Count)
            {
                //add item to orbit
                objectsInOrbit.Add(other.gameObject.GetComponent<InteractableObject>());
                
                //get a reference of item to manipulate from list
                int incomingId = objectsInOrbit.IndexOf(other.gameObject.GetComponent<InteractableObject>());
                
                //set up item with dependencies
                for (int i = 0; i < slotAvailability.Count; i++)
                {
                    //find an available index to lock the object to
                    if (slotAvailability[i])
                    {
                        objectsInOrbit[incomingId].ToggleOrbit(true, targetLocks[i],false, gameObject.GetComponent<GravityField>());
                        slotAvailability[i] = false;
                        return;
                    }
                }
            }
            
        }
        
    }
    
    //for single clearing access
    public void RemoveItem(InteractableObject item, bool shooting = false)
    {
        //clear item and self from communications
        int leavingID = targetLocks.IndexOf(item.attractionPoint);
        slotAvailability[leavingID] = true;
        item.ToggleOrbit(false,null, shooting, gameObject.GetComponent<GravityField>());
        objectsInOrbit.Remove(item);
    }
    
    //for overall clearing access
    public void DisableField()
    {
        //make every slot available
        for (int i = 0; i < slotAvailability.Count; i++)
        {
            slotAvailability[i] = true;
        }
        
        //clear all items and dependencies
        foreach (InteractableObject item in objectsInOrbit)
        {
            item.ToggleOrbit(false);
        }
        foreach (InteractableObject item in objectsOutOfOrbit)
        {
            item.gravity.RevertGravity(false,0f);
            item.launched = false;
        }
        TriggerAim(false);
        //clear list
        objectsInOrbit.Clear();
        objectsOutOfOrbit.Clear();
    }
    #endregion
    
    
    #region Shooting Events
    
    //enable aiming
    public void TriggerAim(bool toggle)
    {
        //logically speaking this should only be called if there are objects in orbit
        //check back if there are issues
        if (toggle)
        {
            //default set to the first object in reference
            if (objectsInOrbit.Count > 0)
            {
                objectToShoot = objectsInOrbit[0];
                objectToShoot.ReadyObject(true, shootPoint);
                owner.myCameraCm.m_Orbits[1].m_Radius = owner.aimedDistance;
                owner.aimedDownSights = true;
            }
        }
        else
        {
            //clear object from aim position
            //there is vulnerability of error as objectToShoot may be null here
            if (objectToShoot)
            {
                objectToShoot.ReadyObject(false, null);
                objectToShoot = null;
            }
            owner.aimedDownSights = false;
            owner.myCameraCm.m_Orbits[1].m_Radius = owner.defaultDistance;
        }
    }
    
    //select a new item to shoot
    public void ScrollOrbit(float inputValue)
    {
        //avoid breakage if list is empty
        if (objectsInOrbit.Count == 0)
        {
            TriggerAim(false);
            return;
        }
        
        //get reference of current object
        int currentObjectID = objectsInOrbit.IndexOf(objectToShoot);
        int newObjectID = 0;
        //based on input values either increase or decrease
        //looping behaviour exists, if it's at the lower or the hightest value loop
        if (inputValue > 0)
        {
            if (currentObjectID == objectsInOrbit.Count - 1)
            {
                newObjectID = 0;
            }
            else
            {
                newObjectID = currentObjectID + 1;
            }
        }
        else if (inputValue < 0)
        {
            if (currentObjectID <= 0)
            {
                newObjectID = objectsInOrbit.Count - 1;
            }
            else
            {
                newObjectID = currentObjectID - 1;
            }
        }
        //reset object
        objectToShoot.ReadyObject(false,null);
        objectToShoot = objectsInOrbit[newObjectID];
        objectToShoot.ReadyObject(true,shootPoint);
    }
    
    //shoot selected item
    public void ShootObject(Vector3 direction)
    {
        //shoot if there's an object ready
        if (objectToShoot)
        {
            //change lists
            objectsOutOfOrbit.Add(objectToShoot);
            RemoveItem(objectToShoot, true);
            
            //launch in direction
            objectToShoot.launched = true;
            objectToShoot.gravity.SetNewGravity(direction,false,0f);
            StartCoroutine(StartEffectTimer(objectToShoot));
            
            //select another object if available
            if (objectsInOrbit.Count > 0)
            {
                ScrollOrbit(1);
            }
            // there is none, deselect aim
            else
            {
                TriggerAim(false);
            }

        }
    }
    
    //start life of gravitational influence per object
    IEnumerator StartEffectTimer(InteractableObject shotObject)
    {
        yield return new WaitForSeconds(projectileLife);
        //finish clearing object from field once timer has ended
        //reset gravity
        shotObject.gravity.RevertGravity(false,0f);
        objectsOutOfOrbit.Remove(shotObject);
        shotObject.launched = false;
    }
    #endregion

}
