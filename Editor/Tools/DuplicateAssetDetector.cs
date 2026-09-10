using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using Object = UnityEngine.Object;

// How it works
// For Textures: texture is downsampled to 32x32 pixels and then its pixels are used to calculate a hash value
// For other assets: the file's MD5 hash is calculated
// Assets that have the same hash value are considered to be the same. Since the algorithm is only hash-based, there might be rare false positives
public class DuplicateAssetDetector : EditorWindow, IHasCustomMenu
{
    [Serializable]
    private class DuplicateAssets
    {
        public class Comparer : IComparer<DuplicateAssets>
        {
            public bool sortByExtension;

            public int Compare(DuplicateAssets x, DuplicateAssets y)
            {
                if (sortByExtension)
                {
                    string extension1 = Path.GetExtension(x.paths[0]) ?? "";
                    string extension2 = Path.GetExtension(y.paths[0]) ?? "";

                    int comparison = extension1.CompareTo(extension2);
                    if (comparison != 0)
                        return comparison;
                }

                return x.paths[0].CompareTo(y.paths[0]);
            }
        }

        public List<string> paths = new List<string>();
    }

    [Serializable]
    private class SaveData
    {
        public List<DuplicateAssets> duplicates = new List<DuplicateAssets>();
    }

    private const string DUMMY_TEXTURE_PATH = "Assets/zzy_dummyy_texturee.png";
    private const string SAVE_FILE_PATH = "Library/DuplicateAssetDetector.json";
    private const float BUTTON_DRAG_THRESHOLD_SQR = 600f;

    private readonly MethodInfo instanceIDFromGUID = typeof(AssetDatabase).GetMethod("GetInstanceIDFromGUID", BindingFlags.NonPublic | BindingFlags.Static);

    private List<DuplicateAssets> duplicates = new List<DuplicateAssets>(); // This is not readonly so that it can be serialized

    private SearchField searchField;
    private string searchTerm;
    private readonly List<DuplicateAssets> searchResults = new List<DuplicateAssets>();

    private bool drawThumbnails = true;

    private double lastClickTime;
    private string lastClickedPath;

    private readonly GUIContent buttonGUIContent = new GUIContent();
    private Vector2 buttonPressPosition;
    private Vector2 scrollPos;

    [MenuItem("Top Race 3d/Duplicate Asset Detector")]
    private static void Init()
    {
        DuplicateAssetDetector window = GetWindow<DuplicateAssetDetector>();
        window.titleContent = new GUIContent("Duplicate Assets");
        window.minSize = new Vector2(200f, 150f);
        window.Show();
    }

    private void Awake()
    {
        LoadSession(null);
    }

    private void OnEnable()
    {
        searchField = new SearchField();
        RefreshSearch();
    }

    // Show additional options in the window's context menu
    public void AddItemsToMenu(GenericMenu menu)
    {
        if (duplicates.Count > 0)
            menu.AddItem(new GUIContent("Save To Clipboard"), false, () => GUIUtility.systemCopyBuffer = JsonUtility.ToJson(new SaveData() { duplicates = duplicates }, true));
        else
            menu.AddDisabledItem(new GUIContent("Save To Clipboard"));

        if (string.IsNullOrEmpty(GUIUtility.systemCopyBuffer))
            menu.AddDisabledItem(new GUIContent("Load From Clipboard"));
        else
        {
            menu.AddItem(new GUIContent("Load From Clipboard"), false, () =>
            {
                string json = GUIUtility.systemCopyBuffer;
                LoadSession(json);
                SaveSession(json); // If load succeeds, overwrite the saved session
            });
        }

        menu.AddSeparator("");

        menu.AddItem(new GUIContent("Draw Thumbnails"), drawThumbnails, () => drawThumbnails = !drawThumbnails);
    }

