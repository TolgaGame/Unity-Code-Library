using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace FPS.Tutorial
{
    
    public class TutorialManager : PersistentSingleton<TutorialManager>
    {
        public GameObject step6Object; 

        // ── PlayerPrefs Keys
        public const string PREF_DONE = "FPS_TutorialDone";
        public const string PREF_STEP = "FPS_TutorialStep";

        [Header("Steps")]
        [SerializeField] private TutorialStep[] steps;

        [Header("References")]
        [SerializeField] private TutorialUI ui;
        [SerializeField] private Transform playerTransform;

        [Header("Main Canvas")]
        public  Canvas mainCanvas;
        public  GraphicRaycaster mainRaycaster;

        // ── State 
        private const int InactiveStepIndex = -1;
        private int currentIndex = 0;
        private TutorialStep Current => currentIndex >= 0 && currentIndex < steps.Length ? steps[currentIndex] : null;
        private bool isWaitingForCompletion;
        private bool tutorialActive;
        private RectTransform activeMarker;
        private Coroutine autoSkipCoroutine;

        // External hooks — wire these up from your gameplay systems
        public static event Action OnTutorialComplete;

        /// <summary>Returns true if the tutorial was previously completed (PlayerPrefs).</summary>
        public static bool IsTutorialCompleted() => PlayerPrefs.GetInt(PREF_DONE, 0) == 1;

        //////////////////////////////////////////////////
    
        protected override void Awake() {
            base.Awake();
            if (IsTutorialCompleted())
            {
                currentIndex = InactiveStepIndex;
                DestroyTutorialCanvas();
                Debug.Log("[Tutorial] Tutorial daha once tamamlandigi icin tum tutorial sistemleri deaktif edildi.");
            }
            SetMainCanvas(false); // Force disable tutorial UI on start
        } 
 
        private void Update()
        {
            if (!tutorialActive || !isWaitingForCompletion || Current == null) 
                return;

            if (CheckCompletion(Current)) 
                CompleteCurrentStep();
        }

        public void StartTutorial()
        {
            if (IsTutorialCompleted())
            {
                currentIndex = InactiveStepIndex;
                Debug.Log("[Tutorial] Tutorial zaten tamamlanmış, atlanıyor (PREF_DONE=1).");
                return;
            }

            if (mainCanvas == null || ui == null)
            {
                Debug.LogWarning("[Tutorial] StartTutorial iptal edildi: tutorial UI referanslari yok. Sistem daha once destroy edilmis olabilir.");
                return;
            }

            Debug.Log($"[Tutorial] ▶ StartTutorial — toplam adım sayısı: {(steps != null ? steps.Length : 0)}, adım [0] başlatılıyor");
            tutorialActive = true;
            currentIndex = 0;
            SetMainCanvas(true);
            PlayerPrefs.SetInt(PREF_STEP, currentIndex);
            PlayerPrefs.Save();
            ShowStep(Current);
        }

        #region Step Control

        public void AdvanceStep()
        {
            Debug.Log($"[Tutorial] AdvanceStep çağrıldı → adım [{currentIndex}] tamamlandı");
            currentIndex++;

            if (currentIndex >= steps.Length) {
                Debug.Log("[Tutorial] ✔ Tüm adımlar tamamlandı → EndTutorial çağrılıyor");
                EndTutorial();
                return;
            }

            Debug.Log($"[Tutorial] → adım [{currentIndex}] \"{Current?.title}\" başlatılıyor (completionType: {Current?.completionType})");
            PlayerPrefs.SetInt(PREF_STEP, currentIndex);
            PlayerPrefs.Save();
        
            ShowStep(Current);
        }

        public void SkipCurrentStep()
        {
            if (Current != null && Current.skippable) {
                Debug.Log($"[Tutorial] ⏭ Adım [{currentIndex}] atlandı");
                AdvanceStep();
            }
            else
                Debug.LogWarning($"[Tutorial] Adım [{currentIndex}] atlanamaz.");

        }

        ///Call from gameplay code to notify the tutorial of an event
        public void NotifyEvent(CompletionType eventType)
        {
            if (!isWaitingForCompletion || Current == null) return;
            if (Current.completionType == eventType)
            {
                CompleteCurrentStep();
            }
        }

        // UI methods
        private void ShowStep(TutorialStep step)
        {
            isWaitingForCompletion = false;
            StopAutoSkip();
            DestroyActiveMarker();
            RectTransform targetUI = null;
            if (step.targetUI != null)
            {
                activeMarker = Instantiate(step.targetUI, mainCanvas.transform);
                activeMarker.gameObject.SetActive(true);
                targetUI = activeMarker;
            }

            ui.ShowStep(step, targetUI);
            isWaitingForCompletion = true;
        }

        private void StopAutoSkip()
        {
            if (autoSkipCoroutine != null)
            {
                StopCoroutine(autoSkipCoroutine);
                autoSkipCoroutine = null;
            }
        }

        private void DestroyActiveMarker()
        {
            if (activeMarker == null) return;
            Destroy(activeMarker.gameObject);
            activeMarker = null;
        }

        private void CompleteCurrentStep()
        {
            if (!isWaitingForCompletion) return;
            isWaitingForCompletion = false;
            StopAutoSkip();

            Debug.Log($"[Tutorial] ✔ Adım [{currentIndex}] \"{Current?.title}\" tamamlandı");
            AdvanceStep();
        }

        private void EndTutorial()
        {
            tutorialActive = false;
            currentIndex = InactiveStepIndex;
            StopAutoSkip();
            DestroyActiveMarker();
            SetMainCanvas(false);
            ui.Hide();
            PlayerPrefs.SetInt(PREF_DONE, 1);
            PlayerPrefs.Save();
            Debug.Log("[Tutorial] 🏁 EndTutorial — tutorial tamamlandı, OnTutorialComplete event'i tetikleniyor");
            OnTutorialComplete?.Invoke();
        }

        #endregion

        #region Utility

        private void SetMainCanvas(bool active)
        {
            if (mainCanvas != null) mainCanvas.enabled = active;
            if (mainRaycaster != null) mainRaycaster.enabled = active;
        }

        private void DestroyTutorialCanvas()
        {
            if (mainCanvas != null)
            {
                Destroy(mainCanvas.gameObject);
                mainCanvas = null;
            }

            mainRaycaster = null;
            ui = null;
            activeMarker = null;
        }

        private bool CheckCompletion(TutorialStep step)
        {
            switch (step.completionType)
            {
                // CompletionType.PressKey — mobilde klavye yok, devre dışı

                case CompletionType.ReachLocation:
                    if (step.targetObject == null || playerTransform == null) return false;
                    return Vector3.Distance(playerTransform.position, step.targetObject.transform.position) <= step.reachDistance;

                case CompletionType.LookAtObject:
                    return IsLookingAt(step.targetObject);

                case CompletionType.Manual:
                    return false; // Completed via NotifyEvent()

                // FireWeapon, EquipWeapon, EnterTrigger, KillEnemy → driven by NotifyEvent()
                default:
                    return false;
            }
        }

        private bool IsLookingAt(GameObject target)
        {
            if (target == null || Camera.main == null) return false;
            Vector3 dir = (target.transform.position - Camera.main.transform.position).normalized;
            return Vector3.Dot(Camera.main.transform.forward, dir) > 0.97f; // ~14° cone
        }

        #endregion

        /// <summary>
        /// steps[] dizisiyle birebir eşleşen adım indeksini döndürür (0, 1, 2, …).
        /// </summary>
        /// <returns></returns>
        public int GetCurrentStepIndex()
        {
            Debug.Log($"[Tutorial] GetCurrentStepIndex çağrıldı → currentIndex: {currentIndex}"); 
            return currentIndex;
        }

    }

}