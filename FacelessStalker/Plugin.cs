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
        const string VERSION = "1.0.2";

        public static Harmony _harmony;
        public static EnemyType SlendermanEnemy;
        public static Item page1Item;
        public static Item page2Item;
        public static Item page3Item;
        public static Item page4Item;
        internal static new ManualLogSource Logger;

        public static AssetBundle SlendermanAssets;

        public static SlendermanConfig SlendermanConfig { get; internal set; }

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
            Assets.PopulateAssets(); //Load slenderman asset bundle

            // Creating Config
            //Config = new(base.Config);
            SlendermanConfig = new SlendermanConfig(Config);

            NetcodePatcher(); // ONLY RUN ONCE

            // Load SlendermanEnemy
            SlendermanEnemy = Assets.SlendermanAssets.LoadAsset<EnemyType>("SlendermanEnemy");
            var slendermanTerminalNode = Assets.SlendermanAssets.LoadAsset<TerminalNode>("SlendermanEnemyTN");

            // Network Prefabs need to be registered first. See https://docs-multiplayer.unity3d.com/netcode/current/basics/object-spawning/
            NetworkPrefabs.RegisterNetworkPrefab(SlendermanEnemy.enemyPrefab);
            RegisterEnemy(SlendermanEnemy, 0, LevelTypes.All, SpawnType.Default, slendermanTerminalNode);

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

            // Plugin startup logic
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
        //public static ConfigEntry<int> configPageRarity; // ConfigEntry<string>, <bool>, <int>, etc
        //public static ConfigEntry<LevelTypes> configPageMoons;

        public readonly ConfigEntry<int> configPageRarity; // ConfigEntry<string>, <bool>, <int>, etc
        public readonly ConfigEntry<LevelTypes> configPageMoons;
        public SlendermanConfig(ConfigFile cfg)
        {
            InitInstance(this);
            configPageRarity = cfg.Bind(
                    "PageItem",                          // Config section
                    "Pages Spawn Weight",                     // Key of this config
                    15,                    // Default value
                    "The default spawn weight aka rarity of the page items. \nThe higher the value, the higher the chances of spawning. (0 = No Chances of Spawning, 100 = Very High Chances). \nNote that there are four separate page items and the value counts for every one of the four."    // Description
            );
            /*configPageMoons = cfg.Bind(
                    "PageItem.Moons",                          // Config section
                    "PAGE_MOONS",                     // Key of this config
                    "All",                    // Default value
                    "[UNUSED] \nThe moons pages can spawn on. \nDefault value is 'All', other possible values are 'Vanilla', 'Modded', 'None' and individual moons in the format of 'MoonLevel', like 'DineLevel', 'MarchLevel', 'ExperimentationLevel' etc. "    // Description
            );*/
            configPageMoons = cfg.Bind(
                "PageItem.Values",
                "Pages Spawn Level",
                LevelTypes.All, //LevelTypes.DineLevel,
                "The LevelTypes/Moons that the page items can spawn on. \nDefault value is 'All', other possible values are 'Vanilla', 'Modded', 'None' and individual moons in the format of 'MoonLevel', like 'DineLevel', 'MarchLevel', 'ExperimentationLevel' etc."
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
