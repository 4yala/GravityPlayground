using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ICollisionReactable
{
    public void OnHighSpeedCollision(InteractableObject otherbody);
}
