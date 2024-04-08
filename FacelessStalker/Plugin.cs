using BepInEx;
using BepInEx.Configuration;
using GameNetcodeStuff;
using HarmonyLib;
using LethalLib.Modules;
using static LethalLib.Modules.Levels;
using static LethalLib.Modules.Enemies;
using BepInEx.Logging;
using System.IO;
using System.Reflection;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using NetworkPrefabs = LethalLib.Modules.NetworkPrefabs;
using SlendermanMod.Behaviours;

namespace SlendermanMod
{
    [BepInPlugin(GUID, NAME, VERSION)]
    [BepInDependency(LethalLib.Plugin.ModGUID)]
    public class Plugin : BaseUnityPlugin
    {
        const string GUID = "sparble.slendermanmod";
        const string NAME = "SlendermanMod";
        const string VERSION = "1.1.1";

        public static Harmony _harmony;
        public static EnemyType SlendermanEnemy;
        public static Item page1Item;
        public static Item page2Item;
        public static Item page3Item;
        public static Item page4Item;
        internal static new ManualLogSource Logger;
        public static AssetBundle SlendermanAssets;
        public static SlendermanConfig SlendermanConfig { get; internal set; }
        public static float slendermanHauntCooldown;
        public static float slendermanHauntIntervalLength;
        public static float slendermanStalkingIntervalLength;
        public static float slendermanChaseDuration;
        public static bool slendermanFlipsLightBreaker;
        public static bool slendermanPlaysSpawningSound;
        public static bool slendermanPlaysApproachingSound;
        public static int slendermanApproachingSoundChance;
        public static float slendermanVolume;

        // Preparing the mod for patching, UnityNetcodePatcher
        // Required by https://github.com/EvaisaDev/UnityNetcodePatcher
        private static void NetcodePatcher()
        {
            var types = Assembly.GetExecutingAssembly().GetTypes();
            foreach (var type in types)
            {
                var methods = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                foreach (var method in methods)
                {
                    var attributes = method.GetCustomAttributes(typeof(RuntimeInitializeOnLoadMethodAttribute), false);
                    if (attributes.Length > 0)
                    {
                        method.Invoke(null, null);
                    }
                }
            }
        }

        private void Awake()
        {
            Logger = base.Logger;
            Assets.PopulateAssets();

            // Creating Config
            SlendermanConfig = new SlendermanConfig(Config);

            NetcodePatcher(); // ONLY RUN ONCE

            // Load SlendermanEnemy
            SlendermanEnemy = Assets.SlendermanAssets.LoadAsset<EnemyType>("SlendermanEnemy");
            var slendermanTerminalNode = Assets.SlendermanAssets.LoadAsset<TerminalNode>("SlendermanEnemyTN");

            // Network Prefabs need to be registered first. See https://docs-multiplayer.unity3d.com/netcode/current/basics/object-spawning/
            NetworkPrefabs.RegisterNetworkPrefab(SlendermanEnemy.enemyPrefab);
            RegisterEnemy(SlendermanEnemy, SlendermanConfig.Instance.configSlendermanSpawnChances.Value, LevelTypes.All, SpawnType.Default, slendermanTerminalNode);

            // Loading the page items
            // Since I am too stupid to add texture variety to an item, I added 4 separate items
            int pageRarity = SlendermanConfig.configPageRarity.Value;

            Item page1Item = Assets.SlendermanAssets.LoadAsset<Item>("Assets/Items/page1Item.asset");
            SpawnSlendermanEnemyItem page1Script = page1Item.spawnPrefab.AddComponent<SpawnSlendermanEnemyItem>(); // Add custom interactions to pageItem
            page1Script.grabbable = true;
            page1Script.grabbableToEnemies = true;
            page1Script.itemProperties = page1Item;
            Utilities.FixMixerGroups(page1Item.spawnPrefab); //Fixes empty audio -- mixer lethallib.modules.
            NetworkPrefabs.RegisterNetworkPrefab(page1Item.spawnPrefab);
            Items.RegisterScrap(page1Item, SlendermanConfig.Instance.configPageRarity.Value, SlendermanConfig.Instance.configPageMoons.Value);

            Item page2Item = Assets.SlendermanAssets.LoadAsset<Item>("Assets/Items/page2Item.asset");
            SpawnSlendermanEnemyItem page2Script = page2Item.spawnPrefab.AddComponent<SpawnSlendermanEnemyItem>();
            page2Script.grabbable = true;
            page2Script.grabbableToEnemies = true;
            page2Script.itemProperties = page2Item;
            Utilities.FixMixerGroups(page2Item.spawnPrefab);
            NetworkPrefabs.RegisterNetworkPrefab(page2Item.spawnPrefab);
            Items.RegisterScrap(page2Item, pageRarity, Levels.LevelTypes.All);

            Item page3Item = Assets.SlendermanAssets.LoadAsset<Item>("Assets/Items/page3Item.asset");
            SpawnSlendermanEnemyItem page3Script = page3Item.spawnPrefab.AddComponent<SpawnSlendermanEnemyItem>();
            page3Script.grabbable = true;
            page3Script.grabbableToEnemies = true;
            page3Script.itemProperties = page3Item;
            Utilities.FixMixerGroups(page3Item.spawnPrefab);
            NetworkPrefabs.RegisterNetworkPrefab(page3Item.spawnPrefab);
            Items.RegisterScrap(page3Item, pageRarity, Levels.LevelTypes.All);

            Item page4Item = Assets.SlendermanAssets.LoadAsset<Item>("Assets/Items/page4Item.asset");
            SpawnSlendermanEnemyItem page4Script = page4Item.spawnPrefab.AddComponent<SpawnSlendermanEnemyItem>();
            page4Script.grabbable = true;
            page4Script.grabbableToEnemies = true;
            page4Script.itemProperties = page4Item;
            Utilities.FixMixerGroups(page4Item.spawnPrefab);
            NetworkPrefabs.RegisterNetworkPrefab(page4Item.spawnPrefab);
            Items.RegisterScrap(page4Item, pageRarity, Levels.LevelTypes.All);
            
            slendermanHauntCooldown = SlendermanConfig.Instance.configSlendermanHauntCooldown.Value;
            slendermanHauntIntervalLength = SlendermanConfig.Instance.configSlendermanAbsentIntervalRate.Value;
            slendermanStalkingIntervalLength = SlendermanConfig.Instance.configSlendermanStalkingIntervalRate.Value;
            slendermanChaseDuration = SlendermanConfig.Instance.configSlendermanChaseDuration.Value;
            slendermanFlipsLightBreaker = SlendermanConfig.Instance.configSlendermanLightBreaker.Value;
            slendermanVolume = SlendermanConfig.Instance.configSlendermanVolume.Value;
            slendermanPlaysSpawningSound = SlendermanConfig.Instance.configSlendermanPlaysSpawnSound.Value;
            slendermanPlaysApproachingSound = SlendermanConfig.Instance.configSlendermanPlaysApproachingSound.Value;
            slendermanApproachingSoundChance = SlendermanConfig.Instance.configSlendermanApproachingSoundChance.Value;

            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }
    }

