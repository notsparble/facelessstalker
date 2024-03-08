using UnityEngine;
using UnityEngine.AI;
using GameNetcodeStuff;
using Unity.Netcode;
using System.Collections;

namespace SlendermanMod
{
    class SlendermanEnemyAI : EnemyAI
    {
        public PlayerControllerB hauntingPlayer;

        public Renderer enemyMeshRenderer;

        public float timer;

        private float stalkingTimer;

        private float chaseTimer;

        private bool couldNotStareLastAttempt;

        private bool sitOutOneStare = false;

        private bool seenByPlayerThisTime;

        private bool disappearWithDelay;

        //private bool makeLightsFlicker;

        private bool enemyMeshEnabled;

        private bool vanishWithSound = false;

        private bool vanishWithJumpscare = false;

        private bool switchedHauntingPlayer = false;

        private int timesSeenByPlayer;

        public static int numSlendermanEnemiesInLevel = 0;

        private int timesCreptCloser;

        private bool creepingCloser = false;

        private float creepingCloserTimer = 4.0f;

        public GameObject[] outsideNodes;

        public NavMeshHit navHit;

        private Coroutine disappearOnDelayCoroutine;

        public Transform turnCompass;

        public AudioClip chaseSFX;

        public AudioClip ambienceSFX;

        public AudioClip disappearSFX;

        public AudioClip jumpscareSFX;

        private enum States
        {
            Absent = 0,
            Stalking = 1,
            Vanishing = 2,
            Chasing = 3
        }

        public override void Start()
        {
            base.Start();
            if (!IsServer) return;

            agent = GetComponent<NavMeshAgent>();
            if (agent == null)
            {
                UnityEngine.Debug.LogError("NavMeshAgent component not found on " + name);
            }
            agent.enabled = true;

            DisableMesh();
            //DisableEnemyMeshClientRpc();

            outsideNodes = GameObject.FindGameObjectsWithTag("OutsideAINode");
            ChoosePlayerToHaunt();
            navHit = default(NavMeshHit);
            UnityEngine.Debug.Log("Slenderman spawned in Level.");
            //numSlendermanEnemiesInLevel++;
            SyncNumSlendermanEnemiesInLevelClientRpc(1);
            TeleportAway();
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            if (!IsServer) return;
            SyncNumSlendermanEnemiesInLevelClientRpc(-1);
        }

        private void ChoosePlayerToHaunt()
        {
            if (!IsServer) return;

            SyncTimesSeenByPlayerClientRpc(0);
            //timesSeenByPlayer = 0; //Reset stare malus
            float num = 0f;
            float num2 = 0f;
            int num3 = 0;
            int num4 = 0;
            for (int i = 0; i < 4; i++)
            {
                if (StartOfRound.Instance.gameStats.allPlayerStats[i].turnAmount > num3)
                {
                    num3 = StartOfRound.Instance.gameStats.allPlayerStats[i].turnAmount;
                    num4 = i;
                }
                if (StartOfRound.Instance.allPlayerScripts[i].insanityLevel > num)
                {
                    num = StartOfRound.Instance.allPlayerScripts[i].insanityLevel;
                    num2 = i;
                }
            }
            int[] array = new int[4];
            for (int j = 0; j < 4; j++)
            {
                if (!StartOfRound.Instance.allPlayerScripts[j].isPlayerControlled)
                {
                    array[j] = 0;
                    continue;
                }
                array[j] += 80;
                if (num2 == (float)j && num > 1f)
                {
                    array[j] += 50;
                }
                if (num4 == j)
                {
                    array[j] += 30;
                }
                if (!StartOfRound.Instance.allPlayerScripts[j].hasBeenCriticallyInjured)
                {
                    array[j] += 10;
                }
                if (StartOfRound.Instance.allPlayerScripts[j].currentlyHeldObjectServer != null && StartOfRound.Instance.allPlayerScripts[j].currentlyHeldObjectServer.scrapValue > 100)
                {
                    array[j] += 30;
                }
            }
            hauntingPlayer = StartOfRound.Instance.allPlayerScripts[RoundManager.Instance.GetRandomWeightedIndex(array)];
            if (hauntingPlayer.isPlayerDead)
            {
                for (int k = 0; k < StartOfRound.Instance.allPlayerScripts.Length; k++)
                {
                    if (!StartOfRound.Instance.allPlayerScripts[k].isPlayerDead)
                    {
                        hauntingPlayer = StartOfRound.Instance.allPlayerScripts[k];
                        break;
                    }
                }
            }
            switchedHauntingPlayer = false;
            HandleChangeHauntingPlayerClientRpc((int)hauntingPlayer.playerClientId);
            //UnityEngine.Debug.Log($"Slenderman: Haunting player with playerClientId: {hauntingPlayer.playerClientId}; actualClientId: {hauntingPlayer.actualClientId}");
        }