    private void OnGUI()
    {
        Event ev = Event.current;
        scrollPos = GUILayout.BeginScrollView(scrollPos);

        // Calculate duplicate assets
        if (GUILayout.Button("Refresh"))
        {
            try
            {
                double startTime = EditorApplication.timeSinceStartup;

                CalculateDuplicateAssets();
                SaveSession(null);

                Debug.Log("Refreshed in " + (EditorApplication.timeSinceStartup - startTime) + " seconds.");
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            finally
            {
                EditorUtility.ClearProgressBar();

                if (File.Exists(DUMMY_TEXTURE_PATH))
                    AssetDatabase.DeleteAsset(DUMMY_TEXTURE_PATH);
            }

            GUIUtility.ExitGUI();
        }

        // Draw found duplicate assets
        if (duplicates.Count > 0)
        {
            Color guiColor = GUI.color;
            EditorGUILayout.HelpBox("- Assets' import settings are ignored, you must check them manually\n- Double click a path to select the asset\n- Click a thumbnail to select all duplicates in that group\n- Right/middle click a path or thumbnail to hide it from the list", MessageType.Info);

            string _searchTerm = searchField.OnToolbarGUI(searchTerm);
            if (_searchTerm != searchTerm)
            {
                searchTerm = _searchTerm;
                RefreshSearch();

                GUIUtility.ExitGUI();
            }

            List<DuplicateAssets> duplicateAssets = string.IsNullOrEmpty(searchTerm) ? duplicates : searchResults;
            for (int i = 0; i < duplicateAssets.Count; i++)
            {
                List<string> paths = duplicateAssets[i].paths;

                if (!drawThumbnails)
                    GUILayout.Label("----------");
                else
                {
                    Texture icon = AssetDatabase.GetCachedIcon(paths[0]);
                    Rect rect = GUILayoutUtility.GetRect(24f, 24f);
                    GUI.DrawTexture(rect, icon ? icon : Texture2D.whiteTexture, ScaleMode.ScaleToFit);

                    GUI.color = Color.clear;

                    if (GUI.Button(rect, GUIContent.none))
                    {
                        if (ev.button == 0)
                        {
                            // Select all duplicate assets in this group
                            List<Object> allDuplicates = new List<Object>(paths.Count);
                            for (int j = 0; j < paths.Count; j++)
                            {
                                if (File.Exists(paths[j]))
                                    allDuplicates.Add(AssetDatabase.LoadMainAssetAtPath(paths[j]));
                            }

                            if (!ev.control && !ev.shift)
                                Selection.objects = allDuplicates.ToArray();
                            else
                            {
                                // While holding CTRL, either add all duplicate assets to current selection or remove them all from current selection
                                List<Object> selection = new List<Object>(Selection.objects);
                                bool allDuplicatesAreInSelection = true;
                                for (int j = 0; j < allDuplicates.Count; j++)
                                {
                                    if (!selection.Contains(allDuplicates[j]))
                                    {
                                        selection.Add(allDuplicates[j]);
                                        allDuplicatesAreInSelection = false;
                                    }
                                }

                                if (allDuplicatesAreInSelection)
                                {
                                    for (int j = 0; j < allDuplicates.Count; j++)
                                        selection.Remove(allDuplicates[j]);
                                }

                                Selection.objects = selection.ToArray();
                            }
                        }
                        else if (ev.button == 1)
                        {
                            // Show an option to hide these duplicate assets from the list
                            int _i = duplicates.IndexOf(duplicateAssets[i]);
                            GenericMenu menu = new GenericMenu();
                            menu.AddItem(new GUIContent("Hide"), false, () => HideDuplicateAssetGroup(_i));
                            menu.ShowAsContext();
                        }
                        else
                            HideDuplicateAssetGroup(duplicates.IndexOf(duplicateAssets[i]));
                    }

                    GUI.color = guiColor;
                }

                for (int j = 0; j < paths.Count; j++)
                {
                    // Buttons must support 1) click and 2) drag & drop. The most reliable way is to simulate GUILayout.Button from scratch
                    buttonGUIContent.text = paths[j];
                    Rect buttonRect = GUILayoutUtility.GetRect(buttonGUIContent, EditorStyles.textArea);
                    int buttonControlID = GUIUtility.GetControlID(FocusType.Passive);
                    switch (ev.GetTypeForControl(buttonControlID))
                    {
                        case EventType.MouseDown:
                            if (buttonRect.Contains(ev.mousePosition))
                            {
                                GUIUtility.hotControl = buttonControlID;
                                buttonPressPosition = ev.mousePosition;
                            }

                            break;
                        case EventType.MouseDrag:
                            if (GUIUtility.hotControl == buttonControlID && ev.button == 0 && (ev.mousePosition - buttonPressPosition).sqrMagnitude >= BUTTON_DRAG_THRESHOLD_SQR)
                            {
                                GUIUtility.hotControl = 0;

                                Object asset = AssetDatabase.LoadMainAssetAtPath(paths[j]);
                                if (asset)
                                {
                                    // Credit: https://forum.unity.com/threads/editor-draganddrop-bug-system-needs-to-be-initialized-by-unity.219342/#post-1464056
                                    DragAndDrop.PrepareStartDrag();
                                    DragAndDrop.objectReferences = new Object[] { asset };
                                    DragAndDrop.StartDrag("DuplicateAssetDetector");
                                }

                                ev.Use();
                            }

                            break;
                        case EventType.MouseUp:
                            if (GUIUtility.hotControl == buttonControlID)
                            {
                                GUIUtility.hotControl = 0;

                                if (buttonRect.Contains(ev.mousePosition))
                                {
                                    if (ev.button == 0 && File.Exists(paths[j]))
                                    {
                                        // Ping clicked duplicate asset
                                        double clickTime = EditorApplication.timeSinceStartup;
                                        if (clickTime - lastClickTime < 0.5f && lastClickedPath == paths[j])
                                        {
                                            if (!ev.control && !ev.shift)
                                                Selection.objects = new Object[] { AssetDatabase.LoadMainAssetAtPath(paths[j]) };
                                            else
                                            {
                                                // While holding CTRL, either add clicked asset to current selection or remove it from current selection
                                                Object asset = AssetDatabase.LoadMainAssetAtPath(paths[j]);
                                                List<Object> selection = new List<Object>(Selection.objects);
                                                if (!selection.Remove(asset))
                                                    selection.Add(asset);

                                                Selection.objects = selection.ToArray();
                                            }
                                        }
                                        else if (instanceIDFromGUID != null)
                                            EditorGUIUtility.PingObject((int)instanceIDFromGUID.Invoke(null, new object[] { AssetDatabase.AssetPathToGUID(paths[j]) }));
                                        else
                                            EditorGUIUtility.PingObject(AssetDatabase.LoadMainAssetAtPath(paths[j]));

                                        lastClickTime = clickTime;
                                        lastClickedPath = paths[j];
                                    }
                                    else if (ev.button == 1)
                                    {
                                        // Show an option to hide that duplicate asset from the list
                                        int _i = duplicates.IndexOf(duplicateAssets[i]), _j = j;
                                        GenericMenu menu = new GenericMenu();
                                        menu.AddItem(new GUIContent("Hide"), false, () => HideDuplicateAsset(_i, _j));
                                        menu.ShowAsContext();
                                    }
                                    else if (ev.button == 2)
                                        HideDuplicateAsset(duplicates.IndexOf(duplicateAssets[i]), j);
                                }
                            }
                            break;
                        case EventType.Repaint:
                            EditorStyles.textArea.Draw(buttonRect, buttonGUIContent, buttonControlID);
                            break;
                    }

                    if (ev.isMouse && GUIUtility.hotControl == buttonControlID)
                        ev.Use();
                }

                if (drawThumbnails)
                    GUILayout.Space(EditorGUIUtility.singleLineHeight);
            }
        }

        GUILayout.EndScrollView();
    }

    private void CalculateDuplicateAssets()
    {
        // Dummy Texture is used while calculating Textures' hashes
        CreateDummyTexture();

        // Key: hash value
        // Value: all assets that are sharing that hash
        Dictionary<string, List<string>> texturesHashLookup = new Dictionary<string, List<string>>(512);
        Dictionary<string, List<string>> genericHashLookup = new Dictionary<string, List<string>>(2048);

        duplicates.Clear();

        try
        {
            string[] paths = AssetDatabase.GetAllAssetPaths();
            string pathsLengthStr = paths.Length.ToString();
            float progressMultiplier = 1f / paths.Length;

            for (int i = 0; i < paths.Length; i++)
            {
                if (i % 30 == 0 && EditorUtility.DisplayCancelableProgressBar("Please wait...", string.Concat("Searching: ", (i + 1).ToString(), "/", pathsLengthStr), (i + 1) * progressMultiplier))
                    throw new Exception("Search aborted");

                if (string.IsNullOrEmpty(paths[i]) || !paths[i].StartsWith("Assets/") || paths[i] == DUMMY_TEXTURE_PATH)
                    continue;

                if (Directory.Exists(paths[i]))
                    continue;

                string hash;
                Dictionary<string, List<string>> hashLookup;

                Type assetType = AssetDatabase.GetMainAssetTypeAtPath(paths[i]);
                if (typeof(Texture).IsAssignableFrom(assetType))
                {
                    hash = CalculateTextureHash(paths[i]).ToString();
                    hashLookup = texturesHashLookup;
                }
                //else if( typeof( GameObject ).IsAssignableFrom( assetType ) && !paths[i].EndsWith( ".prefab" ) && AssetImporter.GetAtPath( paths[i] ) as ModelImporter )
                //{
                //	hash = CalculateModelHash( paths[i] ).ToString();
                //	hashLookup = modelsHashLookup;
                //}
                else
                {
                    hash = CalculateFileHash(paths[i]);
                    hashLookup = genericHashLookup;
                }

                List<string> hashMatch;
                if (!hashLookup.TryGetValue(hash, out hashMatch))
                {
                    hashMatch = new List<string>(1);
                    hashLookup[hash] = hashMatch;
                }

                hashMatch.Add(paths[i]);
            }
        }
        finally
        {
            FindDuplicatesInLookup(texturesHashLookup, false);
            FindDuplicatesInLookup(genericHashLookup, true);
        }
    }

    // Finds entries in the lookup table that have multiple paths (i.e. duplicates)
    private void FindDuplicatesInLookup(Dictionary<string, List<string>> lookup, bool sortByExtension)
    {
        int duplicatesPrevCount = duplicates.Count;

        foreach (var kvPair in lookup)
        {
            List<string> paths = kvPair.Value;
            if (paths.Count > 1)
            {
                paths.Sort();
                duplicates.Add(new DuplicateAssets() { paths = paths });
            }
        }

        // Sort each lookup results amongst themselves
        int count = duplicates.Count - duplicatesPrevCount;
        if (count > 1)
            duplicates.Sort(duplicatesPrevCount, count, new DuplicateAssets.Comparer() { sortByExtension = sortByExtension });
    }

    // Creates dummy Texture asset that will be used to generate Textures' hashes
    private void CreateDummyTexture()
    {
        if (!File.Exists(DUMMY_TEXTURE_PATH))
        {
            File.WriteAllBytes(DUMMY_TEXTURE_PATH, new Texture2D(2, 2).EncodeToPNG());
            AssetDatabase.ImportAsset(DUMMY_TEXTURE_PATH, ImportAssetOptions.ForceUpdate);
        }

        TextureImporter textureImporter = AssetImporter.GetAtPath(DUMMY_TEXTURE_PATH) as TextureImporter;
        textureImporter.maxTextureSize = 32;
        textureImporter.isReadable = true;
        textureImporter.filterMode = FilterMode.Point;
        textureImporter.mipmapEnabled = false;
        textureImporter.alphaSource = TextureImporterAlphaSource.FromInput;
        textureImporter.alphaIsTransparency = true;
        textureImporter.textureCompression = TextureImporterCompression.Uncompressed;
        textureImporter.SaveAndReimport();
    }

    // Refreshes search results
    private void RefreshSearch()
    {
        if (string.IsNullOrEmpty(searchTerm))
            return;

        CompareInfo ci = new CultureInfo("en-US").CompareInfo;
        searchResults.Clear();

        for (int i = 0; i < duplicates.Count; i++)
        {
            List<string> paths = duplicates[i].paths;
            for (int j = 0; j < paths.Count; j++)
            {
                // Case insensitive search (credit: https://stackoverflow.com/a/36254805/2373034)
                if (ci.IndexOf(paths[j], searchTerm, CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace) != -1)
                {
                    searchResults.Add(duplicates[i]);
                    break;
                }
            }
        }
    }

    // Calculates file's MD5 hash
    private string CalculateFileHash(string assetPath)
    {
        using (MD5 md5 = MD5.Create())
        using (FileStream stream = File.OpenRead(assetPath))
        {
            return BitConverter.ToString(md5.ComputeHash(stream));
        }
    }

    // Calculates downsized Texture's pixels' hash
    private long CalculateTextureHash(string texturePath)
    {
        File.Copy(texturePath, DUMMY_TEXTURE_PATH, true);
        AssetDatabase.ImportAsset(DUMMY_TEXTURE_PATH, ImportAssetOptions.ForceUpdate);

        Color32[] colors = AssetDatabase.LoadAssetAtPath<Texture2D>(DUMMY_TEXTURE_PATH).GetPixels32();

        unchecked
        {
            long hash = 0;
            for (int i = 0; i < colors.Length; i++)
            {
                long pixelValue = colors[i].r * 256 * 256 * 256 + colors[i].g * 256 * 256 + colors[i].b * 256 + colors[i].a;
                hash = (hash << 5) + hash ^ pixelValue;
            }

            return hash;
        }
    }

    // Calculates model's vertices hash
    // Not used because multiple FBX files may have shared vertices but different animation clips
    private long CalculateModelHash(string modelPath)
    {
        long hash = 17;
        Object[] subAssets = AssetDatabase.LoadAllAssetRepresentationsAtPath(modelPath);
        for (int i = 0; i < subAssets.Length; i++)
        {
            Mesh mesh = subAssets[i] as Mesh;
            if (!mesh)
                continue;

            Vector3[] vertices = mesh.vertices;
            unchecked
            {
                for (int j = 0; j < vertices.Length; j++)
                {
                    hash = hash * 23 + vertices[j].x.GetHashCode();
                    hash = hash * 23 + vertices[j].y.GetHashCode();
                    hash = hash * 23 + vertices[j].z.GetHashCode();
                }
            }
        }

        return hash;
    }

    // Hides the duplicate asset at the specified index
    private void HideDuplicateAsset(int groupIndex, int assetIndex)
    {
        if (duplicates[groupIndex].paths.Count <= 2)
        {
            int searchGroupIndex = searchResults.IndexOf(duplicates[groupIndex]);
            if (searchGroupIndex >= 0)
                searchResults.RemoveAt(searchGroupIndex);

            duplicates.RemoveAt(groupIndex);
        }
        else
            duplicates[groupIndex].paths.RemoveAt(assetIndex);

        SaveSession(null);
        Repaint();
    }

    // Hides all duplicate assets in the specified group
    private void HideDuplicateAssetGroup(int groupIndex)
    {
        int searchGroupIndex = searchResults.IndexOf(duplicates[groupIndex]);
        if (searchGroupIndex >= 0)
            searchResults.RemoveAt(searchGroupIndex);

        duplicates.RemoveAt(groupIndex);

        SaveSession(null);
        Repaint();
    }

    // Saves current session to file
    private void SaveSession(string json)
    {
        if (string.IsNullOrEmpty(json))
            json = JsonUtility.ToJson(new SaveData() { duplicates = duplicates }, false);

        File.WriteAllText(SAVE_FILE_PATH, json);
    }

    // Restores previous session
    private void LoadSession(string json)
    {
        if (string.IsNullOrEmpty(json))
        {
            if (!File.Exists(SAVE_FILE_PATH))
                return;

            json = File.ReadAllText(SAVE_FILE_PATH);
        }

        SaveData saveData = JsonUtility.FromJson<SaveData>(json);

        // Remove non-existent duplicates
        for (int i = saveData.duplicates.Count - 1; i >= 0; i--)
        {
            List<string> paths = saveData.duplicates[i].paths;
            for (int j = paths.Count - 1; j >= 0; j--)
            {
                if (!File.Exists(paths[j]))
                    paths.RemoveAt(j);
            }

            if (paths.Count <= 1)
                saveData.duplicates.RemoveAt(i);
        }

        duplicates = saveData.duplicates;
        Repaint();
    }
}