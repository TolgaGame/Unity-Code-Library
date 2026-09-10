using UnityEngine;
using UnityEngine.UI;

// if Editor namespace is available, include it for custom inspector functionality
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

// Only compile this part if we're in the Unity Editor
#if UNITY_EDITOR
[CustomEditor(typeof(PanelShower))]
public class PanelShowerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Standart Inspector alanlarını (Canvas ve Raycaster slotlarını) çiz
        DrawDefaultInspector();

        PanelShower panelScript = (PanelShower)target;

        GUILayout.Space(15);
        EditorGUILayout.LabelField("Panels Comp.", EditorStyles.boldLabel);

        GUILayout.BeginHorizontal();

        // Show Butonu (Yeşil)
        GUI.backgroundColor = new Color(0.2f, 0.8f, 0.2f);
        if (GUILayout.Button("Show Panel", GUILayout.Height(30)))
        {
            Undo.RecordObject(panelScript.gameObject, "Show Panel");
            panelScript.Show();
            EditorUtility.SetDirty(panelScript);
        }

        // Hide Butonu (Kırmızı)
        GUI.backgroundColor = new Color(0.8f, 0.2f, 0.2f);
        if (GUILayout.Button("Hide Panel", GUILayout.Height(30)))
        {
            Undo.RecordObject(panelScript.gameObject, "Hide Panel");
            panelScript.Hide();
            EditorUtility.SetDirty(panelScript);
        }

        // Rengi sıfırla
        GUI.backgroundColor = Color.white;
        GUILayout.EndHorizontal();
    }
}
#endif