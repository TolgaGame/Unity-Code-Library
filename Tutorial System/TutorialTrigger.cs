using UnityEngine;

namespace FPS.Tutorial
{
    public class TutorialTrigger : MonoBehaviour
    {
        [SerializeField] private CompletionType eventType = CompletionType.EnterTrigger;
        [SerializeField] private string playerTag = "Player";

        // ── Static helper — call from any gameplay script ──────────────────
        public static void Notify(CompletionType type)
        {
            TutorialManager.Instance?.NotifyEvent(type);
        }

        // ── Trigger zone ───────────────────────────────────────────────────
        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag(playerTag)) return;
            Notify(eventType);
        }


    }
}


// ── Example integration snippets (not compiled — reference only) ───
//
// In your WeaponController.Fire():
//   TutorialTrigger.Notify(CompletionType.FireWeapon);
//
// In your Enemy.Die():
//   TutorialTrigger.Notify(CompletionType.KillEnemy);
//
// In your Inventory.Equip(weapon):
//   TutorialTrigger.Notify(CompletionType.EquipWeapon);

/// <summary>
/// 
/// Lightweight bridge between gameplay systems and TutorialManager.
///
/// ── Usage examples ──────────────────────────────────────────────
///
/// 1. TRIGGER ZONE  → Add this to a Collider (Is Trigger = true).
///    Set eventType = EnterTrigger. When the player enters, it fires.
///
/// 2. WEAPON SYSTEM → Call TutorialTrigger.Notify(CompletionType.FireWeapon)
///    from your fire method.
///
/// 3. ENEMY DEATH   → Call TutorialTrigger.Notify(CompletionType.KillEnemy)
///    from your enemy death handler.
///
/// 4. EQUIP WEAPON  → Call TutorialTrigger.Notify(CompletionType.EquipWeapon)
///    from your inventory / equip logic.
/// </summary>
