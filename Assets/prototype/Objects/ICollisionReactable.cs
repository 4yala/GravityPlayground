using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ICollisionReactable
{
    virtual void OnHighSpeedCollision(InteractableObject otherbody){}
    virtual void SoftCollision(GameObject otherbody){}
    virtual string ReturnUniqueName()
    {
        return "None";
    }

}
