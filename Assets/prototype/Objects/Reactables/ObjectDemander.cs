using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectDemander : MonoBehaviour, ICollisionReactable
{
    public Action uniqueActionToExecute;
    [SerializeField] string objectRequired;
    
    public void SoftCollision(GameObject otherbody)
    {
        if (otherbody.gameObject.GetComponent<ICollisionReactable>() != null)
        {
            if (otherbody.gameObject.GetComponent<ICollisionReactable>().ReturnUniqueName() == objectRequired)
            {
                Debug.Log("working");
                uniqueActionToExecute?.Invoke();
               otherbody.gameObject.GetComponent<ICollisionReactable>().SoftCollision(null);
            }
        }
    }
}
