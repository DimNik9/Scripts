using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System;
using System.Linq;

public class Leaderboard : NetworkBehaviour
{
    [SerializeField] private Transform leaderboardEntityHolder;
    [SerializeField] private LeaderboardEntityDisplay leaderboardEntityPrefab;
    [SerializeField] private int entitiesToDisplay = 8;

    private NetworkList<LeaderboardEntityState> leaderboardEntities;
    private List<LeaderboardEntityDisplay> entityDisplays = new List<LeaderboardEntityDisplay>();

    private void Awake()
    {
        leaderboardEntities = new NetworkList<LeaderboardEntityState>();
    }

    public override void OnNetworkSpawn()
    {
        if (IsClient)
        {
            leaderboardEntities.OnListChanged += HandleLeaderboardEntitiesChanged;
            foreach (LeaderboardEntityState entity in leaderboardEntities)
            {
                HandleLeaderboardEntitiesChanged(new NetworkListEvent<LeaderboardEntityState>
                {
                    Type = NetworkListEvent<LeaderboardEntityState>.EventType.Add,
                    Value = entity
                });
            }
        }
        if (IsServer)
        {
            ThirdPersonController[] players = FindObjectsOfType<ThirdPersonController>();
            foreach (ThirdPersonController player in players)
            {
                HandlePlayerSpawned(player);
            }

            ThirdPersonController.OnPlayerSpawned += HandlePlayerSpawned;
            ThirdPersonController.OnPlayerDespawned += HandlePlayerDespawned;
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsClient)
        {
            leaderboardEntities.OnListChanged -= HandleLeaderboardEntitiesChanged;
        }
        if (IsServer)
        {
            ThirdPersonController.OnPlayerSpawned -= HandlePlayerSpawned;
            ThirdPersonController.OnPlayerDespawned -= HandlePlayerDespawned;
        }
    }

    private void HandleLeaderboardEntitiesChanged(NetworkListEvent<LeaderboardEntityState> changeEvent)
    {
        if (!gameObject.scene.isLoaded) { return; }
        switch (changeEvent.Type)
        {
            case NetworkListEvent<LeaderboardEntityState>.EventType.Add:
                if (!entityDisplays.Any(x => x.ClientId == changeEvent.Value.ClientId))
                {
                    LeaderboardEntityDisplay leaderboardEntity= Instantiate(leaderboardEntityPrefab, leaderboardEntityHolder);
                    leaderboardEntity.Initialise(changeEvent.Value.ClientId, changeEvent.Value.PlayerName, changeEvent.Value.Points);
                    entityDisplays.Add(leaderboardEntity);
                }         
                break;
            case NetworkListEvent<LeaderboardEntityState>.EventType.Remove:
                LeaderboardEntityDisplay displayToRemove =  entityDisplays.FirstOrDefault(x => x.ClientId == changeEvent.Value.ClientId);
                if (displayToRemove != null)
                {
                    displayToRemove.transform.SetParent(null);
                    Destroy(displayToRemove.gameObject);
                    entityDisplays.Remove(displayToRemove);
                }
                break;

            case NetworkListEvent<LeaderboardEntityState>.EventType.Value:
                LeaderboardEntityDisplay displayToUpdate = entityDisplays.FirstOrDefault(x => x.ClientId == changeEvent.Value.ClientId);
                if (displayToUpdate != null)
                {
                    displayToUpdate.UpdatePoints(changeEvent.Value.Points);
                }
                break;
        }

        entityDisplays.Sort((x, y) => y.Points.CompareTo(x.Points));

        for (int i = 0; i < entityDisplays.Count; i++)
        {
            entityDisplays[i].transform.SetSiblingIndex(i);
            entityDisplays[i].UpdateText();
            bool shouldShow = i <= entitiesToDisplay - 1;
            entityDisplays[i].gameObject.SetActive(shouldShow);
        }

        LeaderboardEntityDisplay myDisplay = entityDisplays.FirstOrDefault(x => x.ClientId == NetworkManager.Singleton.LocalClientId);

        if (myDisplay != null)
        {
            if (myDisplay.transform.GetSiblingIndex() >= entitiesToDisplay)
            {
                leaderboardEntityHolder.GetChild(entitiesToDisplay - 1).gameObject.SetActive(false);
                myDisplay.gameObject.SetActive(true);
            }
        }
    }

    private void HandlePlayerSpawned(ThirdPersonController player)
    {
            leaderboardEntities.Add(new LeaderboardEntityState
            {
                ClientId = player.OwnerClientId,
                PlayerName = player.PlayerName.Value,
                Points = 0
            }); ;
        
        player.CurrentPoints.OnValueChanged += (oldPoints, newPoints) =>
            HandlePointsChanged(player.OwnerClientId, newPoints);
    }

    private void HandlePlayerDespawned(ThirdPersonController player)
    {
        if (leaderboardEntities == null) return;

        foreach(LeaderboardEntityState entity in leaderboardEntities)
        {
            if (entity.ClientId != player.OwnerClientId)
            {
                continue;
            }
            leaderboardEntities.Remove(entity);
            break;
        }
        player.CurrentPoints.OnValueChanged -= (oldPoints, newPoints) =>
        HandlePointsChanged(player.OwnerClientId, newPoints);
    }

    private void HandlePointsChanged(ulong clientId, int newPoints)
    {
        for (int i = 0; i < leaderboardEntities.Count; i++)
        {
            if (leaderboardEntities[i].ClientId != clientId) 
            { 
                continue;
            }
            
            leaderboardEntities[i] = new LeaderboardEntityState
            {
                    ClientId = leaderboardEntities[i].ClientId,
                    PlayerName = leaderboardEntities[i].PlayerName,
                    Points = newPoints
                };
                return;
            }
        }
    }