        private Vector3 TryFindingHauntPosition(bool mustBeInLOS = true)
        {
            if (hauntingPlayer.isInsideFactory)
            {
                for (int i = 0; i < allAINodes.Length; i++)
                {
                    // Not within 20 units of the target & Not in LOS:
                    // Target must either be looking more than 60 degrees away from the spot, or be more than 100 units away, or certain nonsolid foliage materials may break the line of sight for this check while not being considered for the previous line of sight check.
                    if ((!mustBeInLOS || !Physics.Linecast(hauntingPlayer.gameplayCamera.transform.position, allAINodes[i].transform.position, StartOfRound.Instance.collidersAndRoomMaskAndDefault)) && !hauntingPlayer.HasLineOfSightToPosition(allAINodes[i].transform.position, 60f, 100, 20f))
                    {
                        //UnityEngine.Debug.DrawLine(hauntingPlayer.gameplayCamera.transform.position, allAINodes[i].transform.position, Color.green, 2f);
                        //UnityEngine.Debug.Log($"Player distance to inside Slenderman haunt position: {Vector3.Distance(hauntingPlayer.transform.position, allAINodes[i].transform.position)}");
                        SetStalkingPosition(allAINodes[i].transform.position);
                        return allAINodes[i].transform.position;
                    }
                }
            }
            //If player is NOT inside facility, spawn outside of the facility
            // Not within 60 units of the target & Not in LOS:
            // Target must either be looking more than 70 (Original 80) degrees away from the spot, or be more than 100 units away, or certain nonsolid foliage materials may break the line of sight for this check while not being considered for the previous line of sight check.
            else if (!hauntingPlayer.isInsideFactory)
            {
                for (int j = 0; j < outsideNodes.Length; j++)
                {
                    if ((!mustBeInLOS || !Physics.Linecast(hauntingPlayer.gameplayCamera.transform.position, outsideNodes[j].transform.position, StartOfRound.Instance.collidersAndRoomMaskAndDefault)) && !hauntingPlayer.HasLineOfSightToPosition(outsideNodes[j].transform.position, 70f, 100, 60f))
                    {
                        //UnityEngine.Debug.Log($"Player distance to outside Slenderman haunt position: {Vector3.Distance(hauntingPlayer.transform.position, allAINodes[j].transform.position)}");
                        SetStalkingPosition(allAINodes[j].transform.position);
                        return outsideNodes[j].transform.position;
                    }
                }
            }
            //UnityEngine.Debug.Log($"SLENDERMAN: Could not find a valid stare position!!!!!!!!!!!!!!!!!!!!!!!!!");
            couldNotStareLastAttempt = true;
            return Vector3.zero;
        }

