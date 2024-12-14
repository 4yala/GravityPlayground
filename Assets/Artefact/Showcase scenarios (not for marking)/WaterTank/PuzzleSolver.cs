using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PuzzleSolver : MonoBehaviour
{
    [SerializeField] StaticTorch myTorchComponent;
    [SerializeField] GameObject doorsToOpen;
    // Start is called before the first frame update
    void Start()
    {
        myTorchComponent = gameObject.GetComponent<StaticTorch>();
    }

    // Update is called once per frame
    void Update()
    {
        if (myTorchComponent.fireLit && doorsToOpen.activeInHierarchy)
        {
            doorsToOpen.SetActive(false);
        }
    }
}
