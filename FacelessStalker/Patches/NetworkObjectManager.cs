using Unity.Netcode;
using HarmonyLib;
using UnityEngine;

namespace SlendermanMod.Patches;

[HarmonyPatch]
public class NetworkObjectManager
{
    //[HarmonyPostfix, HarmonyPatch(typeof(GameNetworkManager), nameof(GameNetworkManager.Start))]
    [HarmonyPostfix, HarmonyPatch(typeof(GameNetworkManager), "Start")]
    public static void Init()
    {
        if (networkPrefab != null)
            return;

        //networkPrefab = (GameObject)slendermanassets.LoadAsset("SlendermanNetworkHandler");
        networkPrefab = (GameObject)Plugin.SlendermanAssets.LoadAsset("SlendermanNetworkHandler");
        networkPrefab.AddComponent<SlendermanNetworkHandler>();

        NetworkManager.Singleton.AddNetworkPrefab(networkPrefab);
    }

    //[HarmonyPostfix, HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.Awake))]
    [HarmonyPostfix, HarmonyPatch(typeof(StartOfRound), "Awake")]
    static void SpawnNetworkHandler()
    {
        if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
        {
            var networkHandlerHost = Object.Instantiate(networkPrefab, Vector3.zero, Quaternion.identity);
            networkHandlerHost.GetComponent<NetworkObject>().Spawn();
        }
    }

    static GameObject networkPrefab;

    [HarmonyPostfix, HarmonyPatch(typeof(RoundManager), nameof(RoundManager.GenerateNewFloor))]
    static void SubscribeToHandler()
    {
        SlendermanNetworkHandler.LevelEvent += ReceivedEventFromServer;
    }

    [HarmonyPostfix, HarmonyPatch(typeof(RoundManager), nameof(RoundManager.DespawnPropsAtEndOfRound))]
    static void UnsubscribeFromHandler()
    {
        SlendermanNetworkHandler.LevelEvent -= ReceivedEventFromServer;
    }

    static void ReceivedEventFromServer(string eventName)
    {
        // Event Code Here
    }

    static void SendEventToClients(string eventName)
    {
        if (!(NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer))
            return;

        SlendermanNetworkHandler.Instance.EventClientRpc(eventName);
    }
}