        private void SetStalkingPosition(Vector3 newPosition, float timeToStare = 20f)
        {
            if (!IsServer) return;
            if (currentBehaviourStateIndex != 1)
            {
                SwitchToBehaviourStateOnLocalClient(1);
                couldNotStareLastAttempt = false;
                Vector3 randomNavMeshPositionInRadiusSpherical = RoundManager.Instance.GetRandomNavMeshPositionInRadiusSpherical(newPosition, 1f, navHit);
                agent.Warp(randomNavMeshPositionInRadiusSpherical);
                moveTowardsDestination = false;
                destination = base.transform.position;
                agent.SetDestination(destination);
                agent.speed = 0f;
                //UnityEngine.Debug.Log("Slenderman: STARTING HAUNT STARE");
                stalkingTimer = timeToStare;
                seenByPlayerThisTime = false; // So the dice-roll when looked at only happens once per stalk
                if (UnityEngine.Random.Range(0, 100) < 50)
                {
                    PlaySfxHauntedPlayerClientRpc("ambienceSFX");
                }
            }
        }

        private IEnumerator disappearOnDelay()
        {
            // Play jumpscare sound BEFORE disappearing
            if (vanishWithJumpscare)
            {
                PlaySfxHauntedPlayerClientRpc("jumpscareSFX");
                vanishWithJumpscare = false;
            }

            yield return new WaitForSeconds(1.5f);
            Disappear();
            disappearOnDelayCoroutine = null;
            if (vanishWithSound)
            {
                PlaySfxHauntedPlayerClientRpc("disappearSFX");
                vanishWithSound = false;
            }
        }

        private void Disappear()
        {
            if (!IsServer) return;
            
            DisableMesh();
            disappearWithDelay = false;
            sitOutOneStare = true;
            TeleportAway(); // So players cant collide w/ him at the previous position + scan him
            SwitchToBehaviourStateOnLocalClient(0); //Switch to Absent state
        }

        private void StartCreepingTowardsPlayer(float movementSpeed = 10.0f)
        {
            if (!IsServer) return;

            if (!creepingCloser)
            {
                DisableMesh();
                creepingCloser = true; //used for collision detection (if colliding w/ haunted player during creeping closer, kill player)
                //UnityEngine.Debug.Log("SLENDERMAN CREEPING CLOSER!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
                timesCreptCloser++;
                agent.speed = movementSpeed;
                agent.acceleration = 24.0f;
                SetDestinationToPosition(hauntingPlayer.transform.position);
                moveTowardsDestination = true;

                creepingCloserTimer = 4.0f;
                timer = 0f;
            }
            else
            {
                UnityEngine.Debug.Log("Slenderman already creeping closer, abort.");
            }
        }

        private void StopCreepingTowardsPlayer()
        {
            if (!IsServer) return;

            creepingCloser = false;
            agent.speed = 0f;
            moveTowardsDestination = false;
            if (!enemyMeshEnabled)
            {
                EnableMesh();
            }
            //UnityEngine.Debug.Log("SLENDERMAN stopped creeping closer!!!");
        }

        private void SwitchHauntingPlayerTo(PlayerControllerB newHauntingTarget)
        {
            if (!IsServer) return;
            
            if (newHauntingTarget != null)
            {
                hauntingPlayer = null; //Resets hauntingPlayer
                hauntingPlayer = newHauntingTarget;
                SyncTimesSeenByPlayerClientRpc(0);
                //timesSeenByPlayer = 0; //Reset stare malus
                //newHauntingTarget = null;
                if (hauntingPlayer.isPlayerDead)
                {
                    for (int k = 0; k < StartOfRound.Instance.allPlayerScripts.Length; k++)
                    {
                        if (!StartOfRound.Instance.allPlayerScripts[k].isPlayerDead)
                        {
                            hauntingPlayer = StartOfRound.Instance.allPlayerScripts[k];
                            break;
                        }
                    }
                }
                HandleChangeHauntingPlayerClientRpc((int)hauntingPlayer.playerClientId);
                //Debug.Log($"Successfully switched hauntingPlayer to: {hauntingPlayer.name}!");
            }
            else
            {
                Debug.LogWarning("Error: Could not set new hauntingPlayer as assigned newHauntingTarget == null!");
            }
            switchedHauntingPlayer = false;
        }

