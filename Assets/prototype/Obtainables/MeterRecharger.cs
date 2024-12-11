using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class MeterRecharger : MonoBehaviour
{
    [SerializeField] float amount;
    [SerializeField] private float reSpawnCoolDown;

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.GetComponent<PlayerControllerDebug>())
        {
            PlayerControllerDebug obtainer = other.gameObject.GetComponent<PlayerControllerDebug>();
            obtainer.currentGravityMeter += amount;
            obtainer.gravityMeter.fillAmount = obtainer.currentGravityMeter / obtainer.myProfile.maxGravityMeter;
            StartCoroutine(ConsumedCoolDown());
        }
    }

    IEnumerator ConsumedCoolDown()
    {
        gameObject.GetComponent<MeshRenderer>().enabled = false;
        gameObject.GetComponent<Collider>().enabled = false;
        yield return new WaitForSeconds(reSpawnCoolDown);
        gameObject.GetComponent<MeshRenderer>().enabled = true;
        gameObject.GetComponent<Collider>().enabled = true;
    }
}
