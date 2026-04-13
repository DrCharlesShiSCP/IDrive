using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

[DisallowMultipleComponent]
public class NetworkConnectionAndRoleUI : MonoBehaviour
{
    public string defaultAddress = "127.0.0.1";
    public string defaultPort = "7777";
    public bool showImGui = true;

    private string address;
    private string port;

    private void Awake()
    {
        address = defaultAddress;
        port = defaultPort;
    }

    private void Start()
    {
        foreach (string arg in System.Environment.GetCommandLineArgs())
        {
            if (arg == "-server" && NetworkManager.Singleton != null && !NetworkManager.Singleton.IsListening)
            {
                ApplyConnectionSettings();
                NetworkManager.Singleton.StartServer();
                return;
            }
        }
    }

    private void OnGUI()
    {
        if (!showImGui || NetworkManager.Singleton == null)
            return;

        if (!NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
        {
            DrawConnectionUi();
            return;
        }

        if (!NetworkManager.Singleton.IsClient)
            return;

        DrawRoleUi();
    }

    private void DrawConnectionUi()
    {
        float x = 12f;
        float y = 12f;
        float labelWidth = 70f;
        float fieldWidth = 150f;
        float buttonWidth = 90f;
        float rowHeight = 28f;

        GUI.Label(new Rect(x, y, labelWidth, rowHeight), "IP");
        address = GUI.TextField(new Rect(x + labelWidth, y, fieldWidth, rowHeight), address);

        y += rowHeight;
        GUI.Label(new Rect(x, y, labelWidth, rowHeight), "Port");
        port = GUI.TextField(new Rect(x + labelWidth, y, fieldWidth, rowHeight), port);

        y += rowHeight + 6f;
        if (GUI.Button(new Rect(x, y, buttonWidth, rowHeight), "Client"))
        {
            ApplyConnectionSettings();
            NetworkManager.Singleton.StartClient();
        }

        if (GUI.Button(new Rect(x + buttonWidth + 8f, y, buttonWidth, rowHeight), "Host"))
        {
            ApplyConnectionSettings();
            NetworkManager.Singleton.StartHost();
        }

        if (GUI.Button(new Rect(x + (buttonWidth + 8f) * 2f, y, buttonWidth, rowHeight), "Server"))
        {
            ApplyConnectionSettings();
            NetworkManager.Singleton.StartServer();
        }
    }

    private void DrawRoleUi()
    {
        TankCrewPlayer player = TankCrewPlayer.Local;
        TankCrewRoleManager manager = TankCrewRoleManager.Instance;

        if (player == null || manager == null)
        {
            GUI.Label(new Rect(12f, 12f, 360f, 28f), "Waiting for network player and role manager...");
            return;
        }

        if (player.AssignedRole != TankCrewRole.None)
            return;

        float x = 12f;
        float y = 12f;
        float width = 210f;
        float height = 30f;

        GUI.Label(new Rect(x, y, width * 2f, height), $"Current role: {player.AssignedRole}");
        y += height + 6f;

        DrawRoleButton(TankCrewRole.Commander, manager, player, x, y, width, height);
        y += height + 4f;
        DrawRoleButton(TankCrewRole.Driver, manager, player, x, y, width, height);
        y += height + 4f;
        DrawRoleButton(TankCrewRole.Spotter, manager, player, x, y, width, height);
        y += height + 4f;
        DrawRoleButton(TankCrewRole.FireControl, manager, player, x, y, width, height);
        y += height + 8f;

        if (GUI.Button(new Rect(x, y, width, height), "Leave Role"))
            player.ClearRole();
    }

    private static void DrawRoleButton(TankCrewRole role, TankCrewRoleManager manager, TankCrewPlayer player, float x, float y, float width, float height)
    {
        ulong clientId = manager.GetClientIdForRole(role);
        bool taken = clientId != TankCrewRoleManager.NoClient;
        bool mine = player.AssignedRole == role;
        string label = taken ? $"{role}: taken by {clientId}" : $"{role}: open";

        GUI.enabled = !taken || mine;
        if (GUI.Button(new Rect(x, y, width, height), label))
            player.RequestRole(role);
        GUI.enabled = true;
    }

    private void ApplyConnectionSettings()
    {
        UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        if (transport == null)
            return;

        if (!ushort.TryParse(port, out ushort parsedPort))
            parsedPort = 7777;

        transport.SetConnectionData(address, parsedPort);
    }
}