        private void StartChasing(float chaseDuration = 20f)
        {
            if (!IsServer) return;
            if (currentBehaviourStateIndex != 3)
            {
                SwitchToBehaviourStateOnLocalClient(3);
                //UnityEngine.Debug.Log($"Slenderman: Starting a chase with {hauntingPlayer.name}");
                disappearWithDelay = false;
                StartChaseAnimClientRpc(); // Animation must be run client side
                
                chaseTimer = chaseDuration;
                timer = 0f;

                agent.speed = 13.0f;
                agent.acceleration = 26.0f;
                //SetDestinationToPosition(hauntingPlayer.transform.position);
                moveTowardsDestination = true;
                //movingTowardsTargetPlayer = true;
                PlaySfxClientRpc("chaseSFX");
                MessWithLights();
                EnableMesh(true);
            }
        }

        private void StopChasing()
        {
            if (!IsServer) return;

            agent.speed = 0f;
            moveTowardsDestination = false;
            StopChaseAnimClientRpc();
            seenByPlayerThisTime = false;
            SwitchToBehaviourStateOnLocalClient(2); //Disappear
            //UnityEngine.Debug.Log($"Slenderman: Chase with {hauntingPlayer.name} ended!");
        }

        private void TeleportAway()
        {
            if (!IsServer) return;
            int random = UnityEngine.Random.Range(25, 200);
            Vector3 warpPosition = hauntingPlayer.transform.position + Vector3.Scale(new Vector3(-random, 0, -random), GameNetworkManager.Instance.localPlayerController.transform.forward);
            agent.Warp(warpPosition); //Warp to a random position at least 25 units away from the player
        }

        // Thanks DarthFigo
        private void HandleChangeHauntingPlayer(int hauntingPlayerId)
        {

            if (hauntingPlayerId == -69420)
            {
                hauntingPlayer = null;
                return;
            }

            PlayerControllerB player = StartOfRound.Instance.allPlayerScripts[hauntingPlayerId];
            hauntingPlayer = player;
            //UnityEngine.Debug.Log($"Target player is now: {player.name}");
        }

        [ClientRpc]
        private void HandleChangeHauntingPlayerClientRpc(int hauntingPlayerId)
        {
            HandleChangeHauntingPlayer(hauntingPlayerId);
        }

        [ClientRpc]
        private void SyncTimesSeenByPlayerClientRpc(int timesSeen)
        {
            if (GameNetworkManager.Instance.localPlayerController == null) return;
            if (timesSeen == 0)
            {
                timesSeenByPlayer = 0;
            }
            else if (timesSeen == 1)
            {
                timesSeenByPlayer++;
            }
            else if (timesSeen == -1)
            {
                timesSeenByPlayer--;
            }
            else
            {
                UnityEngine.Debug.LogWarning($"Slenderman: Synchronization of timesSeenByPlayer - invalid number!");
                return;
            }
        }

        [ClientRpc]
        private void SyncNumSlendermanEnemiesInLevelClientRpc(int numSlendermanEnemies)
        {
            if (GameNetworkManager.Instance.localPlayerController == null) return;
            if (numSlendermanEnemies == 0)
            {
                numSlendermanEnemiesInLevel = 0;
            }
            else if (numSlendermanEnemies == 1)
            {
                numSlendermanEnemiesInLevel++;
            }
            else if (numSlendermanEnemies == -1)
            {
                numSlendermanEnemiesInLevel--;
            }
            else
            {
                UnityEngine.Debug.LogWarning($"Slenderman: Synchronization of numSlendermanEnemiesInLevel - invalid number!");
                return;
            }
        }

        [ClientRpc]
        private void StartChaseAnimClientRpc()
        {
            creatureAnimator.SetBool("Chase", value: true);
        }

        [ClientRpc]
        private void StopChaseAnimClientRpc()
        {
            creatureAnimator.SetBool("Chase", value: false);
        }

