using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace ParkourKnuckle
{
    [BepInPlugin("com.nimius.parkourknuckle", "Parkour Knuckle", "1.2.1")]
    public class Plugin : BaseUnityPlugin
    {
        private Harmony _harmony;
        public static ConfigEntry<bool> EnableParkourRotation;
        void Awake()
        {
            EnableParkourRotation = Config.Bind("Camera Settings", "Use Parkour Rotation", true, "Determines whether the camera will tilt and rotate differently when doing parkour actions.");

            this._harmony = new Harmony("com.nimius.parkourknuckle");
            this._harmony.PatchAll();
            Logger.LogInfo("Harmony Patches applied successfully.");
        }
    }

    [HarmonyPatch(typeof(ENT_Player), "Update")]
    public class PlayerModifierPatch
    {
        private static Quaternion targetRotation;
        private static bool isRotating = false;
        private static float turnSpeed = 24f;
        
        private static float chargeStartTime;
        private static bool isCharging;
        private static float maxChargeTime = 5f;
        private static float leapForceMultiplier = 1.5f;
        private static float lastLeapTime = 0f;
        private static float leapCooldown = 3f;
        private static float minStamina = 1;
        private static float maxStamina = 8;
        private static float upwardArcForce = 1f;
        private static bool isHolding = false;

        private static bool hasWallRunInAir = false;
        private static float tiltLerpSpeed = 4f;
        private static float gripValue = 0f;
        private static bool isHorizRun = false;
        private static int spaceTapCount = 0;
        private static float spaceTapTimer = 0f;
        private const float doubleTapWindow = 0.3f;

        private static bool isVerticalRun = false;
        private static bool hasWallRunVertical = false;
        private static float verticalGraceTimer = 0f;
        private static float maxGraceTime = 0.2f;

        private static bool isVaulting = false;
        private static Vector3 vaultTargetPos;
        private static float vaultTimer = 0f;
        private static float vaultDuration = 0.2f;

        private static bool isSliding = false;
        private static float minSlideSpeed = 6f;
        private static Vector3 slideDir;
        private static bool canSlide = true;
        private static float slideTime = 0f;
        private static float slideDuration = 1.8f;
        private static Vector3 slideStartPos;
        private static Vector3 slideTargetPos;

        [HarmonyPostfix]
        public static void Postfix(ENT_Player __instance)
        {
            CL_GameManager.SetGameFlag("leaderboardIllegal", true);
            var player = __instance;
            bool onCooldown = Time.time < lastLeapTime + leapCooldown;
            var controller = player.GetComponent<CharacterController>();
            bool isGrounded = controller.isGrounded;
            Debug.Log($"{isHolding}");
            isHolding = false;

            foreach (var hand in player.hands)
            {
                if (hand.handhold != null && hand.handhold.GetHolding())
                {
                    isHolding = true;
                    break;
                }                
            }

            if (Input.GetKeyUp(KeyCode.X) && !onCooldown && !isHorizRun && player.health > 0f && (isGrounded || isHolding))
            {
                targetRotation = player.transform.rotation * Quaternion.Euler(0f, 180f, 0f);
                isRotating = true;
            }

            if (isRotating)
            {
                player.transform.rotation = Quaternion.Slerp(player.transform.rotation, targetRotation, Time.deltaTime * turnSpeed);

                if (Quaternion.Angle(player.transform.rotation, targetRotation) < 0.5f)
                {
                    isRotating = false;
                }
            }

            if (Input.GetKeyDown(KeyCode.G) && (isGrounded || isHolding))
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

            if (isCharging && Input.GetKey(KeyCode.G) && (isGrounded || isHolding))
            {
                float currentCharge = Mathf.Min(Time.time - chargeStartTime, maxChargeTime);
                CL_CameraControl.Shake(currentCharge * 0.001f);

                foreach (var hand in player.hands)
                {
                    hand.ShakeHand(currentCharge * 0.001f);
                }
            }

            if (Input.GetKeyUp(KeyCode.G) && isCharging && (isGrounded || isHolding))
            {
                if (!isGrounded && !isHolding)
                {
                    isCharging = false;
                    return;
                }
                float currentCharge = Mathf.Min(Time.time - chargeStartTime, maxChargeTime);
                float chargeDuration = Mathf.Min(Time.time - chargeStartTime, maxChargeTime);
                float finalForce = chargeDuration * leapForceMultiplier;
                float finalCost = Mathf.CeilToInt(Mathf.Lerp(minStamina, maxStamina, chargeDuration / maxChargeTime));

                Vector3 leapDirection = player.cam.transform.forward + (Vector3.up * upwardArcForce);
                Vector3 leapVelocity = leapDirection.normalized * finalForce;

                player.SetDirectionalForce(leapVelocity);

                foreach (var hand in player.hands)
                {
                    hand.gripStrength -= finalCost;
                    if (hand.gripStrength < 0f)
                    {
                        hand.gripStrength = 0f;
                    }

                    if (hand.IsHolding())
                    {
                        hand.DropHand(true);
                    }
                }

                isCharging = false;
            }

            if (Input.GetKeyDown(KeyCode.Space) && Input.GetKey(KeyCode.S) && !Input.GetKey(KeyCode.W))
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
                        float kickForce = 1.5f;

                        player.SetDirectionalForce(kickDir.normalized * kickForce);

                        CL_CameraControl.Shake(0.03f);

                        foreach (var hand in player.hands)
                        {
                            hand.gripStrength -= 1.0f;
                            if (hand.gripStrength < 0f) hand.gripStrength = 0f;
                        }
                    }
                }
            }

            if (isGrounded && !isVerticalRun)
            {
                isHorizRun = false;
                isVerticalRun = false;
                hasWallRunInAir = false;
                hasWallRunVertical = false;
                controller.enabled = true;
                player.transform.rotation = Quaternion.Euler(0, player.transform.eulerAngles.y, 0);
            }

            bool runStamina = true;

            foreach (var hand in player.hands)
            {

                gripValue = hand.gripStrength;

                if (hand.gripStrength <= 0f)
                {
                    runStamina = false;
                }
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
                    player.SetDirectionalForce(((runDir * 0.8f) * (gripValue * 0.1f) + (Vector3.up * 0.1f)));

                    CL_CameraControl.Shake(0.005f);
                    foreach (var hand in player.hands)
                    {
                        hand.gripStrength -= 0.075f;
                        if (hand.gripStrength < 0) hand.gripStrength = 0;
                    }
                }
                else
                {
                    isHorizRun = false;
                    hasWallRunInAir = true;
                    controller.enabled = true;
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

            if (isVerticalRun && Input.GetKeyDown(KeyCode.Space) && verticalGraceTimer > 0 && verticalGraceTimer < maxGraceTime)
            {
                float pushAwayForce = 1.5f;
                float upwardArcForce = 1.5f;

                Vector3 jumpOffDir = ((hitRun.normal * pushAwayForce) * (gripValue * 0.1f)) + ((Vector3.up * upwardArcForce) * (gripValue * 0.1f));
                player.SetDirectionalForce(jumpOffDir);

                isVerticalRun = false;
                verticalGraceTimer = 0;
                hasWallRunVertical = true;
                controller.enabled = true;
                return;
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
                float progress = vaultTimer / vaultDuration;

                player.transform.position = Vector3.Lerp(player.transform.position, vaultTargetPos, 2f);

                if (Vector3.Distance(player.transform.position, vaultTargetPos) < 0.1f)
                {
                    isVaulting = false;
                    controller.enabled = true;
                }

                return;
            }
            
            if (isHoldingRun && !hasWallRunVertical && wallFront && (!hasWallRunVertical || isVerticalRun) && (verticalGraceTimer == 0f))
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
                            if (Physics.SphereCast((player.transform.position + (Vector3.up * 1.0f)), 0.3f, Vector3.up, out RaycastHit hitAboveRun, 0.7f))
                            {
                                isVaulting = false;
                                hasWallRunVertical = true;
                                isVerticalRun = false;
                                controller.enabled = true;
                                return;
                            }
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

                                    if (!Physics.SphereCast(ledgeHit.point + (Vector3.up * 0.1f), 0.3f, Vector3.up, out RaycastHit ceilCheck, 1.8f))
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

                        Vector3 climbVelocity = (Vector3.up * 0.4f) * (gripValue * 0.15f);
                        player.SetDirectionalForce(climbVelocity);

                        CL_CameraControl.Shake(0.008f);
                        foreach (var hand in player.hands)
                        {
                            hand.gripStrength -= 0.09f;
                            if (hand.gripStrength < 0f) hand.gripStrength = 0f;
                        }
                        return;
                    }
                }
            }

            if (!isGrounded && !isVerticalRun && !isVaulting && verticalGraceTimer <= 0 && spaceTapTimer <= 0)
            {
                if (controller.enabled)
                {
                    hasWallRunVertical = true;
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
                if (currentSpeed >= minSlideSpeed && isMovingForward)
                {
                    isSliding = true;
                    canSlide = false;
                    slideDir = horizontalVel.normalized;
                    slideTime = 0f;

                    slideStartPos = player.transform.position;
                    slideTargetPos = slideStartPos + (slideDir * 10f);

                    controller.enabled = false;
                    CL_CameraControl.Shake(0.01f);
                }
            }

            if (isSliding)
            {
                if (Input.GetKeyDown(KeyCode.Space))
                {

                    float slideRemnant = 1f - (slideTime / slideDuration);
                    float lungeForwardPower = 1f * slideRemnant;
                    float lungUpwardPower = 1.5f * slideRemnant;
                    Vector3 lungeVelocity = (slideDir * lungeForwardPower) + (Vector3.up * lungUpwardPower);

                    float staminaCost = lungeVelocity.magnitude * 1f;
                    bool hasEnoughStamina = true;
                    foreach (var hand in player.hands)
                    {
                        if (hand.gripStrength < staminaCost)
                        {
                            hasEnoughStamina = false;
                            break;
                        }
                    }

                    if (hasEnoughStamina)
                    {
                        player.SetDirectionalForce(lungeVelocity);
                        CL_CameraControl.Shake(lungeVelocity.magnitude * 0.01f);

                        foreach (var hand in player.hands)
                        {
                            hand.gripStrength -= staminaCost;
                            if (hand.gripStrength < 0f) hand.gripStrength = 0f;
                        }

                        isSliding = false;
                        controller.enabled = true;
                        return;
                    }
                }

                slideTime += Time.deltaTime;
                float t = slideTime / slideDuration;

                float easedT = 1 - (1 - t) * (1 - t);
                Vector3 nextPos = Vector3.Lerp(slideStartPos, slideTargetPos, easedT);

                if (Physics.Raycast(player.transform.position + (Vector3.up * 0.05f) + (slideDir * 0.3f), slideDir, 0.8f))
                {
                    slideTargetPos = player.transform.position;
                    slideTime = slideDuration;

                    CL_CameraControl.Shake(0.03f);

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

                    if (Plugin.EnableParkourRotation.Value)
                    {
                        player.transform.rotation *= Quaternion.Euler(5f * Time.deltaTime, 0, 0);
                    }
                }
            }
        }
    }
}