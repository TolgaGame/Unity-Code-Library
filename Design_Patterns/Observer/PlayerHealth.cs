using System;
using UnityEngine;

/// <summary>
/// C# event based Observer: the SUBJECT.
/// The lightest version of the pattern. The event keyword is the observer list,
/// and `+=` / `-=` are Subscribe / Unsubscribe. No interfaces needed.
/// Use this when observers only need to react to a plain notification.
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private int maxHealth = 100;

    /// <summary>Arguments: current health, max health.</summary>
    public event Action<int, int> OnHealthChanged;

    /// <summary>Fires once when health reaches zero.</summary>
    public event Action OnDied;

    public int CurrentHealth { get; private set; }
    public int MaxHealth => maxHealth;
    public bool IsDead => CurrentHealth <= 0;

    private void Awake()
    {
        CurrentHealth = maxHealth;
    }

    private void Start()
    {
        // Let observers read the starting value.
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
    }

    public void TakeDamage(int amount)
    {
        if (IsDead || amount <= 0)
            return;

        CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);

        if (IsDead)
            OnDied?.Invoke();
    }

    public void Heal(int amount)
    {
        if (IsDead || amount <= 0)
            return;

        CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + amount);
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
    }
}