        [ClientRpc]
        private void PlaySfxHauntedPlayerClientRpc(string sfxClipName)
        {
            if (hauntingPlayer != GameNetworkManager.Instance.localPlayerController) return;

            if (sfxClipName == "ambienceSFX")
            {
                creatureVoice.clip = ambienceSFX;
                creatureVoice.Play();
            }
            else if (sfxClipName == "disappearSFX")
            {
                creatureVoice.clip = disappearSFX;
                creatureVoice.Play();
            }
            else if (sfxClipName == "chaseSFX")
            {
                creatureVoice.clip = chaseSFX;
                creatureVoice.Play();
            }
            else if (sfxClipName == "jumpscareSFX")
            {
                creatureVoice.clip = jumpscareSFX;
                creatureVoice.Play();
            }
            else
            {
                UnityEngine.Debug.LogWarning("Slenderman: Couldn't find assigned SFX sound!");
                return;
            }
        }

        [ClientRpc]
        private void PlaySfxClientRpc(string sfxClipName)
        {
            if (sfxClipName == "ambienceSFX")
            {
                creatureVoice.clip = ambienceSFX;
                creatureVoice.Play();
            }
            else if (sfxClipName == "disappearSFX")
            {
                creatureVoice.clip = disappearSFX;
                creatureVoice.Play();
            }
            else if (sfxClipName == "chaseSFX")
            {
                creatureVoice.clip = chaseSFX;
                creatureVoice.Play();
            }
            else if (sfxClipName == "jumpscareSFX")
            {
                creatureVoice.clip = jumpscareSFX;
                creatureVoice.Play();
            }
            else
            {
                UnityEngine.Debug.LogWarning("Slenderman: Couldn't find assigned SFX sound!");
                return;
            }
        }

        private void DisableMesh()
        {
            if (!IsServer) return;

            enemyMeshEnabled = false;
            DisableEnemyMeshClientRpc();
        }

        [ClientRpc]
        private void DisableEnemyMeshClientRpc()
        {
            if (enemyMeshRenderer != null)
            {
                enemyMeshRenderer.enabled = false;
            }
            else
            {
                Debug.LogError("Slenderman Renderer not found on this GameObject!");
            }
        }

        private void EnableMesh(bool visibleToEveryone = false)
        {
            if (!IsServer) return;

            enemyMeshEnabled = true;
            if (visibleToEveryone)
            {
                EnableEnemyMeshEveryoneClientRpc();
            }
            else
            {
                EnableEnemyMeshClientRpc();
            }
        }

        [ClientRpc]
        private void EnableEnemyMeshEveryoneClientRpc()
        {
            if (enemyMeshRenderer != null)
            {
                enemyMeshRenderer.enabled = true;
            }
            else
            {
                Debug.LogError("Slenderman Renderer not found on this GameObject!");
            }
        }

        [ClientRpc]
        private void EnableEnemyMeshClientRpc()
        {
            if (hauntingPlayer != GameNetworkManager.Instance.localPlayerController) return;
            
            if (enemyMeshRenderer != null)
            {
                enemyMeshRenderer.enabled = true;
            }
            else
            {
                Debug.LogError("Couldn't enable Slenderman Renderer for hauntingPlayer only!");
            }
        }

        [ClientRpc]
        private void MessWithLightsClientRpc()
        {
            if (hauntingPlayer == null)
            {
                UnityEngine.Debug.LogWarning("Error: Could not increase fear level - hauntingPlayer == null!");
                return;
            }
            if (hauntingPlayer != GameNetworkManager.Instance.localPlayerController)
            {
                return;
            }
            if (timesSeenByPlayer > 0)
            {
                GameNetworkManager.Instance.localPlayerController.JumpToFearLevel(0.8f);
                UnityEngine.Debug.Log($"Jump to fear level 0.8 for player: {hauntingPlayer.name}");
            }
            else
            {
                GameNetworkManager.Instance.localPlayerController.JumpToFearLevel(0.2f);
                UnityEngine.Debug.Log($"Jump to fear level 0.2 for player: {hauntingPlayer.name}");
            }
        }

