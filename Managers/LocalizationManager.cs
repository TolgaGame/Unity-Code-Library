using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public class LocalizationManager : MonoBehaviour
{

    // Singleton instance
    public static LocalizationManager Instance { get; private set; }

    // Single (legacy) Localized String Table Reference (kept for backward compatibility)
    public LocalizedStringTable localizedStringTable;

    private const string LanguagePrefKey = "SelectedLanguage";

    //////////////////////////////////////////

    private void Awake()
    {
        // Singleton pattern implementation
        if (Instance == null) {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
            Destroy(gameObject);
     
    }

    private void Start()
    {
        LoadLanguage();
    }

    // GET LOCALIZE STRING (legacy - uses single assigned table)
    public string GetLocalizedString(string key)
    {
        if (localizedStringTable == null)
            return $"Table not assigned (legacy). Key: {key}";

        var localizedString = new LocalizedString
        {
            TableReference = localizedStringTable.TableReference,
            TableEntryReference = key
        };

    string localizedText = localizedString.GetLocalizedString();
    return string.IsNullOrEmpty(localizedText) ? $"Not found [{key}] (legacy table)" : localizedText;
    }

    // GET LOCALIZE STRING BY TABLE NAME
    public string GetLocalizedString(string tableName, string key)
    {
        if (string.IsNullOrWhiteSpace(tableName)) return "Table name empty";
        if (string.IsNullOrWhiteSpace(key)) return "Key empty";

        // Use the StringDatabase directly (avoids needing a LocalizedString wrapper)
        try
        {
            string result = LocalizationSettings.StringDatabase.GetLocalizedString(tableName, key, null, FallbackBehavior.UseProjectSettings);
            if (string.IsNullOrEmpty(result))
                return $"Not found [{key}] in table [{tableName}]";
            return result;
        }
        catch
        {
            return $"Error accessing table [{tableName}] key [{key}]";
        }
    }

    // OPTIONS MENU CHANGE LANG.
    public void ChangeLanguage(int languageIndex)
    {
        Locale newLocale = null;

        switch (languageIndex)
        {
            case 0:
                newLocale = LocalizationSettings.AvailableLocales.Locales.Find(locale => locale.Identifier.Code == "tr");
                break;
            case 1:
                newLocale = LocalizationSettings.AvailableLocales.Locales.Find(locale => locale.Identifier.Code == "en-US");
                break;
            case 2:
                newLocale = LocalizationSettings.AvailableLocales.Locales.Find(locale => locale.Identifier.Code == "fr");
                break;
            case 3:
                newLocale = LocalizationSettings.AvailableLocales.Locales.Find(locale => locale.Identifier.Code == "de");
                break;
            case 4:
                newLocale = LocalizationSettings.AvailableLocales.Locales.Find(locale => locale.Identifier.Code == "ja-JP");
                break;
            case 5:
                newLocale = LocalizationSettings.AvailableLocales.Locales.Find(locale => locale.Identifier.Code == "pt");
                break;
            case 6:
                newLocale = LocalizationSettings.AvailableLocales.Locales.Find(locale => locale.Identifier.Code == "ru");
                break;
            case 7:
                newLocale = LocalizationSettings.AvailableLocales.Locales.Find(locale => locale.Identifier.Code == "es-ES");
                break;
            case 8:
                newLocale = LocalizationSettings.AvailableLocales.Locales.Find(locale => locale.Identifier.Code == "cs-CZ");
                break;
            case 9:
                newLocale = LocalizationSettings.AvailableLocales.Locales.Find(locale => locale.Identifier.Code == "pl-PL");
                break;
            case 10:
                newLocale = LocalizationSettings.AvailableLocales.Locales.Find(locale => locale.Identifier.Code == "ko-KR");
                break;
            case 11:
                newLocale = LocalizationSettings.AvailableLocales.Locales.Find(locale => locale.Identifier.Code == "id-ID");
                break;
            case 12:
                newLocale = LocalizationSettings.AvailableLocales.Locales.Find(locale => locale.Identifier.Code == "zh-CN");
                break;

                
            default:
                Debug.LogError("Invalid language index!");
                return;
        }

        //// LOCALIZE CHANGE
        if (newLocale != null)
        {
            LocalizationSettings.SelectedLocale = newLocale;
            PlayerPrefs.SetInt(LanguagePrefKey, languageIndex);
            PlayerPrefs.Save();
            Debug.Log($"Language changed to: {newLocale.Identifier.Code}");
        }
        else
        {
            Debug.LogError("Locale not found!");
        }

    }

    // START CHECK LANG.
    private void LoadLanguage()
    {
        int languageIndex;
        if (PlayerPrefs.HasKey(LanguagePrefKey))
        {
            languageIndex = PlayerPrefs.GetInt(LanguagePrefKey);
        }
        else
        {
            languageIndex = 0; // Default to first language (Turkish)
            PlayerPrefs.SetInt(LanguagePrefKey, languageIndex);
        }

        ChangeLanguage(languageIndex);
    }

}