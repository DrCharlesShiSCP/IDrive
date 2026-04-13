using Unity.Netcode;
using UnityEngine;

[DisallowMultipleComponent]
public class TankCrewRoleManager : NetworkBehaviour
{
    public static TankCrewRoleManager Instance { get; private set; }

    public const ulong NoClient = ulong.MaxValue;

    public NetworkVariable<ulong> CommanderClientId = CreateSeat();
    public NetworkVariable<ulong> DriverClientId = CreateSeat();
    public NetworkVariable<ulong> SpotterClientId = CreateSeat();
    public NetworkVariable<ulong> FireControlClientId = CreateSeat();

    private static NetworkVariable<ulong> CreateSeat()
    {
        return new NetworkVariable<ulong>(
            NoClient,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer && NetworkManager != null)
            NetworkManager.OnClientDisconnectCallback += OnClientDisconnected;
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer && NetworkManager != null)
            NetworkManager.OnClientDisconnectCallback -= OnClientDisconnected;
    }

    public bool ClientHasRole(ulong clientId, TankCrewRole role)
    {
        return GetClientIdForRole(role) == clientId;
    }

    public ulong GetClientIdForRole(TankCrewRole role)
    {
        return role switch
        {
            TankCrewRole.Commander => CommanderClientId.Value,
            TankCrewRole.Driver => DriverClientId.Value,
            TankCrewRole.Spotter => SpotterClientId.Value,
            TankCrewRole.FireControl => FireControlClientId.Value,
            _ => NoClient
        };
    }

    public TankCrewRole GetRoleForClient(ulong clientId)
    {
        if (CommanderClientId.Value == clientId)
            return TankCrewRole.Commander;
        if (DriverClientId.Value == clientId)
            return TankCrewRole.Driver;
        if (SpotterClientId.Value == clientId)
            return TankCrewRole.Spotter;
        if (FireControlClientId.Value == clientId)
            return TankCrewRole.FireControl;
        return TankCrewRole.None;
    }

    public bool IsRoleTaken(TankCrewRole role)
    {
        return GetClientIdForRole(role) != NoClient;
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestRoleServerRpc(int requestedRoleValue, ServerRpcParams rpcParams = default)
    {
        TankCrewRole requestedRole = SanitizeRole(requestedRoleValue);
        ulong senderClientId = rpcParams.Receive.SenderClientId;

        if (requestedRole != TankCrewRole.None && IsRoleTaken(requestedRole))
            return;

        ReleaseClientRole(senderClientId);

        if (requestedRole != TankCrewRole.None)
            SetSeat(requestedRole, senderClientId);

        TankCrewPlayer player = GetPlayer(senderClientId);
        if (player != null)
            player.SetRoleFromServer(requestedRole);
    }

    private void OnClientDisconnected(ulong clientId)
    {
        ReleaseClientRole(clientId);
    }

    private void ReleaseClientRole(ulong clientId)
    {
        TankCrewRole role = GetRoleForClient(clientId);
        if (role == TankCrewRole.None)
            return;

        SetSeat(role, NoClient);

        TankCrewPlayer player = GetPlayer(clientId);
        if (player != null)
            player.SetRoleFromServer(TankCrewRole.None);
    }

    private void SetSeat(TankCrewRole role, ulong clientId)
    {
        switch (role)
        {
            case TankCrewRole.Commander:
                CommanderClientId.Value = clientId;
                break;
            case TankCrewRole.Driver:
                DriverClientId.Value = clientId;
                break;
            case TankCrewRole.Spotter:
                SpotterClientId.Value = clientId;
                break;
            case TankCrewRole.FireControl:
                FireControlClientId.Value = clientId;
                break;
        }
    }

    private TankCrewPlayer GetPlayer(ulong clientId)
    {
        if (NetworkManager == null || !NetworkManager.ConnectedClients.TryGetValue(clientId, out NetworkClient client))
            return null;

        return client.PlayerObject != null ? client.PlayerObject.GetComponent<TankCrewPlayer>() : null;
    }

    private static TankCrewRole SanitizeRole(int roleValue)
    {
        return roleValue >= (int)TankCrewRole.None && roleValue <= (int)TankCrewRole.FireControl
            ? (TankCrewRole)roleValue
            : TankCrewRole.None;
    }
}