        // Unused (for now)
        [ClientRpc]
        private void FlipLightsBreakerClientRpc()
        {
            GameNetworkManager.Instance.localPlayerController.JumpToFearLevel(0.2f);
        }

        private void MessWithLights()
        {
            if (!IsServer) return;

            RoundManager.Instance.FlickerLights(flickerFlashlights: true, disableFlashlights: true);
            MessWithLightsClientRpc();
            UnityEngine.Debug.Log($"Flickering flashlights for player: {hauntingPlayer.name}");
        }

        // Unused (for now)
        private void FlipLightsBreaker()
        {
            if (!IsServer) return;

            BreakerBox breakerBox = UnityEngine.Object.FindObjectOfType<BreakerBox>();
            if (breakerBox != null)
            {
                breakerBox.SetSwitchesOff();
                RoundManager.Instance.TurnOnAllLights(on: false);
                FlipLightsBreakerClientRpc();
            }
        }
           
        [ClientRpc]
        private void KillHauntedPlayerClientRpc()
        {
            if (hauntingPlayer != GameNetworkManager.Instance.localPlayerController) return;

            hauntingPlayer.KillPlayer(Vector3.zero, spawnBody: true, CauseOfDeath.Unknown, 1);
        }

        public override void Update()
        {
            base.Update();
            if (!IsServer) return;

            if (StartOfRound.Instance.allPlayersDead)
            {
                return;
            }

            if (!hauntingPlayer.isPlayerControlled || hauntingPlayer.isPlayerDead)
            {
                if (!switchedHauntingPlayer)
                {
                    switchedHauntingPlayer = true;
                    ChoosePlayerToHaunt();
                }
            }

            switch (currentBehaviourStateIndex)
            {
                case (int)States.Absent:
                    {
                        if (enemyMeshEnabled)
                        {
                            DisableMesh();
                        }
                        float num = 50f;
                        if (couldNotStareLastAttempt || sitOutOneStare)
                        {
                            num = 25f; //Every 25 seconds, try to spawn
                        }
                        if (timer > num)
                        {
                            timer = 0f;
                            TryFindingHauntPosition();
                            sitOutOneStare = false;
                        }
                        else
                        {
                            timer += Time.deltaTime;
                        }
                        break;
                    }

                case (int)States.Stalking:
                    {
                        if (!enemyMeshEnabled && !creepingCloser)
                        {
                            EnableMesh();
                        }
                        turnCompass.LookAt(hauntingPlayer.transform);
                        base.transform.eulerAngles = new Vector3(base.transform.eulerAngles.x, turnCompass.eulerAngles.y, base.transform.eulerAngles.z);
                        if (timer > stalkingTimer)
                        {
                            float distanceToPlayer = Vector3.Distance(hauntingPlayer.gameplayCamera.transform.position, base.transform.position);
                            // If slenderman is further away from the player than 10 units, creep closer.
                            // Else, start a chase.
                            if (distanceToPlayer > 10f)
                            {
                                timer = 0f; //Reset Timer
                                StartCreepingTowardsPlayer();
                            }
                            else
                            {
                                if (creepingCloser)
                                {
                                    StopCreepingTowardsPlayer();
                                }
                                StartChasing();
                            }
                        }
                        // Physics.Linecast() = Returns true if there is any collider intersecting the line between start and end.
                        // So: If NO Collider between player & slenderman: Either vanish or start chasing
                        else if (!creepingCloser && !Physics.Linecast(hauntingPlayer.gameplayCamera.transform.position, base.transform.position + Vector3.up * 0.4f, StartOfRound.Instance.collidersAndRoomMask))
                        {
                            if (hauntingPlayer.HasLineOfSightToPosition(base.transform.position + Vector3.up * 0.4f, 50f, 100, 5f)) //range of 100 units, width of 50 degrees
                            {
                                //UnityEngine.Debug.Log("Haunted Player looking at slenderman!!");
                                if (!seenByPlayerThisTime)
                                {
                                    seenByPlayerThisTime = true;
                                    SyncTimesSeenByPlayerClientRpc(1);
                                    float chaseChance = UnityEngine.Random.Range(0, 100);
                                    float distanceToPlayer = Vector3.Distance(hauntingPlayer.gameplayCamera.transform.position, base.transform.position);
                                    if ((distanceToPlayer > 80f && chaseChance <= (1f + timesSeenByPlayer))) //Distance greater than 80 units: 1% chance to start a chase
                                    {
                                        StartChasing();
                                    }
                                    else if ((distanceToPlayer < 80f && distanceToPlayer > (60f + timesSeenByPlayer)) && chaseChance < 5) //Distance 80 - 60 units: 5% chance to start a chase
                                    {
                                        StartChasing();
                                    }
                                    else if ((distanceToPlayer < 60f && distanceToPlayer > (40f + timesSeenByPlayer)) && chaseChance < 15) //Distance 60 - 40 units: 15% chance to start a chase
                                    {
                                        StartChasing();
                                    }
                                    else if ((distanceToPlayer < 40f && distanceToPlayer > (25f + timesSeenByPlayer)) && chaseChance < 30) //Distance 40 - 25 units: 30% chance to start a chase
                                    {
                                        StartChasing();
                                    }
                                    else if ((distanceToPlayer < 25f && distanceToPlayer > (11f + timesSeenByPlayer)) && chaseChance < 50) //Distance 25 - 11 units: 50% chance to start a chase
                                    {
                                        StartChasing();
                                    }
                                    else if (distanceToPlayer <= 11f && chaseChance < (80f + timesSeenByPlayer))
                                    {
                                        StartChasing();
                                    }
                                    else
                                    {
                                        if (timesSeenByPlayer == 1)
                                        {
                                            PlaySfxHauntedPlayerClientRpc("jumpscareSFX");
                                        }
                                        seenByPlayerThisTime = false;
                                        disappearWithDelay = true;
                                        SwitchToBehaviourStateOnLocalClient(2);
                                    }
                                }
                            }
                            // If player is not looking at him
                            else
                            {
                                timer += Time.deltaTime;
                            }
                        }
                        // If any collider intersects between player and slenderman (no LOS)
                        else
                        {
                            timer += Time.deltaTime * 3.0f;
                        }
                        break;
                    }

                case (int)States.Vanishing:
                    {
                        if (disappearWithDelay)
                        {
                            if (disappearOnDelayCoroutine == null)
                            {
                                float distanceToPlayer = Vector3.Distance(hauntingPlayer.gameplayCamera.transform.position, base.transform.position);
                                if (distanceToPlayer <= 80f && distanceToPlayer > 25f)
                                {
                                    vanishWithSound = true;
                                    MessWithLights();
                                }
                                else if (distanceToPlayer <= 25f)
                                {
                                    vanishWithJumpscare = true;
                                    MessWithLights();
                                }
                                disappearOnDelayCoroutine = StartCoroutine(disappearOnDelay());
                            }
                        }
                        else
                        {
                            Disappear();
                        }
                        break;
                    }

                case (int)States.Chasing:
                    {
                        // The chase lasts x seconds (chaseTimer), or until the target player moves more than 100 units away from him (aka entering/leaving facility).
                        if (chaseTimer <= 0f || Vector3.Distance(base.transform.position, hauntingPlayer.transform.position) > 100f)
                        {
                            StopChasing();
                        }
                        else
                        {
                            chaseTimer -= Time.deltaTime;
                        }
                        break;
                    }
            }
        }

