using Unity.Netcode;
using UnityEngine;

[DisallowMultipleComponent]
public class TankCrewPlayer : NetworkBehaviour
{
    public static TankCrewPlayer Local { get; private set; }

    public NetworkVariable<int> AssignedRoleValue = new NetworkVariable<int>(
        (int)TankCrewRole.None,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public TankCrewRole AssignedRole => (TankCrewRole)AssignedRoleValue.Value;

    public override void OnNetworkSpawn()
    {
        AssignedRoleValue.OnValueChanged += OnAssignedRoleChanged;

        if (IsOwner)
        {
            Local = this;
            ApplyLocalRole(AssignedRole);
        }
    }

    public override void OnNetworkDespawn()
    {
        AssignedRoleValue.OnValueChanged -= OnAssignedRoleChanged;

        if (Local == this)
            Local = null;
    }

    public void RequestRole(TankCrewRole role)
    {
        if (!IsOwner || TankCrewRoleManager.Instance == null)
            return;

        TankCrewRoleManager.Instance.RequestRoleServerRpc((int)role);
    }

    public void ClearRole()
    {
        RequestRole(TankCrewRole.None);
    }

    internal void SetRoleFromServer(TankCrewRole role)
    {
        if (!IsServer)
            return;

        AssignedRoleValue.Value = (int)role;
    }

    private void OnAssignedRoleChanged(int previous, int current)
    {
        if (IsOwner)
            ApplyLocalRole((TankCrewRole)current);
    }

    private static void ApplyLocalRole(TankCrewRole role)
    {
        LocalRoleCameraManager manager = LocalRoleCameraManager.Instance;
        if (manager != null)
            manager.ApplyRole(role);
    }
}
