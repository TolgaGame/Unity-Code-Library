using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using System.Collections.Generic;

namespace FPS.Tutorial
{

    public class TutorialUI : PersistentSingleton<TutorialUI>
    {
        [Header("Overlay")]
        [SerializeField] private CanvasGroup overlayGroup;
        [SerializeField] private float fadeDuration = 0.4f;

        [Space]
        [Header("Content")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI speakerNameText;
        [SerializeField] private TextMeshProUGUI narratorText;
        
        [SerializeField] private Image speakerImage;
        [SerializeField] private GameObject narratorPanel;
        [SerializeField] private Button skipButton;

        [Space]
        [Header("Spotlight")]
        [SerializeField] private Color spotlightColor = new Color(0f, 0f, 0f, 0.78f);
        [SerializeField] private float spotlightPadding;
        private float spotlightOverlayAlpha = 0;

        // Private vars
        private float typewriterSpeed = 40f; // chars per second
        private float speakerSlideDuration = 0.35f;
        private float speakerSlideOffset = 400f;

        private Vector2 narratorBottomPos = new Vector2(0f,  0f);
        private Vector2 narratorTopPos    = new Vector2(0f, -75f);

        // ── State
        private Coroutine typewriterCoroutine;
        private TutorialStep currentStep;
        private Vector2 speakerRestPos;
        private Image[] spotPanels;
        private RectTransform canvasRT;
        private RectTransform narratorRT;

        ///////////////////////////////////////////////
       
        protected override void Awake()
        {
            base.Awake();

            if (overlayGroup) overlayGroup.alpha = 0f;
            if (narratorPanel) narratorPanel.SetActive(false);
            if (narratorPanel) narratorRT = narratorPanel.GetComponent<RectTransform>();
            if (speakerImage) speakerRestPos = speakerImage.rectTransform.anchoredPosition;
            if (skipButton) skipButton.onClick.AddListener(OnSkipClicked);

            canvasRT = GetComponentInParent<Canvas>().GetComponent<RectTransform>();
            BuildSpotlightPanels();
        }

        private void Start(){

        }

        #region Public

        public void ShowStep(TutorialStep step, RectTransform targetUI = null)
        {
            StartCoroutine(ShowStepCoroutine(step, targetUI));
        }
        
        private IEnumerator ShowStepCoroutine(TutorialStep step, RectTransform targetUI = null)
        {
            currentStep = step;
            TutorialManager.Instance.mainCanvas.enabled = false;
            TutorialManager.Instance.mainRaycaster.enabled = false;

            yield return new WaitForSeconds(step.transitionDelay);

            TutorialManager.Instance.mainCanvas.enabled = true;
            TutorialManager.Instance.mainRaycaster.enabled = true;

            // Overlay veya spotlight
            if (targetUI != null)
                ShowSpotlight(targetUI);
            else {
                HideSpotlight();
                SetFade(1f);

                if (overlayGroup) 
                    overlayGroup.blocksRaycasts = true;
            }

            // Info texts
            if (titleText) 
                titleText.text = step.GetTitle();

            // Skip button
            if (skipButton) 
                skipButton.gameObject.SetActive(step.skippable);

            // Narrator
            if (step.narratorPanelEnabled && !string.IsNullOrEmpty(step.narratorText))
            {
                if (narratorPanel) 
                    narratorPanel.SetActive(true);

                SetNarratorPosition(step.narratorPosition);

                if (speakerNameText) 
                    speakerNameText.text = step.speakerName;

                // Speaker image
                if (speakerImage != null)
                {
                    speakerImage.gameObject.SetActive(step.speakerImage != null);
                    if (step.speakerImage != null) {
                        speakerImage.sprite = step.speakerImage;
                        SlideInSpeaker();
                    }
                }

                StartTypewriter(step.GetNarratorText());
            }
            else {
                if (narratorPanel) 
                    narratorPanel.SetActive(false);
            }
        }

        public void ShowContinueIndicator(System.Action continueCallback) { }

        public void Hide()
        {
            StopTypewriter();
            HideSpotlight();
            if (overlayGroup) overlayGroup.blocksRaycasts = false;
            SetFade(0f, () => narratorPanel?.SetActive(false));
        }

        #endregion

        #region Typewriter

        private void StartTypewriter(string text)
        {
            StopTypewriter();
            typewriterCoroutine = StartCoroutine(TypewriterRoutine(text));
        }

        private void StopTypewriter()
        {
            if (typewriterCoroutine != null)
            {
                StopCoroutine(typewriterCoroutine);
                typewriterCoroutine = null;
            }
        }

        private IEnumerator TypewriterRoutine(string fullText)
        {
            if (narratorText) narratorText.text = "";
            float delay = 1f / typewriterSpeed;

            foreach (char c in fullText)
            {
                if (narratorText) narratorText.text += c;
                yield return new WaitForSeconds(delay);
            }

            typewriterCoroutine = null;

            if (currentStep != null && currentStep.autoSkip)
            {
                yield return new WaitForSeconds(2f);
                TutorialManager.Instance?.AdvanceStep();
            }
        }

        private void SlideInSpeaker()
        {
            if (speakerImage == null) return;
            RectTransform rt = speakerImage.rectTransform;
            rt.anchoredPosition = speakerRestPos + new Vector2(-speakerSlideOffset, 0f);
            rt.DOAnchorPos(speakerRestPos, speakerSlideDuration).SetEase(Ease.OutCubic);
        }

        private void SetNarratorPosition(NarratorPosition pos)
        {
            if (narratorRT == null) return;
            bool isBottom = pos == NarratorPosition.Bottom;
            float anchorY = isBottom ? 0f : 1f;
            narratorRT.anchorMin       = new Vector2(0.5f, anchorY);
            narratorRT.anchorMax       = new Vector2(0.5f, anchorY);
            narratorRT.pivot           = new Vector2(0.5f, anchorY);
            narratorRT.anchoredPosition = isBottom ? narratorBottomPos : narratorTopPos;
        }

        #endregion

        #region Spotlight

        private void BuildSpotlightPanels()
        {
            spotPanels = new Image[4];
            for (int i = 0; i < 4; i++)
            {
                var go = new GameObject($"_SpotPanel{i}", typeof(Image));
                go.transform.SetParent(canvasRT, false);
                go.transform.SetAsFirstSibling();
                var img = go.GetComponent<Image>();
                img.color = spotlightColor;
                img.raycastTarget = true;  // dış alanı blokla
                go.SetActive(false);
                spotPanels[i] = img;
            }
        }

        private void ShowSpotlight(RectTransform target)
        {
            // Hedefin dört köşesini canvas lokal uzayına çevir
            var corners = new Vector3[4];
            target.GetWorldCorners(corners);
            for (int i = 0; i < 4; i++)
                corners[i] = canvasRT.InverseTransformPoint(corners[i]);

            Rect cr = canvasRT.rect;
            float nxMin = Mathf.InverseLerp(cr.xMin, cr.xMax, corners[0].x - spotlightPadding);
            float nyMin = Mathf.InverseLerp(cr.yMin, cr.yMax, corners[0].y - spotlightPadding);
            float nxMax = Mathf.InverseLerp(cr.xMin, cr.xMax, corners[2].x + spotlightPadding);
            float nyMax = Mathf.InverseLerp(cr.yMin, cr.yMax, corners[2].y + spotlightPadding);

            ApplyPanel(spotPanels[0], 0,     nyMax, 1,     1);      // üst
            ApplyPanel(spotPanels[1], 0,     0,     1,     nyMin);   // alt
            ApplyPanel(spotPanels[2], 0,     nyMin, nxMin, nyMax);   // sol
            ApplyPanel(spotPanels[3], nxMax, nyMin, 1,     nyMax);   // sağ

            foreach (var p in spotPanels) p.gameObject.SetActive(true);

            // Ana overlay'i geri çek; 4 panel kararlığı üstleniyor
            if (overlayGroup)
            {
                overlayGroup.blocksRaycasts = false; // spotlight deliğindeki eleman tıklanabilsin
                overlayGroup.DOFade(spotlightOverlayAlpha, fadeDuration);
            }
        }

        private void HideSpotlight()
        {
            if (spotPanels == null) return;
            foreach (var p in spotPanels) p.gameObject.SetActive(false);
        }

        private static void ApplyPanel(Image panel, float xMin, float yMin, float xMax, float yMax)
        {
            var rt = panel.rectTransform;
            rt.anchorMin = new Vector2(xMin, yMin);
            rt.anchorMax = new Vector2(xMax, yMax);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        private void SetFade(float target, Action onComplete = null)
        {
            if (overlayGroup == null) { onComplete?.Invoke(); return; }
            overlayGroup.DOFade(target, fadeDuration)
                        .OnComplete(() => onComplete?.Invoke());
        }
        
        #endregion

        #region Buttons

        private void OnSkipClicked() => TutorialManager.Instance?.SkipCurrentStep();

        #endregion

        #region Localization

        private void OnEnable()
        {
            bl_Localization.Instance.SubscribeLanguageChange(OnLanguageChanged);
        }

        private void OnDisable()
        {
            bl_Localization.Instance.UnsubscribeLanguageChange(OnLanguageChanged);
        }

        private void OnLanguageChanged(Dictionary<string, string> lang)
        {
            if (currentStep == null) return;

            if (titleText)
                titleText.text = currentStep.GetTitle();

            // Typewriter hâlâ yazıyorsa yeniden başlat, bitmişse direkt yaz
            if (typewriterCoroutine != null)
                StartTypewriter(currentStep.GetNarratorText());
            else if (narratorText)
                narratorText.text = currentStep.GetNarratorText();
        }
        
        #endregion
    }

}