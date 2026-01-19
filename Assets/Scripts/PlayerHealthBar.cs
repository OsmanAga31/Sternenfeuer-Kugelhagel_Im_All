using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// manages the player's health bar UI display in a multiplayer environment
/// automatically finds and connects to the local player to display their health
/// </summary>
public class PlayerHealthBar : MonoBehaviour
{
    [SerializeField] private Image fillImage; // reference to the fill image component
    private PlayerController localPlayer; // reference to the local player's controller

    private void Start()
    {
        FindLocalPlayer();
    }

    /// <summary>
    /// Searches for the local player, sets up health bar updates, and retries if not found.
    /// </summary>
    private void FindLocalPlayer()
    {
        PlayerController[] allPlayers = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);

        foreach (PlayerController player in allPlayers)
        {

            if (player.IsOwner)
            {
                localPlayer = player;

                // subscribe to health change events
                localPlayer.OnHealthChanged += UpdateHealthBar;

                // initialize the health bar with the current health value 
                UpdateHealthBar(localPlayer.CurrentHP, localPlayer.CurrentHP);

                Debug.Log("ocal player found and health bar connected!");
                return;
            }
        }

        // if no local player was found, retry after a short delay
        // this handles cases where the player spawns after the UI initializes
        if (localPlayer == null)
        {
            Invoke(nameof(FindLocalPlayer), 0.5f);
        }
    }

    /// <summary>
    /// Updates the visual representation of the health bar based on current HP    
    /// </summary>
    private void UpdateHealthBar(int previousHP, int currentHP)
    {
        if (fillImage != null && localPlayer != null)
        {
            float fillAmount = (float)currentHP / localPlayer.MaxHP;
            // update fill image and clamp between 0 and 1
            fillImage.fillAmount = Mathf.Clamp01(fillAmount);

            Debug.Log($"Health Bar updated: {currentHP}/{localPlayer.MaxHP} = {fillAmount * 100}%");
        }
        else
        {
            Debug.LogError("fillImage oder localPlayer ist NULL!");
        }
    }

    /// <summary>
    /// Unsubscribes the UpdateHealthBar method from the localPlayer's OnHealthChanged event 
    /// when the object is destroyed.
    /// </summary>
    private void OnDestroy()
    {
        if (localPlayer != null)
        {
            localPlayer.OnHealthChanged -= UpdateHealthBar;
        }
    }
}
