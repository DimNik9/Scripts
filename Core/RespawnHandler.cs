using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class RespawnHandler : NetworkBehaviour
{
    [SerializeField] private NetworkObject playerPrefab;
    [SerializeField] ThirdPersonController[] players;

    public override void OnNetworkSpawn()
    {
        if (!IsServer) { return; }

        players = FindObjectsOfType<ThirdPersonController>();
        foreach(ThirdPersonController player in players)
        {
            HandlePlayerSpawned(player);
        }

        ThirdPersonController.OnPlayerSpawned += HandlePlayerSpawned;
        ThirdPersonController.OnPlayerDespawned += HandlePlayerDespawned;
         
    }

    public override void OnNetworkDespawn()
    {
        if (!IsServer) { return; }

        ThirdPersonController.OnPlayerSpawned -= HandlePlayerSpawned;
        ThirdPersonController.OnPlayerDespawned -= HandlePlayerDespawned;
    }

    private void HandlePlayerSpawned(ThirdPersonController player)
    {
        
        player.Health.OnDie += (Health) => HandlePlayerDie(player);
    }

    private void HandlePlayerDespawned(ThirdPersonController player)
    {
        player.Health.OnDie -= (Health) => HandlePlayerDie(player);
    }

    private void HandlePlayerDie(ThirdPersonController player)
    {
        Destroy(player.gameObject);
        StartCoroutine(RespawnPlayer(player.OwnerClientId));
    }

    private IEnumerator RespawnPlayer(ulong ownerClientId)
    {
        yield return null;

        NetworkObject playerInstance = Instantiate(playerPrefab, SpawnPoint.GetRandomSpawnPos(), Quaternion.identity);

        playerInstance.SpawnAsPlayerObject(ownerClientId);
    }
}
