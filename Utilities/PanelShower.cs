using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class PanelShower : MonoBehaviour
{
    [SerializeField] private Canvas panelCanvas;
    [SerializeField] private GraphicRaycaster panelRaycaster;

    public void Show()
    {
        if (panelCanvas != null) panelCanvas.enabled = true;
        if (panelRaycaster != null) panelRaycaster.enabled = true;
    }

    public void Hide()
    {
        if (panelCanvas != null) panelCanvas.enabled = false;
        if (panelRaycaster != null) panelRaycaster.enabled = false;
    }
}

// The custom inspector must not be compiled into player builds.
#if UNITY_EDITOR
[CustomEditor(typeof(PanelShower))]
public class PanelShowerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        PanelShower panelScript = (PanelShower)target;

        GUILayout.Space(15);
        EditorGUILayout.LabelField("Panel Controls", EditorStyles.boldLabel);

        GUILayout.BeginHorizontal();

        // Green show button
        GUI.backgroundColor = new Color(0.2f, 0.8f, 0.2f);
        if (GUILayout.Button("Show Panel", GUILayout.Height(30)))
        {
            Undo.RecordObject(panelScript.gameObject, "Show Panel");
            panelScript.Show();
            EditorUtility.SetDirty(panelScript);
        }

        // Red hide button
        GUI.backgroundColor = new Color(0.8f, 0.2f, 0.2f);
        if (GUILayout.Button("Hide Panel", GUILayout.Height(30)))
        {
            Undo.RecordObject(panelScript.gameObject, "Hide Panel");
            panelScript.Hide();
            EditorUtility.SetDirty(panelScript);
        }

        // Restore the default color so other GUI is not tinted
        GUI.backgroundColor = Color.white;
        GUILayout.EndHorizontal();
    }
}
#endif