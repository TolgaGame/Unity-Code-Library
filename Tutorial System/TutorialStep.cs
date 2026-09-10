using UnityEngine;

namespace FPS.Tutorial
{
    public enum CompletionType
    {
        ReachLocation,
        LookAtObject,
        FireWeapon,
        EquipWeapon,
        EnterTrigger,
        KillEnemy,
        UIElementInteract,   // InputField, Button vs. → TutorialTrigger.Notify() ile tetikle
        Manual
    }

    public enum NarratorPosition { Bottom, Top }

    [CreateAssetMenu(fileName = "TutorialStep", menuName = "FPS/Tutorial Step")]
    public class TutorialStep : ScriptableObject
    {
        [Header("Content")]
        public string title;
        [TextArea(2, 4)] public string narratorText;
       
        public string speakerName;
        public Sprite speakerImage;
       
        public NarratorPosition narratorPosition = NarratorPosition.Bottom;
        public bool narratorPanelEnabled = true;

        [Header("Focus")]
        public GameObject targetObject;
        public bool useWorldTracking = false;
        public RectTransform targetUI;

        [Header("Completion")]
        public CompletionType completionType = CompletionType.Manual;
        public float reachDistance;
       
        public bool autoSkip;
        public bool skippable;

        public float transitionDelay;

        [Header("Localization Keys")]
        public string titleKey;
        public string narratorTextKey;
        
        // Getter methods to fetch localized text based on keys
        public string GetTitle() => 
            bl_Localization.Instance.GetText(titleKey, title);

        public string GetNarratorText() => 
            bl_Localization.Instance.GetText(narratorTextKey, narratorText);

    }
}
