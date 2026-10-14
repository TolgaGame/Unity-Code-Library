using System.Collections.Generic;
using UnityEngine;

public class FakeLeaderboardManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject userRankPrefab;
    [SerializeField] private Transform gridParent;

    [Header("Player")]
    private float playerScore;
    private const string playerUsername = "MyPlayer";

    [Header("Settings")]
    private int totalFakeUsers = 1200; // Includes the player
    // League thresholds: rank 1-100 = Platinum, 101-500 = Gold, 501-1000 = Silver, 1001+ = Bronze
    [SerializeField] private int rowsAbove = 4;
    [SerializeField] private int rowsBelow = 4;

    #region Internal data

    private struct LeaderboardEntry
    {
        public int rank;
        public string username;
        public float score;
        public bool isPlayer;
    }

    private readonly List<LeaderboardEntry> _sorted = new List<LeaderboardEntry>();

    #endregion

    #region Random name pool

    private static readonly string[] Nicknames =
    {
        "xX_Blaze_Xx","ShadowWolf","NeonFox","IronGhost","DarkMatter",
        "PixelKnight","StormRider","FrostByte","CryptoNova","VoidHunter",
        "TurboAce","NightOwl99","LaserEdge","GhostSniper","AquaZen",
        "CyberPulse","MindBender","TechWizard","SteelFang","LunaticAce",
        "RapidFire","SilentBlade","ZeroGrav","MegaBrain","ProdigyX",
        "CodeBreaker","QuantumLeap","HyperDrive","NovaStar","AlphaWave",
        "BrainStorm","FlashPoint","IceBreaker","ThunderBolt","PhantomX",
        "EchoStrike","AtomicRush","SonicEdge","CobaltFury","NightShift",
        "VortexPro","RedShift","DeltaForce","GlitchHunter","ZenMaster",
        "BlazeRunner","CrystalMind","OmegaPeak","NeonRacer","ShadowPulse",
        "DarkPulse","UltraVibe","MindSurge","GigaWave","ByteHunter",
        "CloudRacer","StarForge","DeepScan","NovaBolt","HexCoder",
        "IronClad","FusionX","PrimeUnit","ZeroPoint","CoreStrike",
        "BinaryGod","NightFury","SpeedDemon","GlitchKing","VoidWalker",
        "ChaosEdge","TitanAce","OracleX","SkyBreaker","PulseRider",
        "IceKing","ThunderAce","LightBolt","PhantomStar","CyberFox",
        "MasterMind","InfinityX","ArcLight","StealthByte","HyperFox",
        "NeonBlaze","QuantumFox","ShadowByte","IronStar","StormEdge",
        "RocketMind","CrystalBolt","DeltaBlaze","NebulaX","TurboWave"
    };

    #endregion

    #region Build the leaderboard once

    private void Start()
    {
        playerScore = Locator.Instance.GameManagerInstance.IQPoints;
        Debug.Log($"[FakeLeaderboardManager] Player IQ: {playerScore}");
        BuildLeaderboard();
    }

    private const float IQMin = 88.0f;
    private const float IQMax = 160.0f;

    private void BuildLeaderboard()
    {
        _sorted.Clear();

        System.Random rng = new System.Random(42); // Fixed seed keeps the leaderboard deterministic between sessions
        HashSet<string> usedNames = new HashSet<string>();

        int fakeTotal = totalFakeUsers - 1;

        float clampedPlayer = Mathf.Clamp(playerScore, IQMin, IQMax);

        float rangeAbove = IQMax - clampedPlayer;
        float rangeBelow = clampedPlayer - IQMin;

        // Distribute fake users proportionally, but guarantee at least rowsAbove above and rowsBelow below the player
        float totalRange = IQMax - IQMin;
        int rawAbove = Mathf.RoundToInt(fakeTotal * (rangeAbove / totalRange));
        int aboveCount = Mathf.Clamp(rawAbove, rowsAbove, fakeTotal - rowsBelow);
        int belowCount = fakeTotal - aboveCount;

        // Users with a higher IQ than the player
        if (aboveCount > 0)
        {
            float aboveMin = clampedPlayer + 0.1f;
            for (int i = 0; i < aboveCount; i++)
            {
                string name = GetUniqueName(rng, usedNames);
                float score = aboveMin + (float)rng.NextDouble() * (IQMax - aboveMin);
                score = Mathf.Clamp(Mathf.Round(score * 10f) / 10f, clampedPlayer + 0.1f, IQMax);
                _sorted.Add(new LeaderboardEntry { username = name, score = score, isPlayer = false });
            }
        }

        // Users with a lower IQ than the player
        if (belowCount > 0)
        {
            float belowMax = Mathf.Max(clampedPlayer - 0.1f, IQMin);
            for (int i = 0; i < belowCount; i++)
            {
                string name = GetUniqueName(rng, usedNames);
                float score;
                if (belowMax <= IQMin)
                {
                    score = IQMin; // No room below the player, pin to the floor
                }
                else
                {
                    score = IQMin + (float)rng.NextDouble() * (belowMax - IQMin);
                    score = Mathf.Clamp(Mathf.Round(score * 10f) / 10f, IQMin, belowMax);
                }
                _sorted.Add(new LeaderboardEntry { username = name, score = score, isPlayer = false });
            }
        }

        _sorted.Add(new LeaderboardEntry { username = playerUsername, score = clampedPlayer, isPlayer = true });

        // Descending by score; on a tie the player ranks above the fake users
        _sorted.Sort((a, b) => {
            int cmp = b.score.CompareTo(a.score);
            if (cmp != 0) return cmp;
            if (a.isPlayer && !b.isPlayer) return -1;
            if (!a.isPlayer && b.isPlayer) return 1;
            return 0;
        });

        for (int i = 0; i < _sorted.Count; i++)
        {
            var entry = _sorted[i];
            entry.rank = i + 1;
            _sorted[i] = entry;
        }

        Debug.Log($"[FakeLeaderboardManager] Leaderboard built with {aboveCount} above, {belowCount} below, player at rank {_sorted.FindIndex(e => e.isPlayer) + 1}");
    }

    private static string GetLeagueForRank(int rank)
    {
        if (rank <= 100) return "PLATINUM";
        if (rank <= 500) return "GOLD";
        if (rank <= 1000) return "SILVER";
        return "BRONZE";
    }

    private static string GetUniqueName(System.Random rng, HashSet<string> used)
    {
        string name;
        int tries = 0;
        do
        {
            string nick = Nicknames[rng.Next(Nicknames.Length)];
            int num = rng.Next(10, 99999);
            name = nick + num;
            tries++;
        } while (used.Contains(name) && tries < 500);

        used.Add(name);
        return name;
    }

    #endregion

    #region Public API

    /// <summary>
    /// Rebuilds the grid with the rows around the player and returns the player's league.
    /// </summary>
    public string ShowLeaderboard()
    {
        ClearGrid();

        int playerIndex = _sorted.FindIndex(e => e.isPlayer);
        if (playerIndex < 0) return string.Empty;

        string league = GetLeagueForRank(_sorted[playerIndex].rank);

        int startIndex = Mathf.Max(0, playerIndex - rowsAbove);
        int endIndex = Mathf.Min(_sorted.Count - 1, playerIndex + rowsBelow);

        for (int i = startIndex; i <= endIndex; i++)
        {
            LeaderboardEntry entry = _sorted[i];
            GameObject go = Instantiate(userRankPrefab, gridParent);

            UserRankItem item = go.GetComponent<UserRankItem>();
            if (item != null)
                item.Setup(entry.rank, entry.username, entry.score, entry.isPlayer);

            go.SetActive(true);
        }

        return league;
    }

    private void ClearGrid()
    {
        foreach (Transform child in gridParent)
        {
            if (child.gameObject != userRankPrefab)
                Destroy(child.gameObject);
        }
    }

    /// <summary>Sets the player's score (e.g. from the save system) and rebuilds the leaderboard.</summary>
    public void SetPlayerScore(float score)
    {
        playerScore = score;
        BuildLeaderboard();
    }

    #endregion
}