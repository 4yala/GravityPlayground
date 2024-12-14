using System;
using UnityEngine;

//a profiler for objects that react with high speed collisions (such as breakable objects)
public class HighSpeedReactor : MonoBehaviour, ICollisionReactable
{
    public Action slamSequence;
    public void OnHighSpeedCollision(InteractableObject otherbody)
    {
        if (otherbody.objectTag == objectType.HeavyItem)
        {
            slamSequence?.Invoke();
        }
        otherbody.OnHighSpeedCollision();
    }
}
