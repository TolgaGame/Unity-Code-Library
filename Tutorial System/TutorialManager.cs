using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace FPS.Tutorial
{
    public class TutorialManager : PersistentSingleton<TutorialManager>
    {
        public GameObject step6Object;

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

        private const int InactiveStepIndex = -1;
        private int currentIndex = 0;
        private TutorialStep Current => currentIndex >= 0 && currentIndex < steps.Length ? steps[currentIndex] : null;
        private bool isWaitingForCompletion;
        private bool tutorialActive;
        private RectTransform activeMarker;
        private Coroutine autoSkipCoroutine;

        public static event Action OnTutorialComplete;

        /// <summary>Returns true if the tutorial was previously completed (PlayerPrefs).</summary>
        public static bool IsTutorialCompleted() => PlayerPrefs.GetInt(PREF_DONE, 0) == 1;

        protected override void Awake()
        {
            base.Awake();
            if (IsTutorialCompleted())
            {
                currentIndex = InactiveStepIndex;
                DestroyTutorialCanvas();
                Debug.Log("[TutorialManager] Tutorial was already completed, so all tutorial systems were deactivated.");
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
                Debug.Log("[TutorialManager] Tutorial already completed, skipping (PREF_DONE=1).");
                return;
            }

            if (mainCanvas == null || ui == null)
            {
                Debug.LogWarning("[TutorialManager] StartTutorial aborted: tutorial UI references are missing. The system may have been destroyed earlier.");
                return;
            }

            Debug.Log($"[TutorialManager] StartTutorial: total steps: {(steps != null ? steps.Length : 0)}, starting step [0]");
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
            Debug.Log($"[TutorialManager] AdvanceStep called: step [{currentIndex}] completed");
            currentIndex++;

            if (currentIndex >= steps.Length)
            {
                Debug.Log("[TutorialManager] All steps completed, calling EndTutorial");
                EndTutorial();
                return;
            }

            Debug.Log($"[TutorialManager] Starting step [{currentIndex}] \"{Current?.title}\" (completionType: {Current?.completionType})");
            PlayerPrefs.SetInt(PREF_STEP, currentIndex);
            PlayerPrefs.Save();

            ShowStep(Current);
        }

        public void SkipCurrentStep()
        {
            if (Current != null && Current.skippable)
            {
                Debug.Log($"[TutorialManager] Step [{currentIndex}] skipped");
                AdvanceStep();
            }
            else
                Debug.LogWarning($"[TutorialManager] Step [{currentIndex}] cannot be skipped.");
        }

        /// <summary>Call from gameplay code to notify the tutorial of an event.</summary>
        public void NotifyEvent(CompletionType eventType)
        {
            if (!isWaitingForCompletion || Current == null) return;
            if (Current.completionType == eventType)
            {
                CompleteCurrentStep();
            }
        }

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

            Debug.Log($"[TutorialManager] Step [{currentIndex}] \"{Current?.title}\" completed");
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
            Debug.Log("[TutorialManager] EndTutorial: tutorial completed, invoking OnTutorialComplete");
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
        /// Returns the index into the steps array (0, 1, 2, ...), or -1 when the tutorial is inactive.
        /// </summary>
        public int GetCurrentStepIndex()
        {
            Debug.Log($"[TutorialManager] GetCurrentStepIndex called: currentIndex: {currentIndex}");
            return currentIndex;
        }
    }
}