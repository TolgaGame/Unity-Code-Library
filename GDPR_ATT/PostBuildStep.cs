#if UNITY_EDITOR && UNITY_IOS
// Adds the ATT usage description to Info.plist automatically after every iOS build.
// Without NSUserTrackingUsageDescription the ATT prompt never appears and App Store review can reject the build.
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;

public class PostBuildStep
{
    // Shown to the user in the ATT prompt. Describe honestly why you track.
    private const string TrackingDescription =
        "Your data will be used to provide you a better and personalized ad experience.";

    [PostProcessBuild(0)]
    public static void OnPostProcessBuild(BuildTarget buildTarget, string pathToXcode)
    {
        if (buildTarget != BuildTarget.iOS)
            return;

        string plistPath = Path.Combine(pathToXcode, "Info.plist");
        var plist = new PlistDocument();
        plist.ReadFromString(File.ReadAllText(plistPath));

        // Keep a description the developer already set by hand.
        if (!plist.root.values.ContainsKey("NSUserTrackingUsageDescription"))
        {
            plist.root.SetString("NSUserTrackingUsageDescription", TrackingDescription);
        }

        File.WriteAllText(plistPath, plist.WriteToString());
    }
}
#endif
