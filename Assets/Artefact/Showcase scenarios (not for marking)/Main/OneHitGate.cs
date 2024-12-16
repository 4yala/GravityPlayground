using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OneHitGate : MonoBehaviour
{
    [SerializeField] HighSpeedReactor myParent;
    // Start is called before the first frame update
    void Awake()
    {
        myParent = GetComponent<HighSpeedReactor>();
        myParent.slamSequence += BreakDownDoor;
    }

    void BreakDownDoor()
    {
        gameObject.SetActive(false);
    }
}
