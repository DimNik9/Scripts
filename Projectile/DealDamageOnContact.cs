using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class DealDamageOnContact : MonoBehaviour
{
    [SerializeField] private int damage;
    private ulong ownerClientId;
    private GameObject parentObj;

    public void SetOwner(ulong ownerClientId, GameObject parent)
    {
        this.ownerClientId = ownerClientId;
        parentObj = parent;
    }

    private void OnCollisionEnter(Collision col)
    {
        if (col.gameObject == null) { return; }

        if (col.gameObject.TryGetComponent<NetworkObject>(out NetworkObject netObj))
        {
            if (ownerClientId == netObj.OwnerClientId)
            {
                return;
            }
        }

        if (col.gameObject.GetComponent<RayFire.RayfireRigid>())
        {
            col.gameObject.GetComponent<RayFire.RayfireRigid>().Demolish();
        }

        if (col.gameObject.TryGetComponent<Health>(out Health health))
        {
            health.TakeDamage(damage);
            parentObj.GetComponent<ThirdPersonController>().CurrentPoints.Value += 2;
            parentObj.GetComponent<Health>().CurrentHealth.Value += 2;
            int tmpPoints = col.gameObject.GetComponent<ThirdPersonController>().CurrentPoints.Value;
            tmpPoints--;
            if (tmpPoints <=0)
            {
                col.gameObject.GetComponent<ThirdPersonController>().CurrentPoints.Value = 0;
            }else
            {
                col.gameObject.GetComponent<ThirdPersonController>().CurrentPoints.Value = tmpPoints;
            }
        }
    }
}
