using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using ParkourKnuckle.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ParkourKnuckle
{
    [BepInPlugin("com.nimius.parkourknuckle", "Parkour Knuckle", "1.2.1")]
    public class Plugin : BaseUnityPlugin
    {
        private Harmony _harmony;

        public static ConfigEntry<bool> EnableParkourRotation;
        public static ConfigEntry<bool> EnableParkourFOV;
        public static ConfigEntry<bool> EnableParkourShake;
        public static bool isUIVisible = false;

        public static ConfigFile ProgressionFile;
        public static Dictionary<string, ConfigEntryBase> SkillConfigs = new Dictionary<string, ConfigEntryBase>();
        void Awake()
        {
            EnableParkourRotation = Config.Bind("Camera Settings", "Use Parkour Rotation", true, "Determines whether the camera will tilt and rotate differently when doing parkour actions.");
            EnableParkourFOV = Config.Bind("Camera Settings", "Use Parkour FOV", true, "Determines whether the camera will zoom in and out during specific parkour actions.");
            EnableParkourShake = Config.Bind("Camera Settings", "Use Parkour Shake", true, "Determines whether the camera will shake during specific parkour actions.");

            string saveSkillPath = Path.Combine(Paths.ConfigPath, "parkourprogress.cfg");
            ProgressionFile = new ConfigFile(saveSkillPath, true);

            SkillConfigs["QuickTurning"] = ProgressionFile.Bind("Progression", "QuickTurning", false, "");
            SkillConfigs["Sliding"] = ProgressionFile.Bind("Progression", "Sliding", false, "");
            SkillConfigs["SlideJumping"] = ProgressionFile.Bind("Progression", "SlideJumping", false, "");
            SkillConfigs["Leaping"] = ProgressionFile.Bind("Progression", "Leaping", false, "");
            SkillConfigs["Rolling"] = ProgressionFile.Bind("Progression", "Rolling", false, "");
            SkillConfigs["WallKicking"] = ProgressionFile.Bind("Progression", "WallKicking", false, "");
            SkillConfigs["WallRunning"] = ProgressionFile.Bind("Progression", "WallRunning", false, "");
            SkillConfigs["VerticalWallRunning"] = ProgressionFile.Bind("Progression", "VerticalWallRunning", false, "");
            SkillConfigs["WallRunBoost"] = ProgressionFile.Bind("Progression", "WallRunBoost", false, "");
            SkillConfigs["HeightCurrency"] = ProgressionFile.Bind("Currency", "HeightCurrency", 0f, "");
            SkillConfigs["MaxHeight"] = ProgressionFile.Bind("Currency", "MaxHeight", 0f, "");

            this._harmony = new Harmony("com.nimius.parkourknuckle");
            this._harmony.PatchAll();
            Logger.LogInfo("Harmony Patches applied successfully.");

            ParkourUI.Initialize();
            Logger.LogInfo("Parkour UI Engine Initialized.");
        }
    }

    [HarmonyPatch(typeof(ENT_Player), "Update")]
    public class PlayerModifierPatch
    {
        private static Quaternion targetRotation;
        private static bool isRotating = false;
        private static readonly float turnSpeed = 24f;
        public static bool onCooldown = false;
        public static readonly float CooldownDur = 0.2f;
        public static float CooldownTime = 0f;

        public static float chargeStartTime;
        public static bool isCharging;
        public static float currentCharge;
        public static readonly float maxChargeTime = 3.5f;
        public static float leapCooldownTime = 0f;
        public static readonly float leapCooldownDur = 2f;
        private static readonly float leapForceMultiplier = 1f;
        public static bool leapCooldown = false;
        private static readonly float minStamina = 1;
        private static readonly float maxStamina = 6;
        private static readonly float upwardArcForce = 1f;
        private static bool isHolding = false;

        private static bool hasWallRunInAir = false;
        private static readonly float tiltLerpSpeed = 4f;
        private static float gripValue = 0f;
        private static bool isHorizRun = false;
        private static int spaceTapCount = 0;
        private static float spaceTapTimer = 0f;
        private const float doubleTapWindow = 0.3f;

        private static bool isVerticalRun = false;
        private static bool hasWallRunVertical = false;
        private static float verticalGraceTimer = 0f;
        private static readonly float maxGraceTime = 0.2f;

        private static bool isVaulting = false;
        private static Vector3 vaultTargetPos;
        private static float vaultTimer = 0f;
        private static readonly float vaultDuration = 2f;

        private static bool isSliding = false;
        private static readonly float minSlideSpeed = 6f;
        private static Vector3 slideDir;
        private static bool canSlide = true;
        private static float slideTime = 0f;
        private static readonly float slideDuration = 1.8f;
        private static Vector3 slideStartPos;
        private static Vector3 slideTargetPos;

        private static float startY;
        private static bool wasGrounded;
        private static bool isRotatingRoll = false;
        private static Quaternion startRotationRoll;
        private static Vector3 rollStartPos;
        private static Vector3 rollTargetPos;
        private static Vector3 rollDir;
        private static float rollTimer = 0f;
        private static readonly float rollDur = 0.5f;
        private static bool cancelNextRollingFallDamage = false;
        private static float rollingFallDamageCancelExpiresAt = -1f;
        private const float rollingFallDamageCancelGrace = 0.2f;

        public static float levelHighestHeight;
        public static int highestMilestone = 0;
        public static float liveRunHeight = 0f;
        public static bool isConsumableUltimate = false;
        public static int roidedCount = 0;

        public static float previousCurrency = 0;

        private static void ArmRollingFallDamageCancel()
        {
            cancelNextRollingFallDamage = true;
            rollingFallDamageCancelExpiresAt = Time.time + rollingFallDamageCancelGrace;
        }

        private static void ExpireRollingFallDamageCancel()
        {
            if (cancelNextRollingFallDamage && Time.time > rollingFallDamageCancelExpiresAt)
            {
                cancelNextRollingFallDamage = false;
                rollingFallDamageCancelExpiresAt = -1f;
            }
        }

        public static bool TryCancelRollingFallDamage(Damageable.DamageInfo info)
        {
            ExpireRollingFallDamageCancel();

            if (!cancelNextRollingFallDamage || info == null || !info.HasTag("falling"))
            {
                return false;
            }

            cancelNextRollingFallDamage = false;
            rollingFallDamageCancelExpiresAt = -1f;
            return true;
        }

        [HarmonyPostfix]
        public static void Postfix(ENT_Player __instance)
        {
            if (CL_GameManager.gamemode.allowLeaderboardScoring)
            {
                CL_GameManager.gamemode.allowLeaderboardScoring = false;
            }

            var player = __instance;
            var controller = player.GetComponent<CharacterController>();
            bool isGrounded = controller.isGrounded;
            isHolding = false;
            ExpireRollingFallDamageCancel();

            foreach (var hand in player.hands)
            {
                if (hand.handhold != null && hand.handhold.GetHolding())
                {
                    isHolding = true;
                    break;
                }
            }

            if (onCooldown)
            {
                if (!isRotating)
                {
                    CooldownTime += Time.deltaTime;
                }
                if (CooldownTime >= CooldownDur)
                {
                    onCooldown = false;
                    CooldownTime = 0f;
                }
            }

            if (Input.GetKeyUp(KeyCode.X) && !onCooldown && !isHorizRun && player.health > 0f)
            {
                if (((ConfigEntry<bool>)Plugin.SkillConfigs["QuickTurning"]).Value)
                {
                    onCooldown = true;
                    targetRotation = player.transform.rotation * Quaternion.Euler(0f, 180f, 0f);
                    isRotating = true;
                }
            }

            if (isRotating)
            {
                player.transform.rotation = Quaternion.Slerp(player.transform.rotation, targetRotation, (Time.deltaTime * (turnSpeed * ((roidedCount / 2) + 1))));

                if (Quaternion.Angle(player.transform.rotation, targetRotation) < 0.5f)
                {
                    isRotating = false;
                }
            }

            if (leapCooldown)
            {
                if (isGrounded || isHolding)
                {
                    leapCooldownTime += Time.deltaTime;
                }
                if (leapCooldownTime >= leapCooldownDur)
                {
                    leapCooldown = false;
                    leapCooldownTime = 0f;
                }
            }

            if (Input.GetKeyDown(KeyCode.G) && (isGrounded || isHolding) && !leapCooldown)
            {
                if (((ConfigEntry<bool>)Plugin.SkillConfigs["Leaping"]).Value)
                {
                    if (isGrounded || isHolding)
                    {
                        bool bothHandsReady = true;

                        foreach (var hand in player.hands)
                        {
                            if (hand.gripStrength < minStamina)
                            {
                                bothHandsReady = false;
                            }
                        }

                        if (bothHandsReady)
                        {
                            chargeStartTime = Time.time;
                            isCharging = true;
                        }
                    }
                }
            }

            currentCharge = 0f;

            if (isCharging && Input.GetKey(KeyCode.G) && (isGrounded || isHolding) && !leapCooldown)
            {
                currentCharge = Mathf.Min(Time.time - chargeStartTime, maxChargeTime);

                if (Plugin.EnableParkourShake.Value)
                {
                    CL_CameraControl.Shake(currentCharge * Time.deltaTime * 0.025f);

                    foreach (var hand in player.hands)
                    {

                        hand.ShakeHand(currentCharge * Time.deltaTime * 0.025f);
                    }
                }

                if (Plugin.EnableParkourFOV.Value)
                {
                    player.FOVShock(PlayerModifierPatch.currentCharge * 0.1f, false);
                }
            }

            if (Input.GetKeyUp(KeyCode.G) && isCharging && (isGrounded || isHolding) && !leapCooldown)
            {
                if (((ConfigEntry<bool>)Plugin.SkillConfigs["Leaping"]).Value)
                {
                    if (!isGrounded && !isHolding)
                    {
                        isCharging = false;
                        return;
                    }
                    currentCharge = Mathf.Min(Time.time - chargeStartTime, maxChargeTime);
                    float chargeDuration = Mathf.Min(Time.time - chargeStartTime, maxChargeTime);
                    float finalForce = chargeDuration * (leapForceMultiplier * ((roidedCount / 2) + 1));
                    float finalCost = Mathf.CeilToInt(Mathf.Lerp(minStamina, maxStamina, chargeDuration / maxChargeTime));

                    Vector3 leapDirection = player.cam.transform.forward + (Vector3.up * upwardArcForce);
                    Vector3 leapVelocity = leapDirection.normalized * finalForce;

                    player.SetDirectionalForce(leapVelocity);

                    foreach (var hand in player.hands)
                    {
                        if (!isConsumableUltimate)
                        {
                            hand.gripStrength -= finalCost;
                            if (hand.gripStrength < 0f)
                            {
                                hand.gripStrength = 0f;
                            }
                        }

                        if (hand.IsHolding())
                        {
                            hand.DropHand(true);
                        }
                    }

                    leapCooldown = true;
                    isCharging = false;
                }
            }

            if (!isGrounded)
            {
                currentCharge = 0f;
                isHolding = false;
                isCharging = false;
            }

            if (Input.GetKeyDown(KeyCode.Space) && Input.GetKey(KeyCode.S))
            {
                if (((ConfigEntry<bool>)Plugin.SkillConfigs["WallKicking"]).Value)
                {
                    bool bothStamina = true;
                    foreach (var hand in player.hands)
                    {
                        if (hand.gripStrength < 1)
                        {
                            bothStamina = false;
                        }
                    }

                    if (!isHolding && bothStamina)
                    {
                        Vector3 backDirection = -player.transform.forward;

                        if (Physics.Raycast(player.transform.position, backDirection, out RaycastHit hit, 1.2f))
                        {
                            Vector3 kickDir = hit.normal + (Vector3.up * 1.2f);
                            float kickForce = 1.5f * ((roidedCount / 2) + 1);

                            player.SetDirectionalForce(kickDir.normalized * kickForce);

                            if (Plugin.EnableParkourFOV.Value)
                            {
                                player.FOVShock(kickForce, false);
                            }

                            if (Plugin.EnableParkourShake.Value)
                            {
                                CL_CameraControl.Shake(Time.deltaTime * 0.15f);
                            }

                            foreach (var hand in player.hands)
                            {
                                if (!isConsumableUltimate)
                                {
                                    hand.gripStrength -= 1.0f;
                                    if (hand.gripStrength < 0f) hand.gripStrength = 0f;
                                }
                            }
                        }
                    }
                }
            }

            if (isGrounded && !isVerticalRun && !isHolding)
            {
                isHorizRun = false;
                isVerticalRun = false;
                hasWallRunInAir = false;
                hasWallRunVertical = false;
                controller.enabled = true;
                player.transform.rotation = Quaternion.Euler(0, player.transform.eulerAngles.y, 0);
            }

            if (!isGrounded && isHolding)
            {
                isHorizRun = false;
                isVerticalRun = false;
                hasWallRunInAir = true;
                hasWallRunVertical = true;
                controller.enabled = true;
            }

            bool canRun = true;
            bool runStamina = true;

            foreach (var hand in player.hands)
            {

                gripValue = hand.gripStrength;

                if (hand.gripStrength <= 0f)
                {
                    canRun = false;
                }
            }

            if (!canRun)
            {
                runStamina = false;
            }

            if (Input.GetKeyDown(KeyCode.Space))
            {
                spaceTapCount++;
                if (spaceTapCount == 1) spaceTapTimer = doubleTapWindow;
            }

            if (spaceTapTimer > 0 && (!isVerticalRun || !isHorizRun))
            {
                spaceTapTimer -= Time.deltaTime;
            }
            else
            {
                spaceTapCount = 0;
            }

            bool wallLeft = Physics.Raycast(player.transform.position, -player.transform.right, out RaycastHit hitLeft, 1.2f);
            bool wallRight = Physics.Raycast(player.transform.position, player.transform.right, out RaycastHit hitRight, 1.2f);

            bool hasDoubleTapped = spaceTapCount >= 2;
            bool isHoldingInput = Input.GetKey(KeyCode.Space) && (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.D));

            if (!hasWallRunInAir && isHoldingInput && (wallLeft || wallRight) && (isHorizRun || hasDoubleTapped))
            {
                if (((ConfigEntry<bool>)Plugin.SkillConfigs["WallRunning"]).Value)
                {
                    if (runStamina)
                    {
                        isHorizRun = true;
                        spaceTapCount = 2;
                        controller.enabled = false;

                        Vector3 wallNormal = wallLeft ? hitLeft.normal : hitRight.normal;
                        Vector3 runDir = Vector3.Cross(wallNormal, Vector3.up).normalized;

                        if (Physics.Raycast(player.transform.position, player.transform.forward, out RaycastHit hitFront, 1.2f))
                        {
                            isHorizRun = false;
                            hasWallRunInAir = true;
                            controller.enabled = true;
                            return;
                        }

                        if (Vector3.Dot(runDir, player.transform.forward) < 0)
                        {
                            runDir = -runDir;
                        }

                        float tiltAmount = (Plugin.EnableParkourRotation.Value) ? (wallLeft ? -15f : 15f) : 0f;

                        Quaternion lookRot = Quaternion.LookRotation(runDir) * Quaternion.Euler(0, 0, tiltAmount);
                        player.transform.rotation = Quaternion.Slerp(player.transform.rotation, lookRot, Time.deltaTime * tiltLerpSpeed);
                        player.SetDirectionalForce(((runDir * 0.8f) * ((gripValue * 0.1f) * ((roidedCount / 3.5f) + 1)) + (Vector3.up * 0.1f)));

                        if (Plugin.EnableParkourFOV.Value)
                        {
                            player.FOVShock(Mathf.Lerp(1, 0, gripValue / 10), false);
                        }

                        if (Plugin.EnableParkourShake.Value)
                        {
                            CL_CameraControl.Shake(Time.deltaTime * 0.25f);
                        }

                        foreach (var hand in player.hands)
                        {
                            if (!isConsumableUltimate)
                            {
                                hand.gripStrength -= Time.deltaTime * 2.5f;
                                if (hand.gripStrength < 0) hand.gripStrength = 0;
                            }
                        }
                    }
                    else
                    {
                        isHorizRun = false;
                        hasWallRunInAir = true;
                        controller.enabled = true;
                    }
                }
            }
            else
            {
                Quaternion uprightRot = Quaternion.Euler(0, player.transform.eulerAngles.y, 0);
                player.transform.rotation = Quaternion.Slerp(player.transform.rotation, uprightRot, Time.deltaTime * tiltLerpSpeed);

                if (!isGrounded && controller.enabled && spaceTapTimer <= 0)
                {
                    hasWallRunInAir = true;
                }

                controller.enabled = true;
                isHorizRun = false;
            }

            bool wallFront = Physics.Raycast(player.transform.position, player.transform.forward, out RaycastHit hitRun, 1.2f);
            bool isHoldingRun = Input.GetKey(KeyCode.Space) && Input.GetKey(KeyCode.W);

            bool hasDoubleTappedRun = spaceTapCount >= 2;

            if (isVerticalRun)
            {
                if (isHoldingRun)
                {
                    verticalGraceTimer = 0;
                }
                else
                {
                    verticalGraceTimer += Time.deltaTime;
                }
            }

            bool canVert = true;
            bool stamVertRun = true;

            foreach (var hand in player.hands)
            {
                if (hand.gripStrength <= 0)
                {
                    canVert = false;
                }
            }

            if (!canVert)
            {
                stamVertRun = false;
            }

            if (isVerticalRun && !stamVertRun)
            {
                isVerticalRun = false;
                hasWallRunVertical = true;
                return;
            }

            if (isVerticalRun && Input.GetKeyDown(KeyCode.Space) && verticalGraceTimer > 0 && verticalGraceTimer < maxGraceTime)
            {
                if (((ConfigEntry<bool>)Plugin.SkillConfigs["WallRunBoost"]).Value)
                {
                    float pushAwayForce = 1.5f * ((roidedCount / 2) + 1);
                    float upwardArcForce = 1.5f * ((roidedCount / 2) + 1);

                    Vector3 jumpOffDir = ((hitRun.normal * pushAwayForce) * (gripValue * 0.1f)) + ((Vector3.up * upwardArcForce) * (gripValue * 0.1f));
                    player.SetDirectionalForce(jumpOffDir);
                    
                    if (Plugin.EnableParkourFOV.Value)
                    {
                        player.FOVShock(2f, false);
                    }

                    isVerticalRun = false;
                    verticalGraceTimer = 0;
                    hasWallRunVertical = true;
                    controller.enabled = true;
                    return;
                }
            }

            if (isVerticalRun && verticalGraceTimer >= maxGraceTime)
            {
                isVerticalRun = false;
                hasWallRunVertical = true;
                controller.enabled = true;
                verticalGraceTimer = 0;
            }

            if (isVaulting)
            {
                vaultTimer += Time.deltaTime;

                player.transform.position = Vector3.Lerp(player.transform.position, vaultTargetPos, vaultDuration);

                if (Vector3.Distance(player.transform.position, vaultTargetPos) < 0.1f)
                {
                    isVaulting = false;
                    controller.enabled = true;
                }

                return;
            }
            
            if (isHoldingRun && !hasWallRunVertical && wallFront && (!hasWallRunVertical || isVerticalRun) && (verticalGraceTimer == 0f))
            {
                if (((ConfigEntry<bool>)Plugin.SkillConfigs["VerticalWallRunning"]).Value)
                {
                    if (!isVerticalRun && !hasDoubleTappedRun)
                    { }
                    else
                    {
                        float wallAngle = Vector3.Angle(hitRun.normal, Vector3.up);

                        if (runStamina && wallAngle > 80f && wallAngle < 100f)
                        {
                            isVerticalRun = true;
                            spaceTapCount = 2;
                            controller.enabled = false;

                            Vector3 faceWallDir = -hitRun.normal;

                            Vector3 ledgeCheckPos = player.transform.position + (Vector3.up * 1.5f);
                            bool wallAbove = Physics.Raycast(ledgeCheckPos, faceWallDir, 0.25f);

                            Quaternion climbRot = Quaternion.LookRotation(Vector3.up, hitRun.normal);

                            if (wallAbove && (!Plugin.EnableParkourRotation.Value || Quaternion.Angle(player.transform.rotation, climbRot) < 25f))
                            {
                                if (Physics.SphereCast((player.transform.position + (Vector3.up * 1.0f)), 0.3f, Vector3.up, out _, 0.7f))
                                {
                                    isVaulting = false;
                                    hasWallRunVertical = true;
                                    isVerticalRun = false;
                                    controller.enabled = true;
                                    return;
                                }
                            }

                            if (Physics.Raycast(player.transform.position, player.transform.up, 3))
                            {
                                isVaulting = false;
                                hasWallRunVertical = true;
                                isVerticalRun = false;
                                controller.enabled = true;
                                return;
                            }

                            if (!wallAbove)
                            {

                                if (!Physics.Raycast(ledgeCheckPos, faceWallDir, 1f))
                                {
                                    Vector3 rayStart = ledgeCheckPos + (faceWallDir * 1f);

                                    if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit ledgeHit, 2f))
                                    {

                                        Vector3 backCheckOrigin = ledgeHit.point + (Vector3.up * 0.5f);
                                        if (Physics.Raycast(backCheckOrigin, -faceWallDir, out RaycastHit wallThickness, 1.5f))
                                        {
                                            if (wallThickness.distance < 0.2f) return;
                                        }

                                        if (!Physics.SphereCast(ledgeHit.point + (Vector3.up * 0.1f), 0.3f, Vector3.up, out _, 1.8f))
                                        {
                                            vaultTargetPos = ledgeHit.point + (Vector3.up * 1.1f);

                                            isVaulting = true;
                                            vaultTimer = 0;
                                            isVerticalRun = false;
                                            hasWallRunVertical = true;
                                            controller.enabled = false;
                                            return;
                                        }
                                    }
                                }
                            }

                            if (Plugin.EnableParkourRotation.Value)
                            {
                                player.transform.rotation = Quaternion.Slerp(player.transform.rotation, climbRot, Time.deltaTime * 10f);
                            }

                            Vector3 climbVelocity = (Vector3.up * 0.4f) * (gripValue * (0.15f * ((roidedCount / 3) + 1)));
                            player.SetDirectionalForce(climbVelocity);
                            
                            if (Plugin.EnableParkourFOV.Value)
                            {
                                player.FOVShock(Mathf.Lerp(1, 0, gripValue / 10));
                            }

                            if (Plugin.EnableParkourShake.Value)
                            {
                                CL_CameraControl.Shake(Time.deltaTime * 0.4f);
                            }

                            foreach (var hand in player.hands)
                            {
                                if (!isConsumableUltimate)
                                {
                                    hand.gripStrength -= Time.deltaTime * 4.5f;
                                    if (hand.gripStrength < 0f) hand.gripStrength = 0f;
                                }
                            }
                            return;
                        }
                    }
                }
            }

            if (!isGrounded && !isVerticalRun && !isVaulting && verticalGraceTimer <= 0 && spaceTapTimer <= 0)
            {
                if (((ConfigEntry<bool>)Plugin.SkillConfigs["VerticalWallRunning"]).Value)
                {
                    if (controller.enabled)
                    {
                        hasWallRunVertical = true;
                    }
                }
            }

            Vector3 horizontalVel = new Vector3(controller.velocity.x, 0, controller.velocity.z);
            Vector3 localHorizontalVel = player.transform.InverseTransformDirection(horizontalVel);

            bool isMovingForward = localHorizontalVel.z > 0.1f;
            float currentSpeed = horizontalVel.magnitude;

            if (!player.IsCrouching() || isGrounded)
            {
                canSlide = true;
            }

            if (isGrounded && player.IsCrouching() && !isSliding && canSlide)
            {
                if (((ConfigEntry<bool>)Plugin.SkillConfigs["Sliding"]).Value)
                {
                    if (currentSpeed >= minSlideSpeed && isMovingForward)
                    {
                        isSliding = true;
                        canSlide = false;
                        slideDir = horizontalVel.normalized;
                        slideTime = 0f;

                        slideStartPos = player.transform.position;
                        slideTargetPos = slideStartPos + (slideDir * (10f * ((roidedCount / 2) + 1)));

                        controller.enabled = false;

                        if (Plugin.EnableParkourShake.Value)
                        {
                            CL_CameraControl.Shake(Time.deltaTime * 0.1f);
                        }
                    }
                }
            }

            if (isSliding)
            {
                if (Input.GetKeyDown(KeyCode.Space))
                {
                    if (((ConfigEntry<bool>)Plugin.SkillConfigs["SlideJumping"]).Value)
                    {
                        float slideRemnant = 1f - (slideTime / slideDuration);
                        float lungeForwardPower = 1f * slideRemnant * ((roidedCount / 2) + 1);
                        float lungUpwardPower = 1.5f * slideRemnant * ((roidedCount / 2) + 1);
                        Vector3 lungeVelocity = (slideDir * lungeForwardPower) + (Vector3.up * lungUpwardPower);

                        float staminaCost = lungeVelocity.magnitude * 1f;
                        bool slideStaminaReady = true;
                        bool hasEnoughStamina = true;
                        foreach (var hand in player.hands)
                        {
                            if (hand.gripStrength < staminaCost)
                            {
                                slideStaminaReady = false;
                                break;
                            }
                        }

                        if (!slideStaminaReady)
                        {
                            hasEnoughStamina = false;
                        }

                        if (hasEnoughStamina)
                        {
                            player.SetDirectionalForce(lungeVelocity);

                            if (Plugin.EnableParkourShake.Value)
                            {
                                CL_CameraControl.Shake(lungeVelocity.magnitude * Time.deltaTime * 0.1f);
                            }

                            if (Plugin.EnableParkourFOV.Value)
                            {
                                player.FOVShock(2, false);
                            }

                            foreach (var hand in player.hands)
                            {
                                if (!isConsumableUltimate)
                                {
                                    hand.gripStrength -= staminaCost;
                                    if (hand.gripStrength < 0f) hand.gripStrength = 0f;
                                }
                            }

                            isSliding = false;
                            controller.enabled = true;
                            return;
                        }
                    }
                }

                slideTime += Time.deltaTime;
                float t = slideTime / slideDuration;

                float easedT = 1 - (1 - t) * (1 - t);
                Vector3 nextPos = Vector3.Lerp(slideStartPos, slideTargetPos, (easedT * ((roidedCount / 10) + 1)));

                if (Physics.Raycast(player.transform.position + (Vector3.up * 0.05f) + (slideDir * 0.3f), slideDir, 0.8f))
                {
                    slideTargetPos = player.transform.position;
                    slideTime = slideDuration;

                    if (Plugin.EnableParkourShake.Value)
                    {
                        CL_CameraControl.Shake(Time.deltaTime * 0.1f);
                    }

                    isSliding = false;
                    controller.enabled = true;
                    return;
                }
                else if (t >= 1.0f || !player.IsCrouching() || !isGrounded)
                {
                    isSliding = false;
                    controller.enabled = true;
                }
                else
                {
                    player.transform.position = nextPos;

                    if (Plugin.EnableParkourFOV.Value)
                    {
                        player.FOVShock(Mathf.Lerp(1.5f, 0f, easedT), false);
                    }

                    if (Plugin.EnableParkourRotation.Value)
                    {
                        player.transform.rotation *= Quaternion.Euler(5f * Time.deltaTime, 0, 0);
                    }
                }
            }

            if (wasGrounded && !isGrounded)
            {
                startY = player.transform.position.y;
            }

            if (!wasGrounded && isGrounded)
            {
                if (((ConfigEntry<bool>)Plugin.SkillConfigs["Rolling"]).Value)
                {
                    float fallDistance = startY - player.transform.position.y;

                    if (fallDistance >= 15 && player.IsCrouching() && !isRotatingRoll)
                    {
                        isRotatingRoll = true;
                        rollTimer = 0f;
                        startRotationRoll = player.transform.rotation;
                        controller.enabled = false;
                        ArmRollingFallDamageCancel();

                        rollDir = player.transform.forward;
                        rollDir.y = 0;
                        rollDir.Normalize();

                        rollStartPos = player.transform.position;
                        rollTargetPos = rollStartPos + (rollDir * 5f);
                    }
                }
            }

            if (isRotatingRoll)
            {
                rollTimer += Time.deltaTime;
                float RollPercent = rollTimer / rollDur;
                float currentX = Mathf.Lerp(0f, 360f, RollPercent);
                if (Plugin.EnableParkourRotation.Value)
                {
                    player.transform.rotation = startRotationRoll * Quaternion.Euler(currentX, 0f, 0f);
                }

                if (RollPercent >= 1f)
                {
                    player.transform.rotation = startRotationRoll;
                    player.UnLock();
                    isRotatingRoll = false;
                }

                float tRoll = rollTimer / rollDur;
                float easedTRoll = 1 - (1 - tRoll) * (1 - tRoll);
                Vector3 nextPosRoll = Vector3.Lerp(rollStartPos, rollTargetPos, easedTRoll);

                if (Physics.Raycast(player.transform.position + (Vector3.up * 0.05f) + (rollDir * 0.3f), rollDir, 0.8f))
                {
                    rollTargetPos = player.transform.position;
                    rollTimer = rollDur;

                    controller.enabled = true;
                    return;
                }
                else if (tRoll >= 1.0f)
                {
                    controller.enabled = true;
                }
                else
                {
                    player.transform.position = nextPosRoll;
                }
            }

            wasGrounded = isGrounded;

            float MaxHeight = ((ConfigEntry<float>)Plugin.SkillConfigs["MaxHeight"]).Value;
            float HeightCurrency = ((ConfigEntry<float>)Plugin.SkillConfigs["HeightCurrency"]).Value;
            
            int currentMilestone = (int)(levelHighestHeight / 100);
            levelHighestHeight = player.transform.position.y;

            if (player.health <= 0)
            {
                if (levelHighestHeight > MaxHeight)
                {
                    ((ConfigEntry<float>)Plugin.SkillConfigs["MaxHeight"]).Value = levelHighestHeight;
                }

                liveRunHeight = 0f;
                highestMilestone = 0;
                return;
            }

            if (liveRunHeight < MaxHeight)
            {

                liveRunHeight = MaxHeight;
            }

            int bonusPoints = 0;

            if (currentMilestone > highestMilestone)
            {
                int gainedMilestone = currentMilestone - highestMilestone;

                previousCurrency = HeightCurrency;

                ((ConfigEntry<float>)Plugin.SkillConfigs["HeightCurrency"]).Value += gainedMilestone * 10f;

                if (levelHighestHeight > liveRunHeight && MaxHeight >= 100)
                {
                    int hundredsPlace = (int)(levelHighestHeight / 100);

                    bonusPoints += hundredsPlace;

                    if (bonusPoints > 10)
                    {
                        bonusPoints = 10;
                    }

                    ((ConfigEntry<float>)Plugin.SkillConfigs["HeightCurrency"]).Value += bonusPoints;
                    liveRunHeight = levelHighestHeight;
                }

                ParkourUI.Instance.StartCoroutine(ParkourUI.Instance.AnimateCurrencyGain((int)(previousCurrency), gainedMilestone * 10 + bonusPoints));
                
                highestMilestone = currentMilestone;
            }
        }
    }

    [HarmonyPatch(typeof(ENT_Player), "Damage")]
    public class RollingFallDamagePatch
    {
        [HarmonyPrefix]
        public static bool Prefix(Damageable.DamageInfo info, ref bool __result)
        {
            if (!PlayerModifierPatch.TryCancelRollingFallDamage(info))
            {
                return true;
            }

            __result = false;
            return false;
        }
    }

    [HarmonyPatch(typeof(EventSystem), "Update")]
    public class WorldUpdate
    {
        private static bool playerExisted = false;

        [HarmonyPrefix]
        public static void Postfix()
        {
            float levelHighestHeight = PlayerModifierPatch.levelHighestHeight;
            var player = ENT_Player.playerObject;
            float MaxHeight = ((ConfigEntry<float>)Plugin.SkillConfigs["MaxHeight"]).Value;

            if (player != null)
            {
                
                if (!playerExisted)
                {
                    playerExisted = true;
                }

                if (player?.curBuffs != null)
                {
                    var listField = player.curBuffs.GetType().GetFields((System.Reflection.BindingFlags)62).FirstOrDefault(f => f.FieldType == typeof(List<BuffContainer>));
                    var activeList = listField?.GetValue(player.curBuffs) as List<BuffContainer>;

                    var buffNames = activeList?.SelectMany(c => c?.buffs ?? new List<BuffContainer.Buff>()).Where(b => b != null && !string.IsNullOrEmpty(b.id)).Select(b => b.id);

                    PlayerModifierPatch.isConsumableUltimate = buffNames != null && buffNames.Any(name => name == "pilled" || name == "roided");
                    PlayerModifierPatch.roidedCount = buffNames != null ? buffNames.Count(name => name == "roided") : 0;

                    Debug.Log($"{PlayerModifierPatch.isConsumableUltimate}, {PlayerModifierPatch.roidedCount}");
                }
            }

            if (player == null && playerExisted)
            {
                if (levelHighestHeight > MaxHeight)
                {
                    ((ConfigEntry<float>)Plugin.SkillConfigs["MaxHeight"]).Value = levelHighestHeight;
                }

                PlayerModifierPatch.liveRunHeight = 0f;
                PlayerModifierPatch.highestMilestone = 0;
                playerExisted = false;
            }

            if (ParkourUI.hasPurchased)
            {
                ((ConfigEntry<float>)Plugin.SkillConfigs["HeightCurrency"]).Value = ParkourUI.newCurrencyAmount;
                ParkourUI.hasPurchased = false;
                ParkourUI.UpdateCurrencyDisplay();
            }
        }
    }
}