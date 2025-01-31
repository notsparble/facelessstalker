using System.Diagnostics;
using UnityEngine;
using Unity.Netcode;
using NetworkPrefabs = LethalLib.Modules.NetworkPrefabs;
using static LethalLib.Modules.Enemies;
using JetBrains.Annotations;
using LethalLib.Modules;

namespace SlendermanMod.Behaviours
{
    internal class SpawnSlendermanEnemyItem : PhysicsProp
    {
        EnemyType slendermanEnemy = Plugin.SlendermanEnemy;

        private bool isSpawned = false;

        //private bool pageHasBeenUsed = false; stays on false for all pages for the rest of the round (other days as well) for whatever reason so this stays deactivated for now -> same for isSpawned

        public override void EquipItem()
        {
            base.EquipItem();

            /*if (isSpawned)
            {
                UnityEngine.Debug.Log("Slenderman already spawned in this round.");
                return;
            }*/

            if (playerHeldBy != null)
            {
                if (!StartOfRound.Instance.inShipPhase && RoundManager.Instance.currentLevel.spawnEnemiesAndScrap) // no scrap ? gordion;
                {
                    if (SlendermanEnemyAI.numSlendermanEnemiesInLevel <= 0 && !isSpawned) //&& !pageHasBeenUsed)
                    {
                        // There is no slenderman present
                        // Thanks Hamunii!
                        SpawnSlenderman();
                        isSpawned = true;
                    }
                    else
                    {
                        UnityEngine.Debug.Log("Slenderman not spawning as there's already one haunting the players or the page has already been used!");
                    }
                }
                else
                {
                    UnityEngine.Debug.Log("Slenderman not spawning as the ship is currently in orbit or on a safe moon!!");
                    return;
                }
            }
            else
            {
                UnityEngine.Debug.LogWarning("Player holding item == null!");
                return;
            }

            /*if (!pageHasBeenUsed)
            {
                pageHasBeenUsed = true;
                return;
            }*/
        }

        [ServerRpc(RequireOwnership = false)]
        public void SpawnSlendermanServerRpc(Vector3 spawnPosition)
        {
            if (!IsServer)
            {
                UnityEngine.Debug.LogWarning("SpawnSlendermanServerRpc should only be called on the server.");
                return;
            }

            UnityEngine.Debug.Log("SpawnSlendermanServerRpc called on the server.");

            if (isSpawned)
            {
                UnityEngine.Debug.LogWarning("Slenderman already spawned in this round.");
                return;
            }
            
            GameObject slendermanEnemyObject = Instantiate(slendermanEnemy.enemyPrefab, spawnPosition, Quaternion.identity);
            // Debugging
            if (slendermanEnemyObject != null)
            {
                slendermanEnemyObject.GetComponentInChildren<NetworkObject>().Spawn(destroyWithScene: true);
                UnityEngine.Debug.LogWarning("Slenderman Enemy spawned by page!");
            }
            else
            {
                return;
            }
        }

        public void SpawnSlenderman()
        {
            Vector3 spawnPosition = GameNetworkManager.Instance.localPlayerController.transform.position + Vector3.Scale(new Vector3(-5, 5, -5), GameNetworkManager.Instance.localPlayerController.transform.forward);
            SpawnSlendermanServerRpc(spawnPosition); // serverRpcParams will be filled in automatically
        }
    }
}