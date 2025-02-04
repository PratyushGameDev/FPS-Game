using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class DaggerController : MonoBehaviour
{
    MeshCollider meshcollider;
    public PhotonView pV;
    public SingleShotGun daggerManager;

    private void Awake()
    {
        meshcollider = GetComponent<MeshCollider>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (daggerManager.canDaggerSwing == false && other.gameObject.transform != transform.root.gameObject && pV.IsMine)
        {

            if (other.gameObject != transform.root.gameObject)
            {
                if (!other.gameObject.CompareTag("Player"))
                {
                    other.gameObject.transform.root.gameObject.GetComponent<IDamageable>()?.TakeDamage(40);
                }
                if (other.transform.parent.transform.parent.GetComponent<ShieldManager>() != null)
                {
                    if (other.transform.parent.transform.parent.GetComponent<ShieldManager>().hasOpenedShield)
                    {
                        other.transform.parent.gameObject.GetComponent<ShieldManager>().TakeHit();
                    }
                }
            }

            StartCoroutine(nameof(DisableDagger));
        }
    }

    private IEnumerator DisableDagger()
    {
        meshcollider.enabled = false;
        yield return new WaitForSeconds(2);
        meshcollider.enabled = true;
    }
}
