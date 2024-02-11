using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class Health : NetworkBehaviour
{
    [SerializeField] GameObject ui;
    [field: SerializeField] public int MaxHealth { get; private set; } = 100;

    public NetworkVariable<int> CurrentHealth = new NetworkVariable<int>();

    private bool isDead;

    public Action<Health> OnDie;

    [SerializeField] SimpleHealthBar healthBar;


    public override void OnNetworkSpawn()
    {
       
        if (!IsOwner)
        {
            Destroy(ui);
        }

        if (!IsServer) { return; }

        CurrentHealth.Value = MaxHealth;
    }

    public void Update()
    {
        healthBar.UpdateBar((int) CurrentHealth.Value, 100f);
    }

    public void TakeDamage(int damageValue)
    {
        ModifyHealth(-damageValue);
    }

    public void RestoreHealth(int healValue)
    {
        ModifyHealth(healValue);
    }

    private void ModifyHealth(int value)
    {
        if (isDead) { return; }
        int newHealth = CurrentHealth.Value + value;
        CurrentHealth.Value = Mathf.Clamp(newHealth, 0, MaxHealth);
        if (CurrentHealth.Value <= 0)
        {
            OnDie?.Invoke(this);
            isDead = true;
        }
    }
}
