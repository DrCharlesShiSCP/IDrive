using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class EnemyTracker : MonoBehaviour
{
    public TextMeshProUGUI enemiesLeftText;
    public GameObject winMessage;
    public List<GameObject> enemies;

    private bool winShown;

    void Start()
    {
        enemies = new List<GameObject>();
        foreach (TankExplode tank in FindObjectsByType<TankExplode>(FindObjectsSortMode.None))
        {
            if (tank.CompareTag("Enemy"))
                enemies.Add(tank.gameObject);
        }

        if (winMessage != null)
            winMessage.SetActive(false);

        UpdateEnemiesLeftText();
    }

    void Update()
    {
        enemies.RemoveAll(enemy => enemy == null);
        UpdateEnemiesLeftText();

        if (!winShown && enemies.Count == 0)
        {
            winShown = true;
            if (winMessage != null)
                winMessage.SetActive(true);
        }
    }

    private void UpdateEnemiesLeftText()
    {
        if (enemiesLeftText != null)
            enemiesLeftText.text = enemies.Count.ToString();
    }
}
