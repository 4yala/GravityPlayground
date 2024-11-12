using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GravityField : MonoBehaviour
{
    #region Variables
    //vvv these two lists are correspondent with each other, they must always be the same length and update accordingly
    [SerializeField] List<Transform> targetLocks;
    [SerializeField] public List<bool> slotAvailability;
    //^^^
    [SerializeField] Transform shootPoint;
    [SerializeField] InteractableObject objectToShoot;
    [SerializeField] float projectileLife;
    
    //this list will communicate with the other two lists to check if it's able to join up, however will the dynamic unlike the other two.
    [SerializeField] public List<InteractableObject> objectsInOrbit;
    [SerializeField] public List<InteractableObject> objectsOutOfOrbit;
    
    //does it need just the player?
    [SerializeField] public PlayerControllerDebug owner;
    
    
    #endregion

    #region Inventory changes
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
                objectsInOrbit[incomingId].myAttractor = gameObject.GetComponent<GravityField>();
                for (int i = 0; i < slotAvailability.Count; i++)
                {
                    //find an available index to lock the object to
                    if (slotAvailability[i])
                    {
                        objectsInOrbit[incomingId].ToggleOrbit(true, targetLocks[i]);
                        slotAvailability[i] = false;
                        return;
                    }
                }
            }
            
        }
        
    }
    
    //for single clearing access
    public void RemoveItem(InteractableObject item)
    {
        //clear item and self from communications
        int leavingID = targetLocks.IndexOf(item.holdsterTarget);
        slotAvailability[leavingID] = true;
        item.holdsterTarget = null;
        item.myAttractor = null;
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
            item.myAttractor = null;
            item.holdsterTarget = null;
            item.gravity.RevertGravity(false,0f);
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
                owner.aimedDownSights = true;
            }
        }
        else
        {
            //clear object from aim position
            //there is vulnerability of error as objectToShoot may be null here
            if (!objectToShoot)
            {
                return;
            }
            objectToShoot.ReadyObject(false, null);
            objectToShoot = null;
            owner.aimedDownSights = false;
        }
    }
    public void ScrollOrbit(float inputValue)
    {
        //avoid breakage if list is empty
        if (objectsInOrbit.Count == 0)
        {
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
    public void ShootObject(Vector3 direction)
    {
        //shoot if there's an object ready
        if (objectToShoot)
        {
            //change lists
            objectsOutOfOrbit.Add(objectToShoot);
            RemoveItem(objectToShoot);
            
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
