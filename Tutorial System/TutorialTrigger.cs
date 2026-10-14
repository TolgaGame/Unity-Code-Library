using UnityEngine;

namespace FPS.Tutorial
{
    /// <summary>
    /// Bridge between gameplay systems and TutorialManager. Put it on a trigger collider,
    /// or call <see cref="Notify"/> from gameplay code (e.g. on weapon fire or enemy death).
    /// </summary>
    public class TutorialTrigger : MonoBehaviour
    {
        [SerializeField] private CompletionType eventType = CompletionType.EnterTrigger;
        [SerializeField] private string playerTag = "Player";

        public static void Notify(CompletionType type)
        {
            TutorialManager.Instance?.NotifyEvent(type);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag(playerTag)) return;
            Notify(eventType);
        }
    }
}
