using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KeyBehaviour : MonoBehaviour
{
    [SerializeField] InteractableObject myParent;

    private void Awake()
    {
        myParent = gameObject.GetComponent<InteractableObject>();
        myParent.uniqueCollisionReaction += UniqueReaction;
    }

    void UniqueReaction()
    {
        gameObject.SetActive(false);
    }
}
