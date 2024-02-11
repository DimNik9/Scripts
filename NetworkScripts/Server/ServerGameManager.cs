using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Lobbies;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Services.Lobbies.Models;
using System.Text;
using Unity.Services.Authentication;
using Unity.Services.Matchmaker.Models;

public class ServerGameManager : IDisposable
{
    private string serverIP;
    private int serverPort;
    private int queryPort;
    private MatchplayBackfiller backfiller;
    public NetworkServer NetworkServer { get; private set; }

    private MultiplayAllocationService multiplayAllocationService;
    
    public ServerGameManager(string serverIP, int serverPort, int serverQPort, NetworkManager manager, NetworkObject playerPrefab)
    {
        this.serverIP = serverIP;
        this.serverPort = serverPort;
        this.queryPort = serverQPort;
        NetworkServer = new NetworkServer(manager, playerPrefab);
        multiplayAllocationService = new MultiplayAllocationService();
    }

    public async Task StartGameServerAsync()
    {
        await multiplayAllocationService.BeginServerCheck();
        try
        {
            MatchmakingResults matchmakerPayload = await GetMatchmakerPayload();
            if (matchmakerPayload != null)
            {
                await StartBackfill(matchmakerPayload);
                NetworkServer.OnUserJoined += UserJoined;
                NetworkServer.OnUserLeft += UserLeft;
            }else
            {
                Debug.LogWarning("Matchmaker payload timed out");
            }
        }
        catch(Exception e)
        {
            Debug.LogWarning(e);
        }
        if (!NetworkServer.OpenConnection(serverIP, serverPort))
        {
            Debug.LogWarning("Network Server did not start as expected");
            return;
        }
    }

    private async Task<MatchmakingResults> GetMatchmakerPayload()
    {
        Task<MatchmakingResults> matchmakerPayloadTask = multiplayAllocationService.SubscribeAndAwaitMatchmakerAllocation();
        if (await Task.WhenAny(matchmakerPayloadTask, Task.Delay(20000)) == matchmakerPayloadTask) {
            return matchmakerPayloadTask.Result;
        }
        return null;
    }
    private async Task StartBackfill(MatchmakingResults payload)
    {
        backfiller = new MatchplayBackfiller($"{serverIP}:{serverPort}", payload.QueueName, payload.MatchProperties, 20);

        if (backfiller.NeedsPlayers())
        {
            await backfiller.BeginBackfilling();
        }
        
    }

    private void UserJoined(UserData user)
    {
        backfiller.AddPlayerToMatch(user);
        multiplayAllocationService.AddPlayer();
        if (!backfiller.NeedsPlayers() && backfiller.IsBackfilling)
        {
            _ = backfiller.StopBackfill();
        }
    }    
    
    private void UserLeft(UserData user)
    {
        int playerCount = backfiller.RemovePlayerFromMatch(user.userAuthId);
        multiplayAllocationService.RemovePlayer();

        if (playerCount <= 0)
        {
            CloseServer();
            return;
        }

        if (backfiller.NeedsPlayers() && !backfiller.IsBackfilling)
        {
            _ = backfiller.BeginBackfilling();
        }
    }
    private async void CloseServer()
    {
        await backfiller.StopBackfill();
        Dispose();
        Application.Quit();
    }

    public void Dispose()
    {
        NetworkServer.OnUserJoined -= UserJoined;
        NetworkServer.OnUserLeft -= UserJoined;
        backfiller?.Dispose();
        multiplayAllocationService?.Dispose();
        NetworkServer?.Dispose();
    }
}
