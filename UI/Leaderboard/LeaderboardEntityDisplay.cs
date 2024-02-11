using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class LeaderboardEntityDisplay : MonoBehaviour
{
    [SerializeField] private TMP_Text displayText;
    [SerializeField] private Color myColour;

    private FixedString32Bytes playerName;
    public ulong ClientId { get; private set; }
    public int Points { get; private set; }

    public void Initialise(ulong clientId, FixedString32Bytes playerName, int points)
    {
        this.ClientId = clientId;
        this.playerName = playerName;

        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            displayText.color = myColour;
        }

        UpdatePoints(points);
    }

    public void UpdateText()
    {
        displayText.text = $"{transform.GetSiblingIndex() + 1}. {playerName} ({Points})";
    }

    public void UpdatePoints(int points)
    {
        Points = points;
        UpdateText();
    }
}
