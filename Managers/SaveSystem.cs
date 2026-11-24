// Standalone: no other scripts needed.
// Saves any [Serializable] class as JSON in Application.persistentDataPath.
//
// Usage:
//   [Serializable] public class PlayerData { public int coins; public int level; }
//
//   SaveSystem.Save("player", new PlayerData { coins = 100, level = 3 });
//   PlayerData data = SaveSystem.Load("player", new PlayerData());
//   SaveSystem.Delete("player");
//
// Note: JsonUtility cannot save a bare int / string / List. Put them inside a [Serializable] class.
// Obfuscation only stops casual editing of the file. It is NOT real security.
using System;
using System.IO;
using System.Text;
using UnityEngine;

public static class SaveSystem
{
    /// <summary>Hide the save file content from casual editing (Base64 + XOR).</summary>
    public static bool useObfuscation = true;

    /// <summary>Change this key for your game.</summary>
    public static string obfuscationKey = "ChangeMe";

    const string Extension = ".sav";

    static string GetPath(string fileName)
    {
        return Path.Combine(Application.persistentDataPath, fileName + Extension);
    }

    public static bool Exists(string fileName)
    {
        return File.Exists(GetPath(fileName));
    }

    public static bool Save<T>(string fileName, T data)
    {
        string path = GetPath(fileName);
        string tempPath = path + ".tmp";

        try
        {
            string json = JsonUtility.ToJson(data);
            if (useObfuscation)
                json = Obfuscate(json);

            // Write to a temp file first, so a crash during saving cannot corrupt the old save.
            File.WriteAllText(tempPath, json, Encoding.UTF8);
            if (File.Exists(path))
                File.Delete(path);
            File.Move(tempPath, path);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError("[SaveSystem] Save failed (" + fileName + "): " + e.Message);
            return false;
        }
    }

    /// <summary>Returns the saved data, or defaultValue if there is no save or it cannot be read.</summary>
    public static T Load<T>(string fileName, T defaultValue = default)
    {
        string path = GetPath(fileName);
        if (!File.Exists(path))
            return defaultValue;

        try
        {
            string json = File.ReadAllText(path, Encoding.UTF8);
            if (useObfuscation)
                json = Deobfuscate(json);

            T data = JsonUtility.FromJson<T>(json);
            return data != null ? data : defaultValue;
        }
        catch (Exception e)
        {
            Debug.LogError("[SaveSystem] Load failed (" + fileName + "): " + e.Message);
            return defaultValue;
        }
    }

    public static void Delete(string fileName)
    {
        string path = GetPath(fileName);
        if (File.Exists(path))
            File.Delete(path);
    }

    static string Obfuscate(string text)
    {
        return Convert.ToBase64String(Xor(Encoding.UTF8.GetBytes(text)));
    }

    static string Deobfuscate(string text)
    {
        return Encoding.UTF8.GetString(Xor(Convert.FromBase64String(text)));
    }

    static byte[] Xor(byte[] data)
    {
        byte[] key = Encoding.UTF8.GetBytes(string.IsNullOrEmpty(obfuscationKey) ? "key" : obfuscationKey);
        byte[] result = new byte[data.Length];
        for (int i = 0; i < data.Length; i++)
            result[i] = (byte)(data[i] ^ key[i % key.Length]);
        return result;
    }
}
