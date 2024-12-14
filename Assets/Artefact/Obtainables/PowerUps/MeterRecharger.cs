using System.Collections;
using UnityEngine;

//simple script that boosts the player's meter on collision then respawns after a cooldown
public class MeterRecharger : MonoBehaviour
{
    [SerializeField] float amount;
    [SerializeField] float reSpawnCoolDown;
    
    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.GetComponent<PlayerController>())
        {
            PlayerController obtainer = other.gameObject.GetComponent<PlayerController>();
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
