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
    
    
    //this list will communicate with the other two lists to check if it's able to join up, however will the dynamic unlike the other two.
    [SerializeField] public List<InteractableObject> objectsInOrbit;
    
    #endregion
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnTriggerEnter(Collider other)
    {
        //check if item can be picked up
        if (other.gameObject.tag == "Interactable")
        {
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

    public void RemoveItem(InteractableObject item)
    {
        //clear item and self from communications
        int leavingID = targetLocks.IndexOf(item.target);
        slotAvailability[leavingID] = true;
        item.target = null;
        item.myAttractor = null;
        //objectsInOrbit.Remove(item);
    }
}
