// Tools/FixTextureCompressionDXT5_GameSourceSprites.cs
using UnityEditor;
using UnityEngine;

public class FixTextureCompressionDXT5_GameSourceSprites
{
    [MenuItem("Tools/Fix Sprites To DXT5")]
    static void FixSpritesInGameSourceFolder()
    {
        string targetFolder = "Assets/_GameSource/Sprites/GameplaySprites/CharacterSpritess";
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { targetFolder });
        int fixedCount = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;

            if (importer != null && importer.textureType == TextureImporterType.Sprite)
            {
                // Genel ayarlar
                importer.textureCompression = TextureImporterCompression.Compressed;
                importer.crunchedCompression = false;
                importer.mipmapEnabled = false;

                // PC (Standalone) override ayarları
                TextureImporterPlatformSettings pcSettings = importer.GetPlatformTextureSettings("Standalone");
                pcSettings.overridden = true;
                pcSettings.format = TextureImporterFormat.DXT5; // RGBA Compressed DXT5 (BC3)
                pcSettings.textureCompression = TextureImporterCompression.Compressed;
                pcSettings.crunchedCompression = false;
                pcSettings.maxTextureSize = 512;

                importer.SetPlatformTextureSettings(pcSettings);

                importer.SaveAndReimport();
                
                fixedCount++;
            }
        }

        Debug.Log($"✅ Fixed {fixedCount} sprite(s) in '{targetFolder}' to DXT5 (PC Override enabled).");
    }
}