        public override void DoAIInterval()
        {
            base.DoAIInterval();
            if (!IsServer) return;

            switch (currentBehaviourStateIndex)
            {
                case (int)States.Stalking:
                    {
                        // If currently creeping closer, check timer & stop after creeping time runs out
                        if (creepingCloser && timer > creepingCloserTimer)
                        {
                            StopCreepingTowardsPlayer();
                        }
                        break;
                    }
                case (int)States.Chasing:
                    {
                        if (hauntingPlayer != null && !hauntingPlayer.isPlayerDead)
                        {
                            SetDestinationToPosition(hauntingPlayer.transform.position);
                        }
                        else
                        {
                            if (moveTowardsDestination)
                            {
                                moveTowardsDestination = false;
                                destination = base.transform.position;
                                agent.SetDestination(destination);
                            }
                            return;
                        }
                        break;
                    }
            }
        }

        private PlayerControllerB PlayerMeetsStandardCollisionConditions(Collider other)
        {
            if (!IsServer) return null;

            if (isEnemyDead)
            {
                return null;
            }

            if (!ventAnimationFinished)
            {
                return null;
            }
            
            if (stunNormalizedTimer >= 0f)
            {
                return null;
            }

            PlayerControllerB playerControllerB = other.gameObject.GetComponent<PlayerControllerB>();

            if (playerControllerB == null)
            {
                return null;
            }

            if (!playerControllerB.isPlayerDead && playerControllerB.sinkingValue < 0.7300000190734863)
            {
                return playerControllerB;
            }
            return null;
        }

