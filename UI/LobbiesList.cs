using System.Collections;
using System.Collections.Generic;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;

public class LobbiesList : MonoBehaviour
{
    private bool isRefreshing;

    [SerializeField] private MainMenu mainMenu;
    [SerializeField] private LobbyItem lobbyItemPrefab;
    [SerializeField] private Transform lobbyItemParent;


    private void OnEnable()
    {
        RefreshList();
    }

    public async void RefreshList()
    {
        if (isRefreshing) { return; }

        isRefreshing = true;

        try
        {
            QueryLobbiesOptions options = new QueryLobbiesOptions();
            options.Count = 25;

            //Filters which lobbies will be displayed
            options.Filters = new List<QueryFilter>()
            {
                new QueryFilter(
                    field: QueryFilter.FieldOptions.AvailableSlots,
                    op: QueryFilter.OpOptions.GT,
                    value: "0"),
                new QueryFilter(
                    field: QueryFilter.FieldOptions.IsLocked,
                    op: QueryFilter.OpOptions.EQ,
                    value: "0")

            };

            QueryResponse lobbies = await Lobbies.Instance.QueryLobbiesAsync(options);

            //Destroys already existing lobby items
            foreach(Transform child in lobbyItemParent)
            {
                Destroy(child.gameObject);
            }

            //Spawn new lobby items
            foreach(Lobby lobby in lobbies.Results)
            {
                LobbyItem lobbyItem = Instantiate(lobbyItemPrefab, lobbyItemParent);
                lobbyItem.Initialise(this, lobby);
            }

        }
        catch(LobbyServiceException e)
        {
            Debug.Log(e);
        }


        isRefreshing = false;
    }

    public void JoinAsync(Lobby lobby)
    {
        mainMenu.JoinAsync(lobby);
    }
}
