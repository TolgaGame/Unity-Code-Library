using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UpgradeManager : MonoBehaviour
{

    [Header("BUTTONS")]
    [Space]
    public Button TankHPUpgradeButton;
    public Button TankSPEEDUpgradeButton;
    public Button TankGUNRELOADUpgradeButton;
    public Button SatelliteButton;

    [Header("TEXT")]
    [Space]
    public TextMeshProUGUI TankHPUpgradePriceText;
    public TextMeshProUGUI TankSPEEDUpgradePriceText;
    public TextMeshProUGUI TankGUNRELOADUpgradePriceText;
    public TextMeshProUGUI SatellitePriceText;

    public TextMeshProUGUI MoneyText;

    [Header("UPGRADE LEVELS")]
    [Space]
    private int _tankHPLevel;
    private int _tankSPEEDLevel;
    private int _tankGUNRELOADLevel;

    private int _upgradePrice;

    //////////////////////////////////////

    private void Start()
    {
        CheckUpgrades("TankHP");
        CheckUpgrades("TankSPEED");
        CheckUpgrades("TankGUNRELOAD");

        //// Satellite Check
        if (PlayerPrefs.HasKey("Satellite"))
        {
            SatelliteButton.interactable = false;
            SatellitePriceText.text = "BUYED";
        }
        else
        {
            SatelliteButton.interactable = true;
        }
    }

    /// CHECK UPGRADES LEVEL
    private void CheckUpgrades(string upgradeName)
    {
        if (PlayerPrefs.HasKey(upgradeName))
        {
            switch (upgradeName)
            {
                case "TankHP" :
                    _tankHPLevel = PlayerPrefs.GetInt(upgradeName);
                    TankHPUpgradePriceText.text = (_tankHPLevel * 100).ToString();
                    if (_tankHPLevel == 5)
                    {
                        TankHPUpgradeButton.interactable = false;
                        TankHPUpgradePriceText.text = "FULL";
                    }
                break;

                case "TankSPEED" :
                    _tankSPEEDLevel = PlayerPrefs.GetInt(upgradeName);
                    TankSPEEDUpgradePriceText.text = (_tankSPEEDLevel * 100).ToString();
                    if (_tankSPEEDLevel == 5)
                    {
                        TankSPEEDUpgradeButton.interactable = false;
                        TankSPEEDUpgradePriceText.text = "FULL";
                    }
                break;

                case "TankGUNRELOAD" :
                    _tankGUNRELOADLevel = PlayerPrefs.GetInt(upgradeName);
                    TankGUNRELOADUpgradePriceText.text = (_tankGUNRELOADLevel * 100).ToString();
                    if (_tankGUNRELOADLevel == 5)
                    {
                        TankGUNRELOADUpgradeButton.interactable = false;
                        TankGUNRELOADUpgradePriceText.text = "FULL";
                    }
                break;
            }
        }
        else // FIRST INIT UPGRADES
        {
            switch (upgradeName)
            {
                case "TankHP" :
                    _tankHPLevel = 1;
                    PlayerPrefs.SetInt(upgradeName, 1);
                    TankHPUpgradePriceText.text = "100";
                break;

                case "TankSPEED" :
                    _tankSPEEDLevel = 1;
                    PlayerPrefs.SetInt(upgradeName, 1);
                    TankSPEEDUpgradePriceText.text = "100";
                break;

                case "TankGUNRELOAD" :
                    _tankGUNRELOADLevel = 1;
                    PlayerPrefs.SetInt(upgradeName, 1);
                    TankGUNRELOADUpgradePriceText.text = "100";
                break;
            }
        }
    }

    /// Upgrade Button
    public void OnPressedUPGRADEButton(string upgradeName)
    {
        // Check Price
        switch (upgradeName)
        {
            case "TankHP" :
                _upgradePrice = int.Parse(TankHPUpgradePriceText.text);
            break;

            case "TankSPEED" :
                _upgradePrice = int.Parse(TankSPEEDUpgradePriceText.text);
            break;

            case "TankGUNRELOAD" :
                _upgradePrice = int.Parse(TankGUNRELOADUpgradePriceText.text);
            break;
        }

        // Check my balance and buy
        if (PlayerPrefs.GetInt("money") >= _upgradePrice)
        {
            Debug.Log("BUY PROCESS");

            PlayerPrefs.SetInt(upgradeName, (PlayerPrefs.GetInt(upgradeName) + 1));

            PlayerPrefs.SetInt("money", (PlayerPrefs.GetInt("money") - _upgradePrice));
            MoneyText.text = PlayerPrefs.GetInt("money").ToString();

            CheckUpgrades(upgradeName);
        }
        else
        {
            Debug.Log("NOT ENOUGH MONEY");
        }
    }

    /// Satellite Buy Button
    public void OnPressedSatelliteButton()
    {
        if (PlayerPrefs.GetInt("money") >= 2000)
        {
             Debug.Log("BUY PROCESS Satellite");

            PlayerPrefs.SetInt("Satellite", 1);

            PlayerPrefs.SetInt("money", (PlayerPrefs.GetInt("money") - 2000));
            MoneyText.text = PlayerPrefs.GetInt("money").ToString();

            SatelliteButton.interactable = false;
            SatellitePriceText.text = "BUYED";
        }
        else
        {
            Debug.Log("NOT ENOUGH MONEY");
        }
    }
}