        public override void OnCollideWithPlayer(Collider other)
        {
            base.OnCollideWithPlayer(other);
            if (!IsServer) return;

            PlayerControllerB playerControllerB = PlayerMeetsStandardCollisionConditions(other);
            if (playerControllerB != null)
            {
                UnityEngine.Debug.Log("Slenderman: collided with player");
                if (playerControllerB == hauntingPlayer)
                {
                    if (currentBehaviourStateIndex == 1)
                    {
                        if (creepingCloser)
                        {
                            return;
                        }
                        else
                        {
                            UnityEngine.Debug.Log("Slenderman: collided with hauntingPlayer during stalking phase!");
                            SwitchToBehaviourStateOnLocalClient(2); //Vanish
                        }
                    }
                    else if (currentBehaviourStateIndex == 3) // || creepingCloser == true
                    {
                        hauntingPlayer.KillPlayer(Vector3.zero, spawnBody: true, CauseOfDeath.Unknown, 1);
                        KillHauntedPlayerClientRpc();
                        UnityEngine.Debug.Log("Slenderman: killed player");
                        moveTowardsDestination = false;
                        
                        disappearWithDelay = false;
                        StopChasing();
                    }
                }
                else
                {
                    // STATE 1 FOR NOW - to add a bit variety & make him able to change targets to unknowing players
                    if (currentBehaviourStateIndex == 1 && creepingCloser)
                    {
                        switchedHauntingPlayer = true;
                        // Change targets to the new player
                        SwitchHauntingPlayerTo(playerControllerB);
                        UnityEngine.Debug.Log($"Slenderman: collided with non-haunted player during creepingCloser. Changing targets to {playerControllerB.name}");
                        StopChasing();
                    }
                    else if (currentBehaviourStateIndex == 3)
                    {
                        switchedHauntingPlayer = true;
                        // Change targets to the new player
                        SwitchHauntingPlayerTo(playerControllerB);
                        UnityEngine.Debug.Log($"Slenderman: collided with non-haunted player during chase. Changing targets to {playerControllerB.name}");
                        StopChasing();
                    }
                    else
                    {
                        UnityEngine.Debug.Log("Slenderman: collided with non-haunted player outside of a chase.");
                        SwitchToBehaviourStateOnLocalClient(2); // Vanish
                    }
                }
            }
            else
            {
                UnityEngine.Debug.LogWarning("ERROR: Slenderman player collision: playerControllerB == null!!!!!!!!!!!!!!!!!!!");
                return;
            }
        }
    }
}