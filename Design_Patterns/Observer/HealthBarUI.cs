using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// C# event based Observer: the OBSERVER.
/// Updates a slider whenever PlayerHealth reports a change.
/// </summary>
public class HealthBarUI : MonoBehaviour
{
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private Slider slider;
    [SerializeField] private GameObject deathPanel;

    private void OnEnable()
    {
        if (playerHealth == null)
            return;

        playerHealth.OnHealthChanged += HandleHealthChanged;
        playerHealth.OnDied += HandleDied;
    }

    private void OnDisable()
    {
        // Every += needs a matching -= or the event keeps this object alive and calls a destroyed object.
        if (playerHealth == null)
            return;

        playerHealth.OnHealthChanged -= HandleHealthChanged;
        playerHealth.OnDied -= HandleDied;
    }

    private void HandleHealthChanged(int current, int max)
    {
        if (slider == null)
            return;

        slider.value = max > 0 ? (float)current / max : 0f;
    }

    private void HandleDied()
    {
        if (deathPanel != null)
            deathPanel.SetActive(true);
    }
}
