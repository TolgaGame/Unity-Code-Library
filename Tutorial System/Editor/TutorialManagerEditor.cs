#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace FPS.Tutorial.Editor
{
    public static class TutorialDevTools
    {
        private const string MENU_ROOT = "Tools/WolfTeen/Tutorial Dev/";

        [MenuItem(MENU_ROOT + "Clear Tutorial PlayerPrefs")]
        public static void ClearTutorialPrefs()
        {
            bool confirm = EditorUtility.DisplayDialog(
                "Clear Tutorial PlayerPrefs",
                $"The following keys will be deleted:\n\n" +
                $"  • {TutorialManager.PREF_DONE}\n" +
                $"  • {TutorialManager.PREF_STEP}\n\n" +
                "Continue?",
                "Yes, Delete",
                "Cancel");

            if (!confirm) return;

            PlayerPrefs.DeleteKey(TutorialManager.PREF_DONE);
            PlayerPrefs.DeleteKey(TutorialManager.PREF_STEP);
            PlayerPrefs.Save();

            Debug.Log("[TutorialDevTools] Tutorial PlayerPrefs cleared. " +
                      "The tutorial will run from the beginning when the game starts.");
        }

        [MenuItem(MENU_ROOT + "Force Mark as Completed")]
        public static void ForceMarkCompleted()
        {
            PlayerPrefs.SetInt(TutorialManager.PREF_DONE, 1);
            PlayerPrefs.Save();
            Debug.Log("[TutorialDevTools] Tutorial marked as completed.");
        }

        [MenuItem(MENU_ROOT + "Complete Current Step (Runtime)", false, 1)]
        public static void CompleteCurrentStep()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog(
                    "Play Mode Required",
                    "This option only works in Play Mode.\n\nStart the game first.",
                    "OK");
                return;
            }

            if (TutorialManager.Instance == null)
            {
                Debug.LogWarning("[TutorialDevTools] TutorialManager.Instance not found.");
                return;
            }

            int stepBefore = TutorialManager.Instance.GetCurrentStepIndex();
            TutorialManager.Instance.AdvanceStep();
            Debug.Log($"[TutorialDevTools] Step [{stepBefore}] counted as completed, moved to step [{TutorialManager.Instance.GetCurrentStepIndex()}].");
            if (stepBefore == 2)
            {
                GameObject.Find("SingUp").SetActive(false);
                TutorialManager.Instance.step6Object.SetActive(true);
            }
        }
    }
}
#endif
