#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using FPS.Tutorial;

namespace FPS.Tutorial.Editor
{
    public static class TutorialDevTools
    {
        private const string MENU_ROOT = "Tools/WolfTeen/Tutorial Dev/";

        [MenuItem(MENU_ROOT + "Clear Tutorial PlayerPrefs")]
        public static void ClearTutorialPrefs()
        {
            bool confirm = EditorUtility.DisplayDialog(
                "Tutorial PlayerPrefs Temizle",
                $"Aşağıdaki key'ler silinecek:\n\n" +
                $"  • {TutorialManager.PREF_DONE}\n" +
                $"  • {TutorialManager.PREF_STEP}\n\n" +
                "Devam edilsin mi?",
                "Evet, Sil",
                "İptal");

            if (!confirm) return;

            PlayerPrefs.DeleteKey(TutorialManager.PREF_DONE);
            PlayerPrefs.DeleteKey(TutorialManager.PREF_STEP);
            PlayerPrefs.Save();

            Debug.Log("[TutorialDev] Tutorial PlayerPrefs temizlendi. " +
                      "Oyunu başlatınca tutorial baştan çalışır.");
        }

        [MenuItem(MENU_ROOT + "Force Mark as Completed")]
        public static void ForceMarkCompleted()
        {
            PlayerPrefs.SetInt(TutorialManager.PREF_DONE, 1);
            PlayerPrefs.Save();
            Debug.Log("[TutorialDev] Tutorial tamamlandı olarak işaretlendi.");
        }

        [MenuItem(MENU_ROOT + "Complete Current Step (Runtime)", false, 1)]
        public static void CompleteCurrentStep()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog(
                    "Play Mode Gerekli",
                    "Bu seçenek yalnızca Play Mode'da çalışır.\n\nÖnce oyunu başlatın.",
                    "Tamam");
                return;
            }

            if (TutorialManager.Instance == null)
            {
                Debug.LogWarning("[TutorialDev] TutorialManager.Instance bulunamadı.");
                return;
            }

            int stepBefore = TutorialManager.Instance.GetCurrentStepIndex();
            TutorialManager.Instance.AdvanceStep();
            Debug.Log($"[TutorialDev] Adım [{stepBefore}] tamamlandı sayıldı → adım [{TutorialManager.Instance.GetCurrentStepIndex()}]'e geçildi.");
            if (stepBefore == 2)
            {
                GameObject.Find("SingUp").SetActive(false);
                TutorialManager.Instance.step6Object.SetActive(true);
            }

        }
    }
}
#endif
