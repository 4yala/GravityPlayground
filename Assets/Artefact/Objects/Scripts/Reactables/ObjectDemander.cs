using System;
using UnityEngine;

//a profiler for objects that have collision reactions to unique objects
public class ObjectDemander : MonoBehaviour, ICollisionReactable
{
    public Action uniqueActionToExecute;

    [SerializeField] public string objectRequired;
    
    public void SoftCollision(GameObject otherbody)
    {
        if (otherbody.gameObject.GetComponent<ICollisionReactable>() != null)
        {
            if (otherbody.gameObject.GetComponent<ICollisionReactable>().ReturnUniqueName() == objectRequired)
            {
                uniqueActionToExecute?.Invoke();
               otherbody.gameObject.GetComponent<ICollisionReactable>().SoftCollision(null);
            }
        }
    }
}
