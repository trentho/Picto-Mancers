using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StatsUI : MonoBehaviour
{
    [SerializeField] Image healthBar;

    // Update is called once per frame
    void Update()
    {
        float health;
        if (MultiplayerManager.Instance.NetManager.IsHost)
        {
            health = GameManager.Instance.serverHealth.Value;
        }
        else
        {
            health = GameManager.Instance.clientHealth.Value;
        }
        healthBar.rectTransform.localScale = new Vector3(health / GameManager.Instance.maxHealth, 1, 1);

    }
}
