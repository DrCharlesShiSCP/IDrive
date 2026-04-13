using Unity.Netcode;
using UnityEngine;

[DisallowMultipleComponent]
public class NetworkGameOutcomeManager : NetworkBehaviour
{
    public static NetworkGameOutcomeManager Instance { get; private set; }

    private enum OutcomeStateValue
    {
        None = 0,
        Win = 1,
        Lose = 2
    }

    [SerializeField] private GameObject winTextObject;
    [SerializeField] private GameObject loseTextObject;
    [SerializeField] private float enemyPollInterval = 0.25f;

    public NetworkVariable<int> OutcomeState = new NetworkVariable<int>(
        (int)OutcomeStateValue.None,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private float nextEnemyPollTime;
    private bool hasSeenEnemies;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Multiple NetworkGameOutcomeManager instances found. Keeping the first instance.");
            enabled = false;
            return;
        }

        Instance = this;
        ApplyOutcome(OutcomeStateValue.None);
    }

    public override void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        base.OnDestroy();
    }

    public override void OnNetworkSpawn()
    {
        OutcomeState.OnValueChanged += OnOutcomeChanged;
        ApplyOutcome((OutcomeStateValue)OutcomeState.Value);
    }

    public override void OnNetworkDespawn()
    {
        OutcomeState.OnValueChanged -= OnOutcomeChanged;
    }

    private void Update()
    {
        if (!IsSpawned || !IsServer || OutcomeState.Value != (int)OutcomeStateValue.None)
            return;

        if (Time.time < nextEnemyPollTime)
            return;

        nextEnemyPollTime = Time.time + Mathf.Max(0.05f, enemyPollInterval);
        int enemiesRemaining = CountRemainingEnemies();

        if (enemiesRemaining > 0)
        {
            hasSeenEnemies = true;
            return;
        }

        if (hasSeenEnemies)
            ShowWinForAll();
    }

    public void ShowWinForAll()
    {
        if (IsServer)
            OutcomeState.Value = (int)OutcomeStateValue.Win;
    }

    public void ShowLoseForAll()
    {
        if (IsServer)
            OutcomeState.Value = (int)OutcomeStateValue.Lose;
    }

    [ServerRpc(RequireOwnership = false)]
    public void ShowLoseForAllServerRpc()
    {
        ShowLoseForAll();
    }

    [ContextMenu("Show Win For All")]
    private void ShowWinForAllFromContextMenu()
    {
        ShowWinForAll();
    }

    [ContextMenu("Show Lose For All")]
    private void ShowLoseForAllFromContextMenu()
    {
        ShowLoseForAll();
    }

    private static int CountRemainingEnemies()
    {
        int count = 0;
        TankExplode[] tanks = FindObjectsByType<TankExplode>(FindObjectsSortMode.None);

        foreach (TankExplode tank in tanks)
        {
            if (tank != null && !tank.HasExploded && tank.CompareTag("Enemy"))
                count++;
        }

        return count;
    }

    private void OnOutcomeChanged(int previousValue, int newValue)
    {
        ApplyOutcome((OutcomeStateValue)newValue);
    }

    private void ApplyOutcome(OutcomeStateValue outcome)
    {
        if (winTextObject != null)
            winTextObject.SetActive(outcome == OutcomeStateValue.Win);

        if (loseTextObject != null)
            loseTextObject.SetActive(outcome == OutcomeStateValue.Lose);
    }
}