    public static class Assets
    {
        public static AssetBundle SlendermanAssets = null;
        public static void PopulateAssets()
        {
            string sAssemblyLocation = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "slendermanbundle");
            
            SlendermanAssets = AssetBundle.LoadFromFile(sAssemblyLocation);
            if (SlendermanAssets == null)
            {
                Plugin.Logger.LogError("Failed to load slendermanbundle assets.");
                return;
            }
        }
    }

    [System.Serializable]
    public class SlendermanConfig : SyncedInstance<SlendermanConfig>
    {
        public readonly ConfigEntry<int> configPageRarity;
        public readonly ConfigEntry<LevelTypes> configPageMoons;
        public readonly ConfigEntry<int> configSlendermanSpawnChances;
        public readonly ConfigEntry<float> configSlendermanHauntCooldown;
        public readonly ConfigEntry<float> configSlendermanAbsentIntervalRate;
        public readonly ConfigEntry<float> configSlendermanStalkingIntervalRate;
        public readonly ConfigEntry<float> configSlendermanChaseDuration;
        public readonly ConfigEntry<float> configSlendermanVolume;
        public readonly ConfigEntry<bool> configSlendermanLightBreaker;
        public readonly ConfigEntry<bool> configSlendermanPlaysSpawnSound;
        public readonly ConfigEntry<bool> configSlendermanPlaysApproachingSound;
        public readonly ConfigEntry<int> configSlendermanApproachingSoundChance;
        public SlendermanConfig(ConfigFile cfg)
        {
            InitInstance(this);
            configPageRarity = cfg.Bind(
                    "PageItem",            // Config section
                    "Pages Spawn Weight",  // Key of this config
                    15,                    // Default value
                    "The default spawn weight aka rarity of the page items. \nThe higher the value, the higher the chances of spawning. (0 = No Chances of Spawning, 100 = Very High Chances). \nNote that there are four separate page items and the value counts for every one of the four."    // Description
            );
            configPageMoons = cfg.Bind(
                "PageItem.Values",
                "Pages Spawn Level",
                LevelTypes.All,
                "The LevelTypes/Moons that the page items can spawn on. \nDefault value is 'All', other possible values are 'Vanilla', 'Modded', 'None' and individual moons in the format of 'MoonLevel', like 'DineLevel', 'MarchLevel', 'ExperimentationLevel' etc."
            );
            configSlendermanSpawnChances = cfg.Bind(
                    "Slenderman",
                    "Natural Spawn Chances",
                    0,
                    "The chances of spawning Slenderman, independent from any pages. \nDefault value is 0, which will cause him to never spawn naturally and only spawn him when a page is picked up."    // Description
            );
            configSlendermanHauntCooldown = cfg.Bind(
                    "Slenderman.Behavior",
                    "Absent State Cooldown",
                    35.0f,
                    "The duration of the cooldown after first picking up a page, looking at slenderman and after a chase before Slenderman tries to spawn again (in seconds). \nThe higher the value, the more time the player has before Slenderman spawns in."    // Description
            );
            configSlendermanAbsentIntervalRate = cfg.Bind(
                    "Slenderman.Behavior",
                    "Absent State Interval Length",
                    15.0f,
                    "The duration of intervals of the Slenderman trying to find a haunting spot during his Absent state (in seconds). If he fails to find a spot, he will wait x seconds again before trying to find a spot again. \nThe higher the value, the longer the intervals are and the longer it will take for him to try to re-spawn. \nHigh numbers may result in him almost never spawning at all on small moons without many outside objects. (0 = Very frequent spawn intervals, 60 = Wait 60 seconds before trying to find a spawning spot again)."    // Description
            );
            configSlendermanStalkingIntervalRate = cfg.Bind(
                    "Slenderman.Behavior",
                    "Stalking State Interval Length",
                    20.0f,
                    "The duration of intervals (in seconds) of the Slenderman stalking the player before creeping closer - The value MUST be higher than 4.0 and it is recommended to not set this number too high or low. \nThe higher the value, the longer the intervals are and the longer it takes him to creep closer to the haunted player. \nHigh numbers mean the player has much time to spot Slenderman, while very low numbers mean they will have to constantly check their surroundings. (4.5 = Pretty much immediate chase, 60 = Wait 60 seconds before creeping closer)."    // Description
            );
            configSlendermanChaseDuration = cfg.Bind(
                    "Slenderman.Behavior",
                    "Chase Duration",
                    20.0f,
                    "The duration of a chase (in seconds)."
            );
            configSlendermanLightBreaker = cfg.Bind(
                    "Slenderman.Behavior",
                    "Light Breaker Flip",
                    false,
                    "Whether Slenderman should shut off the facility lights after being seen the first time."    // Description
            );
            configSlendermanPlaysSpawnSound = cfg.Bind(
                    "Slenderman.Behavior",
                    "Play Spawning Sound",
                    true,
                    "Whether the global spawning sound should be played after Slenderman spawned in the round (both naturally and through picking up a page)."    // Description
            );
            configSlendermanPlaysApproachingSound = cfg.Bind(
                    "Slenderman.Behavior",
                    "Play Approaching Sound",
                    true,
                    "Whether the approaching sound should be enabled for the haunted player."    // Description
            );
            configSlendermanApproachingSoundChance = cfg.Bind(
                    "Slenderman.Behavior",
                    "Approaching Sound Chance",
                    25,
                    "(Requires the 'Play Approaching Sound' option to be set to 'true') - The chance of Slenderman playing a static noise for the haunted player when creeping closer in % (must be between 0 and 100) - 0 means no sound will be played at all, 100 means for every interval he will play the sound 100%."
            );
            configSlendermanVolume = cfg.Bind(
                    "Slenderman.Volume",
                    "Slenderman Volume",
                    1.0f,
                    "The volume of the Slenderman enemy voice. 1.0 means 100% (default value), 0.8 means 80% etc."    // Description
            );
        }

        public static void RequestSync()
        {
            if (!IsClient) return;

            using FastBufferWriter stream = new(IntSize, Allocator.Temp);
            MessageManager.SendNamedMessage("ModName_OnRequestConfigSync", 0uL, stream);
        }

        public static void OnRequestSync(ulong clientId, FastBufferReader _)
        {
            if (!IsHost) return;

            Plugin.Logger.LogInfo($"Config sync request received from client: {clientId}");

            byte[] array = SerializeToBytes(Instance);
            int value = array.Length;

            using FastBufferWriter stream = new(value + IntSize, Allocator.Temp);

            try
            {
                stream.WriteValueSafe(in value, default);
                stream.WriteBytesSafe(array);

                MessageManager.SendNamedMessage("ModName_OnReceiveConfigSync", clientId, stream);
            }
            catch (System.Exception e)
            {
                Plugin.Logger.LogInfo($"Error occurred syncing config with client: {clientId}\n{e}");
            }
        }

        public static void OnReceiveSync(ulong _, FastBufferReader reader)
        {
            if (!reader.TryBeginRead(IntSize))
            {
                Plugin.Logger.LogError("Config sync error: Could not begin reading buffer.");
                return;
            }

            reader.ReadValueSafe(out int val, default);
            if (!reader.TryBeginRead(val))
            {
                Plugin.Logger.LogError("Config sync error: Host could not sync.");
                return;
            }

            byte[] data = new byte[val];
            reader.ReadBytesSafe(ref data, val);

            SyncInstance(data);

            Plugin.Logger.LogInfo("Successfully synced config with host.");
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlayerControllerB), "ConnectClientToPlayerObject")]
        public static void InitializeLocalPlayer()
        {
            if (IsHost)
            {
                MessageManager.RegisterNamedMessageHandler("SlendermanMod_OnRequestConfigSync", OnRequestSync);
                Synced = true;

                return;
            }

            Synced = false;
            MessageManager.RegisterNamedMessageHandler("SlendermanMod_OnReceiveConfigSync", OnReceiveSync);
            RequestSync();
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameNetworkManager), "StartDisconnect")]
        public static void PlayerLeave()
        {
            SlendermanConfig.RevertSync();
        }
    }
}
