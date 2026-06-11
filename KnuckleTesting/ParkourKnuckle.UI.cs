using BepInEx.Configuration;
using DarkMachine.UI;
using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ParkourKnuckle.UI
{
    public class ParkourUI : MonoBehaviour
    {
        public static ParkourUI Instance { get; private set; }

        private const float percentLineFillRequiredToUnlock = 0.50f;

        private const float popInDuration = 0.25f;
        private const float popOutDuration = 0.18f;

        private const float nodeAnimateDuration = 0.35f;

        private GameObject uiInstance;
        private GameObject skillTreePanel;
        private GameObject openButtonObj;
        private GameObject closeButtonObj;
        private Transform contentTransform;
        private GameObject clickOutsideBlocker;
        private static TextMeshProUGUI currencyTextComponent;
        private static TextMeshProUGUI heightTextComponent;
        private GameObject currencyIconObj;
        private GameObject currencyAddObj;
        private TextMeshProUGUI mainText;
        private TextMeshProUGUI infoText;
        private CanvasGroup currencyAddCanvasGroup;
        private GameObject resetConfirmBox;
        private Button confirmButton;
        private Button denyButton;
        private GameObject abilityTimesContainer;
        private GameObject leapAbilityBox;
        private GameObject qtAbilityBox;
        private Image leapAbilityTimer;
        private Image leapForceGlow;
        private Image qtAbilityTimer;
        private GameObject loadingScreen;
        private GameObject loadingImages;
        private TextMeshProUGUI mapLoadingText;
        private TextMeshProUGUI seedLoadingText;
        private TextMeshProUGUI idLoadingText;
        private GameObject wallRunFadeHelper;
        private GameObject wallRunLeft;
        private GameObject wallRunRight;
        private GameObject wallRunUp;
        private CanvasGroup wallRunLeftGroup = null;
        private CanvasGroup wallRunRightGroup = null;
        private CanvasGroup wallRunUpGroup = null;

        private GameObject optionsContentObj;
        private GameObject forwardButtonObj;
        private GameObject backButtonObj;
        private Transform headerGroup;
        private TextMeshProUGUI textHeaderComponent;
        private Toggle cameraRotationToggle;
        private Toggle cameraFOVToggle;
        private Toggle cameraShakeToggle;
        private Toggle uiGlowToggle;
        private Button resetProgressButton;

        private GameObject descriptionPanelObj;
        private TextMeshProUGUI perkDescriptionText;
        private Button purchaseButton;
        private TextMeshProUGUI purchaseButtonText;
        private TextMeshProUGUI perkCurrencyText;
        private GameObject perkCurrencyIcon;
        private string currentlySelectedSkillKey = string.Empty;

        private bool pendingPurchaseAnimation = false;

        private readonly Dictionary<string, Button> skillButtons = new Dictionary<string, Button>();
        private readonly Dictionary<string, string[]> skillDependencies = new Dictionary<string, string[]>();
        private readonly Dictionary<string, Image> skillLines = new Dictionary<string, Image>();
        private readonly List<Coroutine> activeAnimations = new List<Coroutine>();
        private readonly Dictionary<string, Coroutine> activeButtonCoroutines = new Dictionary<string, Coroutine>();
        private readonly Dictionary<string, GameObject> graffitiScreens = new Dictionary<string, GameObject>();

        public static bool hasPurchased = false;
        public static float newCurrencyAmount = 0;
        public static bool purchaseTrue = true;

        private readonly Dictionary<string, string> perkDescriptions = new Dictionary<string, string>()
        {
            { "QuickTurning", "You can quick turn 180 degrees at any point with no debuff by tapping X. This may help save time when you need to turn around at fast intervals." },
            { "Sliding", "While running (or moving at a high velocity), hold down the crouch key to slide." },
            { "SlideJumping", "During a slide, you can jump to gain a boost. This boost does consume hand stamina. As the duration of your slide grows, less power will be put into the boost, however, less stamina will be taken." },
            { "Leaping", "Holding G will cause you to charge up a leap. The longer you charge your leap, the further your leap will go, but your stamina will be consumed the more you charge your leap." },
            { "Rolling", "While you are falling from great height, hold down your crouch button to initiate a roll when you land. This takes no stamina and prevents fall damage from the landing impact." },
            { "WallKicking", "While your back is turned away from a wall, hold S and press the spacebar to kick off of that wall. holding down S after the kick will allow you to control your distance while in the air." },
            { "WallRunning", "You can run on the side of walls by standing close to a wall and double tapping and holding the spacebar and holding A or D depending on which side of the wall you are standing near. This consumes very little hand stamina." },
            { "VerticalWallRunning", "You have the ability to run up to walls and climb them without needing any tools whatsoever. Double tap and hold spacebar and hold W at the same time while facing a wall to begin a vertical wall run. This consumes a moderate amount of hand stamina.\r\n\r\n" },
            { "WallRunBoost", "While vertically wall running, you can release W and quickly tap spacebar to kick off of the wall and give yourself a backwards boost. This does not use up your stamina, however, the power of the boost is determined by your hand stamina." }
        };

        private readonly Dictionary<string, string> configToIconMap = new Dictionary<string, string>()
        {
            { "QuickTurning", "QuickTurnIcon" },
            { "Sliding", "SlideIcon" },
            { "SlideJumping", "SlideJumpIcon" },
            { "Leaping", "LeapIcon" },
            { "Rolling", "RollIcon" },
            { "WallKicking", "WallKickIcon" },
            { "WallRunning", "WRHIcon" },
            { "VerticalWallRunning", "WRVIcon" },
            { "WallRunBoost", "WRBIcon" }
        };

        private readonly Dictionary<string, float> skillPrices = new Dictionary<string, float>()
        {
            { "QuickTurning", 0f },
            { "Sliding", 15f },
            { "SlideJumping", 30f },
            { "Leaping", 100f },
            { "Rolling", 75f },
            { "WallKicking", 30f },
            { "WallRunning", 50f },
            { "VerticalWallRunning", 60f },
            { "WallRunBoost", 75f }
        };

        private Coroutine activePanelPopCoroutine;
        private Coroutine activeButtonPopCoroutine;

        private bool isAnimatingBranch = false;

        private bool wasPlayerObjectPresent = false;

        private const float wobbleSpeed = 2.5f;
        private const float wobbleMagnitude = 4.0f;
        private const float noiseSpacing = 100.0f;

        private const float FADE_SPEED = 5f;

        private readonly Dictionary<string, Vector2> nodeOriginPositions = new Dictionary<string, Vector2>();
        private RectTransform quickTurnRect;
        private readonly Dictionary<string, Vector2> lineOriginPositions = new Dictionary<string, Vector2>();
        private RectTransform lineToSlideRect;

        private GameObject perkIconHolderObj;
        private readonly Dictionary<string, GameObject> perkIcons = new Dictionary<string, GameObject>();

        private bool hadLoading = false;
        private string activeGraffitiName = null;
        private float playerCheckTimer = 0f;
        private const float PLAYER_CHECK_INTERVAL = 0.2f;
        private bool cachedIsPlayerPresent = false;
        private CL_GameManager cachedGameManager = null;
        private RectTransform cachedPerkIconRect = null;

        public static void Initialize()
        {
            if (Instance != null) return;

            GameObject uiHost = new GameObject("ParkourKnuckle_UIHost");
            DontDestroyOnLoad(uiHost);

            Instance = uiHost.AddComponent<ParkourUI>();
            Instance.LoadAndBuildUI();
        }

        private void Update()
        {
            playerCheckTimer += Time.deltaTime;
            if (playerCheckTimer >= PLAYER_CHECK_INTERVAL)
            {
                playerCheckTimer = 0f;
                cachedIsPlayerPresent = GameObject.Find("CL_Player") != null;
            }
            bool isPlayerObjectPresent = cachedIsPlayerPresent;

            if (isPlayerObjectPresent && !wasPlayerObjectPresent)
            {
                wasPlayerObjectPresent = true;
                if (Plugin.isUIVisible) ToggleUIPanel(false);
                if (openButtonObj != null) openButtonObj.SetActive(false);

                if (currencyIconObj != null) currencyIconObj.SetActive(false);

                if (abilityTimesContainer != null)
                {
                    abilityTimesContainer.SetActive(true);
                }

                if (wallRunFadeHelper != null)
                {
                    wallRunFadeHelper.SetActive(true);
                }
            }
            else if (!isPlayerObjectPresent && wasPlayerObjectPresent)
            {
                wasPlayerObjectPresent = false;
                if (openButtonObj != null && !Plugin.isUIVisible)
                {
                    openButtonObj.SetActive(true);
                    openButtonObj.transform.localScale = Vector3.one;
                }

                if (currencyIconObj != null)
                {
                    currencyIconObj.SetActive(true);
                    UpdateCurrencyDisplay();
                }

                if (abilityTimesContainer != null)
                {
                    abilityTimesContainer.SetActive(false);
                }

                if (wallRunFadeHelper != null)
                {
                    wallRunFadeHelper.SetActive(false);
                }
            }

            if (isPlayerObjectPresent)
            {
                if (qtAbilityBox != null)
                {
                    if ((((ConfigEntry<bool>)Plugin.SkillConfigs["QuickTurning"]).Value) && ENT_Player.playerObject.health > 0f && !cachedGameManager.isPaused) qtAbilityBox.SetActive(true);
                    else qtAbilityBox.SetActive(false);

                    if (qtAbilityTimer != null && ParkourKnuckle.PlayerModifierPatch.onCooldown)
                    {
                        qtAbilityTimer.fillAmount = ParkourKnuckle.PlayerModifierPatch.CooldownTime / ParkourKnuckle.PlayerModifierPatch.CooldownDur;
                    }
                    else if (qtAbilityTimer != null)
                    {
                        qtAbilityTimer.fillAmount = 1;
                    }
                }

                if (leapAbilityBox != null)
                {
                    if ((((ConfigEntry<bool>)Plugin.SkillConfigs["Leaping"]).Value) && ENT_Player.playerObject.health > 0f && !cachedGameManager.isPaused) leapAbilityBox.SetActive(true);
                    else leapAbilityBox.SetActive(false);

                    if (leapAbilityTimer != null && ParkourKnuckle.PlayerModifierPatch.leapCooldown)
                    {
                        leapAbilityTimer.fillAmount = ParkourKnuckle.PlayerModifierPatch.leapCooldownTime / ParkourKnuckle.PlayerModifierPatch.leapCooldownDur;
                    }
                    else if (leapAbilityTimer != null)
                    {
                        leapAbilityTimer.fillAmount = 1;
                    }

                    if (leapForceGlow != null && ParkourKnuckle.PlayerModifierPatch.isCharging)
                    {
                        leapForceGlow.fillAmount = ParkourKnuckle.PlayerModifierPatch.currentCharge / ParkourKnuckle.PlayerModifierPatch.maxChargeTime;
                        leapForceGlow.color = new Color(leapForceGlow.color.r, leapForceGlow.color.g, leapForceGlow.color.b, Mathf.Max(0.1f, leapForceGlow.fillAmount / 2f));
                    }
                    else if (leapForceGlow != null)
                    {
                        leapForceGlow.fillAmount = 0;
                    }
                }

                if (currencyIconObj != null && (ENT_Player.playerObject.health <= 0 || cachedGameManager.isPaused))
                {
                    currencyIconObj.SetActive(true);
                } else if (currencyIconObj != null && (ENT_Player.playerObject.health > 0 && !cachedGameManager.isPaused))
                {
                    currencyIconObj.SetActive(false);
                }

                if (wallRunFadeHelper != null && Plugin.EnableWallRunHelper.Value)
                {
                    if (wallRunLeftGroup != null)
                    {
                        bool shouldShowLeft = PlayerModifierPatch.canRun && PlayerModifierPatch.wallLeft && !PlayerModifierPatch.hasWallRunInAir && !PlayerModifierPatch.isHorizRun;
                        float targetAlphaLeft = shouldShowLeft ? 1f : 0f;
                        wallRunLeftGroup.alpha = Mathf.MoveTowards(wallRunLeftGroup.alpha, targetAlphaLeft, FADE_SPEED * Time.deltaTime);
                    }

                    if (wallRunRightGroup != null)
                    {
                        bool shouldShowRight = PlayerModifierPatch.canRun && PlayerModifierPatch.wallRight && !PlayerModifierPatch.hasWallRunInAir && !PlayerModifierPatch.isHorizRun;
                        float targetAlphaRight = shouldShowRight ? 1f : 0f;
                        wallRunRightGroup.alpha = Mathf.MoveTowards(wallRunRightGroup.alpha, targetAlphaRight, FADE_SPEED * Time.deltaTime);
                    }

                    if (wallRunUpGroup != null)
                    {
                        bool shouldShowUp = PlayerModifierPatch.canVert && PlayerModifierPatch.wallFront && !PlayerModifierPatch.hasWallRunVertical && !PlayerModifierPatch.isVerticalRun;
                        float targetAlphaUp = shouldShowUp ? 1f : 0f;
                        wallRunUpGroup.alpha = Mathf.MoveTowards(wallRunUpGroup.alpha, targetAlphaUp, FADE_SPEED * Time.deltaTime);
                    }

                    if (cachedGameManager != null)
                    {
                        if (cachedGameManager.isPaused)
                        {
                            wallRunFadeHelper.SetActive(false);
                        } else if (ENT_Player.playerObject.health <= 0)
                        {
                            wallRunFadeHelper.SetActive(false);
                        } else
                        {
                            wallRunFadeHelper.SetActive(true);
                        }
                    }
                } else if (wallRunFadeHelper != null && !Plugin.EnableWallRunHelper.Value)
                {
                    wallRunFadeHelper.SetActive(false);
                }

                if (openButtonObj.activeSelf)
                {
                    openButtonObj.SetActive(false);
                }
            }

            if (cachedGameManager == null)
            {
                cachedGameManager = CL_GameManager.FindObjectOfType<CL_GameManager>();
            }

            if (cachedGameManager != null)
            {
                if (CL_GameManager.IsLoading() && !hadLoading)
                {
                    hadLoading = true;
                    loadingScreen.SetActive(true);

                    int randomNumber = UnityEngine.Random.Range(1, 10);
                    string targetName = "Graffiti" + randomNumber;

                    if (graffitiScreens.TryGetValue(targetName, out GameObject screen))
                    {
                        screen.SetActive(true);
                        activeGraffitiName = targetName;
                    }
                }
                else if (!CL_GameManager.IsLoading() && hadLoading)
                {
                    hadLoading = false;
                    mapLoadingText.text = "";
                    seedLoadingText.text = "";
                    idLoadingText.text = "";
                    loadingScreen.SetActive(false);

                    if (!string.IsNullOrEmpty(activeGraffitiName))
                    {
                        if (graffitiScreens.TryGetValue(activeGraffitiName, out GameObject screen))
                        {
                            screen.SetActive(false);
                        }

                        activeGraffitiName = null;
                    }
                }
            }

            if (cachedGameManager != null && CL_GameManager.IsLoading())
            {
                M_Level currentLevel = M_Level.FindAnyObjectByType<M_Level>();
                
                if (currentLevel != null)
                {
                    string foundMapName = currentLevel.levelName;
                    int foundSeedNumber = currentLevel.GetLevelSeed();
                    int foundIDNumber = currentLevel.GetInstanceID();

                    mapLoadingText.text = foundMapName;
                    seedLoadingText.text = foundSeedNumber.ToString();
                    idLoadingText.text = foundIDNumber.ToString();
                }
            }

            if (Plugin.isUIVisible && !isAnimatingBranch)
            {
                ApplyProceduralWobble();
            }

            if (Plugin.isUIVisible && perkIconHolderObj != null && perkIconHolderObj.activeSelf)
            {
                float timeEngine = Time.time * wobbleSpeed;

                if (cachedPerkIconRect == null)
                {
                    cachedPerkIconRect = perkIconHolderObj.GetComponent<RectTransform>();
                }

                float noiseX = Mathf.Sin(timeEngine) * wobbleMagnitude;
                float noiseY = Mathf.Cos(timeEngine * 0.8f) * wobbleMagnitude;

                if (cachedPerkIconRect != null)
                {
                    cachedPerkIconRect.anchoredPosition = new Vector2(noiseX, noiseY);
                }
            }
        }

        private void ApplyProceduralWobble()
        {
            float timeEngine = Time.time * wobbleSpeed;

            foreach (var kvp in skillButtons)
            {
                string configKey = kvp.Key;
                Button btn = kvp.Value;

                if (!(activeButtonCoroutines.TryGetValue(configKey, out var routine) && routine != null))
                {
                    RectTransform rect = btn.GetComponent<RectTransform>();
                    if (rect != null && nodeOriginPositions.TryGetValue(configKey, out Vector2 origin))
                    {
                        Vector2 sampleCoordinates;

                        if (IsConnectedToRoot(configKey) || AreDependenciesMet(configKey))
                        {
                            sampleCoordinates = Vector2.zero;
                        }
                        else
                        {
                            sampleCoordinates = origin * 0.005f;
                        }

                        float noiseX = (Mathf.PerlinNoise(sampleCoordinates.x + timeEngine, sampleCoordinates.y) * 2f) - 1f;
                        float noiseY = (Mathf.PerlinNoise(sampleCoordinates.x, sampleCoordinates.y + timeEngine) * 2f) - 1f;

                        Vector2 offset = new Vector2(noiseX, noiseY) * wobbleMagnitude;
                        rect.anchoredPosition = origin + offset;
                    }
                }

                if (skillLines.TryGetValue(configKey, out Image lineImg))
                {
                    RectTransform lineRect = lineImg.GetComponent<RectTransform>();
                    if (lineRect != null && lineOriginPositions.TryGetValue(configKey, out Vector2 lineOrigin))
                    {
                        if (IsConnectedToRoot(configKey) || AreDependenciesMet(configKey))
                        {
                            Vector2 lineSampleCoords = Vector2.zero;

                            float lineNoiseX = (Mathf.PerlinNoise(lineSampleCoords.x + timeEngine, lineSampleCoords.y) * 2f) - 1f;
                            float lineNoiseY = (Mathf.PerlinNoise(lineSampleCoords.x, lineSampleCoords.y + timeEngine) * 2f) - 1f;

                            Vector2 lineOffset = new Vector2(lineNoiseX, lineNoiseY) * wobbleMagnitude;
                            lineRect.anchoredPosition = lineOrigin + lineOffset;
                        }
                        else
                        {
                            lineRect.anchoredPosition = lineOrigin;
                        }
                    }
                }
            }
        }

        private bool IsConnectedToRoot(string configKey)
        {
            if (configKey == "QuickTurning")
            {
                if (Plugin.SkillConfigs.TryGetValue("QuickTurning", out var qtConfig))
                {
                    return ((ConfigEntry<bool>)qtConfig).Value;
                }
                return false;
            }

            if (Plugin.SkillConfigs.TryGetValue(configKey, out var config) && !((ConfigEntry<bool>)config).Value)
            {
                return false;
            }

            if (skillDependencies.TryGetValue(configKey, out string[] prerequisites))
            {
                foreach (string parentKey in prerequisites)
                {
                    if (IsConnectedToRoot(parentKey))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private void LoadAndBuildUI()
        {
            string modPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string bundlePath = Path.Combine(modPath, "Assets", "parkourui");

            if (!File.Exists(bundlePath))
            {
                CleanUpFailedInit();
                return;
            }

            AssetBundle bundle = AssetBundle.LoadFromFile(bundlePath);
            if (bundle == null)
            {
                CleanUpFailedInit();
                return;
            }

            GameObject uiPrefab = null;
            string[] assetNames = bundle.GetAllAssetNames();
            if (assetNames.Length > 0)
            {
                uiPrefab = bundle.LoadAsset<GameObject>(assetNames[0]);
            }

            if (uiPrefab == null)
            {
                bundle.Unload(false);
                CleanUpFailedInit();
                return;
            }

            uiInstance = Instantiate(uiPrefab);
            DontDestroyOnLoad(uiInstance);
            bundle.Unload(false);

            Canvas ourCanvas = uiInstance.GetComponent<Canvas>();
            if (ourCanvas != null)
            {
                ourCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                ourCanvas.worldCamera = null;
                ourCanvas.sortingOrder = 32767;
            }

            CanvasScaler scaler = uiInstance.GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f;
            }

            openButtonObj = uiInstance.transform.Find("OpenSTButton")?.gameObject;
            skillTreePanel = uiInstance.transform.Find("STMainBorder")?.gameObject;
            currencyIconObj = uiInstance.transform.Find("CurrencyIcon")?.gameObject;
            currencyAddObj = uiInstance.transform.Find("CurrencyAdd")?.gameObject;
            abilityTimesContainer = uiInstance.transform.Find("AbilityTimes")?.gameObject;
            loadingScreen = uiInstance.transform.Find("LoadingScreen")?.gameObject;
            wallRunFadeHelper = uiInstance.transform.Find("WallRunHelper")?.gameObject;

            if (skillTreePanel != null)
            {
                closeButtonObj = skillTreePanel.transform.Find("CloseSTButton")?.gameObject;

                contentTransform = skillTreePanel.transform.Find("PerkContent");
                if (contentTransform == null)
                {
                    contentTransform = skillTreePanel.transform.Find("Content");
                }

                optionsContentObj = skillTreePanel.transform.Find("OptionsContent")?.gameObject;
                descriptionPanelObj = skillTreePanel.transform.Find("PerkDescription")?.gameObject;
                resetConfirmBox = skillTreePanel.transform.Find("ResetConfirmBox")?.gameObject;

                if (resetConfirmBox != null)
                {
                    resetConfirmBox.SetActive(false);
                    confirmButton = resetConfirmBox.transform.Find("ConfirmButton")?.GetComponent<Button>();
                    denyButton = resetConfirmBox.transform.Find("DenyButton")?.GetComponent<Button>();

                    if (confirmButton != null)
                    {
                        confirmButton.onClick.RemoveAllListeners();
                        confirmButton.onClick.AddListener(HandleResetConfirmed);
                    }

                    if (denyButton != null)
                    {
                        denyButton.onClick.RemoveAllListeners();
                        denyButton.onClick.AddListener(HandleResetDenied);
                    }
                }

                headerGroup = skillTreePanel.transform.Find("STMainTextHeader");
                if (headerGroup != null)
                {
                    forwardButtonObj = headerGroup.Find("ForwardButton")?.gameObject;
                    backButtonObj = headerGroup.Find("BackButton")?.gameObject;
                    textHeaderComponent = headerGroup.GetComponentInChildren<TextMeshProUGUI>(true);
                }

                if (optionsContentObj != null)
                {
                    cameraRotationToggle = optionsContentObj.transform.Find("EPRToggle")?.GetComponent<Toggle>();
                    cameraFOVToggle = optionsContentObj.transform.Find("EPFToggle")?.GetComponent<Toggle>();
                    cameraShakeToggle = optionsContentObj.transform.Find("EPSToggle")?.GetComponent<Toggle>();
                    uiGlowToggle = optionsContentObj.transform.Find("EWRGToggle")?.GetComponent<Toggle>();
                    resetProgressButton = optionsContentObj.transform.Find("ResetButton")?.GetComponent<Button>();
                }

                if (descriptionPanelObj != null)
                {
                    perkDescriptionText = descriptionPanelObj.transform.Find("PerkDescText")?.GetComponent<TextMeshProUGUI>();
                    purchaseButton = descriptionPanelObj.transform.Find("PurchaseButton")?.GetComponent<Button>();

                    if (purchaseButton != null)
                    {
                        purchaseButtonText = purchaseButton.transform.Find("PurchaseText")?.GetComponent<TextMeshProUGUI>();
                    }
                }
            }

            CreateClickOutsideBlocker();
            MapInterfaceControls();
            MapProgressionNodes();

            bool playerObjectDetectedOnStartup = GameObject.Find("CL_Player") != null;
            wasPlayerObjectPresent = playerObjectDetectedOnStartup;

            if (openButtonObj != null)
            {
                openButtonObj.transform.localScale = Plugin.isUIVisible ? Vector3.zero : Vector3.one;
                openButtonObj.SetActive(!Plugin.isUIVisible && !playerObjectDetectedOnStartup);
            }

            if (currencyIconObj != null)
            {
                currencyTextComponent = currencyIconObj.transform.Find("CurrencyText")?.GetComponent<TextMeshProUGUI>();
                heightTextComponent = currencyIconObj.transform.Find("HeightText")?.GetComponent<TextMeshProUGUI>();
            }

            UpdateCurrencyDisplay();

            if (skillTreePanel != null)
            {
                skillTreePanel.transform.localScale = Plugin.isUIVisible ? Vector3.one : Vector3.zero;
                skillTreePanel.SetActive(Plugin.isUIVisible);
            }

            clickOutsideBlocker?.SetActive(Plugin.isUIVisible);
            SwitchSubMenuContext(true);

            perkIconHolderObj = descriptionPanelObj.transform.Find("PerkIconHolder")?.gameObject;

            if (perkIconHolderObj != null)
            {
                foreach (Transform child in perkIconHolderObj.transform)
                {
                    if (child.name == "PerkCurrencyAmount" || child.name == "PerkCurrencyIcon")
                        continue;

                    perkIcons[child.name] = child.gameObject;
                    child.gameObject.SetActive(false);
                }

                perkCurrencyText = perkIconHolderObj.transform.Find("PerkCurrencyAmount")?.GetComponent<TextMeshProUGUI>();
                perkCurrencyIcon = perkIconHolderObj.transform.Find("PerkCurrencyIcon")?.gameObject;
            }

            currencyAddObj = uiInstance.transform.Find("CurrencyAdd")?.gameObject;
            if (currencyAddObj != null)
            {
                currencyAddCanvasGroup = currencyAddObj.GetComponent<CanvasGroup>();
                if (currencyAddCanvasGroup == null) currencyAddCanvasGroup = currencyAddObj.AddComponent<CanvasGroup>();

                mainText = currencyAddObj.transform.Find("CurrencyAddTextMain")?.GetComponent<TextMeshProUGUI>();
                infoText = currencyAddObj.transform.Find("CurrencyAddTextInto")?.GetComponent<TextMeshProUGUI>();
                currencyAddObj.SetActive(false);
            }

            if (abilityTimesContainer != null)
            {
                leapAbilityBox = abilityTimesContainer.transform.Find("LeapAbility").gameObject;
                qtAbilityBox = abilityTimesContainer.transform.Find("QTAbility").gameObject;

                if (leapAbilityBox != null)
                {
                    leapAbilityTimer = leapAbilityBox.transform.Find("LeapTimer").GetComponent<Image>();
                    leapForceGlow = leapAbilityBox.transform.Find("LeapForceGlow").GetComponent<Image>();
                    leapAbilityBox.SetActive(false);
                }   
                
                if (qtAbilityBox != null)
                {
                    qtAbilityTimer = qtAbilityBox.transform.Find("QTTimer").GetComponent<Image>();
                    leapAbilityBox.SetActive(false);
                }
                
                abilityTimesContainer.SetActive(false);
            }

            if (loadingScreen != null)
            {
                loadingImages = loadingScreen.transform.Find("LoadingImages").gameObject;
                foreach (Transform child in loadingImages.transform)
                {
                    if (child.name.StartsWith("Graffiti"))
                    {
                        graffitiScreens[child.name] = child.gameObject;
                        child.gameObject.SetActive(false);
                    }
                }

                mapLoadingText = loadingScreen.transform.Find("MapText").GetComponent<TextMeshProUGUI>();
                mapLoadingText.text = "";
                seedLoadingText = loadingScreen.transform.Find("SeedText").GetComponent<TextMeshProUGUI>();
                seedLoadingText.text = "";
                idLoadingText = loadingScreen.transform.Find("MapIDText").GetComponent<TextMeshProUGUI>();
                idLoadingText.text = "";
                loadingScreen.SetActive(false);
            }

            if (wallRunFadeHelper != null)
            {
                wallRunFadeHelper.SetActive(false);
                wallRunLeft = wallRunFadeHelper.transform.Find("WallRunLeft")?.gameObject;
                wallRunRight = wallRunFadeHelper.transform.Find("WallRunRight")?.gameObject;
                wallRunUp = wallRunFadeHelper.transform.Find("WallRunUp")?.gameObject;

                if (wallRunLeft != null) wallRunLeftGroup = wallRunLeft.GetComponent<CanvasGroup>() ?? wallRunLeft.AddComponent<CanvasGroup>();
                if (wallRunRight != null) wallRunRightGroup = wallRunRight.GetComponent<CanvasGroup>() ?? wallRunRight.AddComponent<CanvasGroup>();
                if (wallRunUp != null) wallRunUpGroup = wallRunUp.GetComponent<CanvasGroup>() ?? wallRunUp.AddComponent<CanvasGroup>();

                wallRunLeftGroup.alpha = 0f;
                wallRunRightGroup.alpha = 0f;
                wallRunUpGroup.alpha = 0f;
            }
        }

        public static void UpdateCurrencyDisplay()
        {
            if (currencyTextComponent != null && Plugin.SkillConfigs.TryGetValue("HeightCurrency", out var currencyconfig))
            {
                currencyTextComponent.text = ((ConfigEntry<float>)currencyconfig).Value.ToString();
            }
            
            if (heightTextComponent != null && Plugin.SkillConfigs.TryGetValue("MaxHeight", out var heightconfig))
            {
                int hcfloat = (int)((ConfigEntry<float>)heightconfig).Value;
                heightTextComponent.text = hcfloat.ToString() + "m";
            }
        }

        public IEnumerator AnimateCurrencyGain(int oldVal, int addedAmount)
        {
            int finalVal = oldVal + addedAmount;

            currencyAddCanvasGroup.alpha = 0f;
            currencyAddObj.SetActive(true);
            mainText.text = oldVal.ToString();
            infoText.text = $"+{addedAmount}";

            float elapsed = 0f;
            float fadeDuration = 0.3f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                currencyAddCanvasGroup.alpha = Mathf.Clamp01(elapsed / fadeDuration);
                yield return null;
            }

            yield return new WaitForSeconds(0.5f);

            RectTransform infoRect = infoText.GetComponent<RectTransform>();
            Vector2 startPos = infoRect.anchoredPosition;
            Vector2 targetPos = mainText.GetComponent<RectTransform>().anchoredPosition;

            elapsed = 0f;
            float moveDuration = 0.4f;
            while (elapsed < moveDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0, 1, elapsed / moveDuration);
                infoRect.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);

                infoText.alpha = 1f - t;
                yield return null;
            }

            elapsed = 0f;
            float tickerDuration = 0.6f;
            while (elapsed < tickerDuration)
            {
                elapsed += Time.deltaTime;
                int current = (int)Mathf.Lerp(oldVal, finalVal, elapsed / tickerDuration);
                mainText.text = current.ToString();
                yield return null;
            }
            mainText.text = finalVal.ToString();

            elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                currencyAddCanvasGroup.alpha = 1f - Mathf.Clamp01(elapsed / fadeDuration);
                yield return null;
            }

            currencyAddObj.SetActive(false);
        }

        private void CreateClickOutsideBlocker()
        {
            if (uiInstance == null) return;

            GameObject blocker = new GameObject("STOutsideClickBlocker");
            blocker.transform.SetParent(uiInstance.transform, false);

            if (skillTreePanel != null)
            {
                blocker.transform.SetSiblingIndex(skillTreePanel.transform.GetSiblingIndex());
            }

            RectTransform rect = blocker.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;

            Image img = blocker.AddComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0f);

            Button btn = blocker.AddComponent<Button>();
            btn.onClick.AddListener(() => ToggleUIPanel(false));

            clickOutsideBlocker = blocker;
        }

        private void CleanUpFailedInit()
        {
            Instance = null;
            Destroy(gameObject);
        }

        private void MapInterfaceControls()
        {
            openButtonObj?.GetComponent<Button>()?.onClick.AddListener(() => ToggleUIPanel(true));
            closeButtonObj?.GetComponent<Button>()?.onClick.AddListener(() => ToggleUIPanel(false));

            forwardButtonObj?.GetComponent<Button>()?.onClick.AddListener(() => {
                if (descriptionPanelObj != null && descriptionPanelObj.activeSelf) return;
                SwitchSubMenuContext(false);
            });

            backButtonObj?.GetComponent<Button>()?.onClick.AddListener(() => {
                if (descriptionPanelObj != null && descriptionPanelObj.activeSelf)
                {
                    ReturnFromDescriptionPage();
                }
                else
                {
                    SwitchSubMenuContext(true);
                }
            });

            if (cameraRotationToggle != null)
            {
                cameraRotationToggle.isOn = Plugin.EnableParkourRotation.Value;
                cameraRotationToggle.onValueChanged.AddListener(isChecked => {
                    Plugin.EnableParkourRotation.Value = isChecked;
                });
            }

            if (cameraFOVToggle != null)
            {
                cameraFOVToggle.isOn = Plugin.EnableParkourFOV.Value;
                cameraFOVToggle.onValueChanged.AddListener(isChecked => {
                    Plugin.EnableParkourFOV.Value = isChecked;
                });
            }

            if (cameraShakeToggle != null)
            {
                cameraShakeToggle.isOn = Plugin.EnableParkourShake.Value;
                cameraShakeToggle.onValueChanged.AddListener(isChecked => {
                    Plugin.EnableParkourShake.Value = isChecked;
                });
            }

            if (uiGlowToggle != null)
            {
                uiGlowToggle.isOn = Plugin.EnableWallRunHelper.Value;
                uiGlowToggle.onValueChanged.AddListener(isChecked => {
                    Plugin.EnableWallRunHelper.Value = isChecked;
                });
            }

            resetProgressButton?.onClick.AddListener(ShowResetConfirmPopup);
            purchaseButton?.onClick.AddListener(OnPurchasePressed);
        }

        private void SwitchSubMenuContext(bool showPerksTree)
        {
            if (contentTransform != null) contentTransform.gameObject.SetActive(showPerksTree);
            if (optionsContentObj != null) optionsContentObj.SetActive(!showPerksTree);
            if (descriptionPanelObj != null) descriptionPanelObj.SetActive(false);

            if (headerGroup != null) headerGroup.gameObject.SetActive(true);
            if (forwardButtonObj != null) forwardButtonObj.SetActive(showPerksTree);
            if (backButtonObj != null) backButtonObj.SetActive(!showPerksTree);

            if (textHeaderComponent != null)
            {
                textHeaderComponent.gameObject.SetActive(true);
                textHeaderComponent.text = showPerksTree ? "SKILL TREE" : "OPTIONS";
            }

            if (!showPerksTree && cameraRotationToggle != null)
            {
                cameraRotationToggle.isOn = Plugin.EnableParkourRotation.Value;
            }

            if (!showPerksTree && cameraFOVToggle != null)
            {
                cameraFOVToggle.isOn = Plugin.EnableParkourFOV.Value;
            }

            if (!showPerksTree && cameraShakeToggle != null)
            {
                cameraShakeToggle.isOn = Plugin.EnableParkourShake.Value;
            }

            if (!showPerksTree && uiGlowToggle != null)
            {
                uiGlowToggle.isOn = Plugin.EnableWallRunHelper.Value;
            }

            if (contentTransform != null)
                StartCoroutine(FadePanel(contentTransform.gameObject, showPerksTree, 0.3f));

            if (optionsContentObj != null)
                StartCoroutine(FadePanel(optionsContentObj.gameObject, !showPerksTree, 0.3f));
        }

        private IEnumerator FadePanel(GameObject obj, bool fadeIn, float duration)
        {
            CanvasGroup cg = obj.GetComponent<CanvasGroup>();
            if (cg == null) cg = obj.AddComponent<CanvasGroup>();

            float startAlpha = cg.alpha;
            float endAlpha = fadeIn ? 1f : 0f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                cg.alpha = Mathf.Lerp(startAlpha, endAlpha, elapsed / duration);
                yield return null;
            }
            cg.alpha = endAlpha;
        }

        private void OnResetRequested()
        {
            foreach (var key in skillButtons.Keys)
            {
                if (Plugin.SkillConfigs.TryGetValue(key, out var config))
                {
                    ((ConfigEntry<bool>)config).Value = false;
                }
            }

            newCurrencyAmount = 0f;
            ((ConfigEntry<float>)Plugin.SkillConfigs["HeightCurrency"]).Value = 0f;
            ((ConfigEntry<float>)Plugin.SkillConfigs["MaxHeight"]).Value = 0f;
            UpdateCurrencyDisplay();

            foreach (var anim in activeAnimations)
            {
                if (anim != null) StopCoroutine(anim);
            }
            activeAnimations.Clear();
            isAnimatingBranch = false;

            RefreshAllNodeVisuals();
        }

        private void ShowResetConfirmPopup()
        {
            if (optionsContentObj != null) optionsContentObj.SetActive(false);
            if (headerGroup != null) headerGroup.gameObject.SetActive(false);
            if (resetConfirmBox != null) resetConfirmBox.SetActive(true);
        }

        private void HandleResetConfirmed()
        {
            OnResetRequested();

            if (resetConfirmBox != null) resetConfirmBox.SetActive(false);
            if (optionsContentObj != null) optionsContentObj.SetActive(true);
            if (headerGroup != null) headerGroup.gameObject.SetActive(true);
        }

        private void HandleResetDenied()
        {
            if (resetConfirmBox != null) resetConfirmBox.SetActive(false);
            if (optionsContentObj != null) optionsContentObj.SetActive(true);
            if (headerGroup != null) headerGroup.gameObject.SetActive(true);
        }

        private void MapProgressionNodes()
        {
            if (contentTransform == null) return;

            skillButtons.Clear();
            skillDependencies.Clear();
            skillLines.Clear();
            nodeOriginPositions.Clear();

            RegisterNode("QTButton", "QuickTurning", null, "LineToSlide");
            RegisterNode("SlideButton", "Sliding", new string[] { "QuickTurning" }, null);
            RegisterNode("SlideJButton", "SlideJumping", new string[] { "Sliding" }, "LineToSJ");
            RegisterNode("LeapButton", "Leaping", new string[] { "SlideJumping" }, "LineToLeaping");
            RegisterNode("RollButton", "Rolling", new string[] { "Sliding" }, "LineToRoll");
            RegisterNode("WKButton", "WallKicking", new string[] { "Sliding" }, "LineToWK");
            RegisterNode("WRButton", "WallRunning", new string[] { "WallKicking" }, "LineToWR");
            RegisterNode("VWRButton", "VerticalWallRunning", new string[] { "WallRunning" }, "LineToVWR");
            RegisterNode("WRBButton", "WallRunBoost", new string[] { "VerticalWallRunning" }, "LineToWRB");

            foreach (var kvp in skillButtons)
            {
                string configKey = kvp.Key;
                RectTransform rect = kvp.Value.GetComponent<RectTransform>();
                if (rect != null)
                {
                    nodeOriginPositions[configKey] = rect.anchoredPosition;
                    if (configKey == "QuickTurning")
                    {
                        quickTurnRect = rect;
                    }
                }

                if (skillLines.TryGetValue(configKey, out Image lineImg))
                {
                    RectTransform lineRect = lineImg.GetComponent<RectTransform>();
                    if (lineRect != null)
                    {
                        lineOriginPositions[configKey] = lineRect.anchoredPosition;

                        if (lineImg.gameObject.name == "LineToSlide")
                        {
                            lineToSlideRect = lineRect;
                        }
                    }
                }
            }
        }

        private void RegisterNode(string buttonObjectName, string configKey, string[] prerequisites, string lineObjectName)
        {
            Transform btnTrans = contentTransform.Find(buttonObjectName);
            if (btnTrans == null) return;

            Button btn = btnTrans.GetComponent<Button>();
            if (btn == null) return;

            skillButtons[configKey] = btn;

            if (prerequisites != null)
            {
                skillDependencies[configKey] = prerequisites;
            }

            if (!string.IsNullOrEmpty(lineObjectName))
            {
                Transform lineTrans = contentTransform.Find(lineObjectName);
                if (lineTrans != null)
                {
                    Image lineImg = lineTrans.GetComponent<Image>();
                    if (lineImg != null)
                    {
                        skillLines[configKey] = lineImg;
                    }
                }
            }

            btn.onClick.AddListener(() => OnNodeInteracted(configKey));
        }

        private bool AreDependenciesMet(string configKey)
        {
            if (!skillDependencies.TryGetValue(configKey, out var prerequisites))
            {
                return true;
            }

            foreach (string reqKey in prerequisites)
            {
                if (Plugin.SkillConfigs.TryGetValue(reqKey, out var config))
                {
                    if (!((ConfigEntry<bool>)config).Value) return false;
                }
                else
                {
                    return false;
                }
            }

            return true;
        }

        private void OnNodeInteracted(string configKey)
        {
            if (isAnimatingBranch) return;
            if (!AreDependenciesMet(configKey)) return;

            if (Plugin.SkillConfigs.TryGetValue(configKey, out var config))
            {
                currentlySelectedSkillKey = configKey;
                pendingPurchaseAnimation = false;

                if (contentTransform != null)
                {
                    CanvasGroup cg = contentTransform.GetComponent<CanvasGroup>();
                    if (cg == null) cg = contentTransform.gameObject.AddComponent<CanvasGroup>();

                    StartCoroutine(FadeCanvasGroup(cg, 1f, 0f, 0.2f));

                    StartCoroutine(DeactivateAfterDelay(contentTransform.gameObject, 0.2f));
                }

                if (textHeaderComponent != null) textHeaderComponent.gameObject.SetActive(false);

                if (headerGroup != null) headerGroup.gameObject.SetActive(true);
                if (forwardButtonObj != null) forwardButtonObj.SetActive(false);
                if (backButtonObj != null) backButtonObj.SetActive(true);

                if (descriptionPanelObj != null)
                {
                    RectTransform descRect = descriptionPanelObj.GetComponent<RectTransform>();
                    descRect.anchoredPosition = new Vector2(1000f, 0f);
                    descriptionPanelObj.SetActive(true);

                    StartCoroutine(AnimateSlide(descriptionPanelObj, descRect.anchoredPosition, Vector2.zero, 0.3f));
                }

                if (perkDescriptionText != null)
                {
                    if (perkDescriptions.TryGetValue(configKey, out string uniqueDesc))
                    {
                        perkDescriptionText.text = uniqueDesc;
                    }
                }

                if (purchaseButton != null)
                {
                    purchaseButton.gameObject.SetActive(true);

                    if (((ConfigEntry<bool>)config).Value)
                    {
                        purchaseButton.interactable = false;
                        if (purchaseButtonText != null) purchaseButtonText.text = "ACTIVE";
                    }
                    else
                    {
                        purchaseButton.interactable = true;
                        if (purchaseButtonText != null) purchaseButtonText.text = "PURCHASE";
                    }
                }
            }

            if (perkIconHolderObj != null)
            {
                perkIconHolderObj.SetActive(true);

                if (((ConfigEntry<bool>)config).Value)
                {
                    if (perkCurrencyText != null) perkCurrencyText.gameObject.SetActive(false);
                    if (perkCurrencyIcon != null) perkCurrencyIcon.SetActive(false);
                }
                else
                {
                    if (perkCurrencyText != null)
                    {
                        perkCurrencyText.gameObject.SetActive(true);
                        float price = skillPrices.TryGetValue(configKey, out float p) ? p : 0f;
                        perkCurrencyText.text = price > 0 ? price.ToString() : "FREE";
                    }
                    if (perkCurrencyIcon != null) perkCurrencyIcon.SetActive(true);
                }

                foreach (var icon in perkIcons.Values)
                {
                    if (icon != null) icon.SetActive(false);
                }

                string iconName;
                if (configToIconMap.TryGetValue(configKey, out string mappedName))
                {
                    iconName = mappedName;
                }
                else
                {

                    iconName = configKey + "Icon";
                }

                if (perkIcons.TryGetValue(iconName, out GameObject iconObj))
                {
                    iconObj.SetActive(true);
                }
            }
        }
        private IEnumerator DeactivateAfterDelay(GameObject obj, float delay)
        {
            yield return new WaitForSeconds(delay);
            obj.SetActive(false);
        }

        private IEnumerator AnimateSlide(GameObject targetObj, Vector2 startPos, Vector2 endPos, float duration)
        {
            RectTransform rect = targetObj.GetComponent<RectTransform>();
            if (rect == null) yield break;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                rect.anchoredPosition = Vector2.Lerp(startPos, endPos, Mathf.SmoothStep(0f, 1f, t));
                yield return null;
            }
            rect.anchoredPosition = endPos;
        }

        private void OnPurchasePressed()
        {
            if (string.IsNullOrEmpty(currentlySelectedSkillKey)) return;
            if (isAnimatingBranch) return;

            if (!skillPrices.TryGetValue(currentlySelectedSkillKey, out float cost)) cost = 0;

            if (((ConfigEntry<float>)ParkourKnuckle.Plugin.SkillConfigs["HeightCurrency"]).Value < cost)
                {
                    return;
                }

            if (Plugin.SkillConfigs.TryGetValue(currentlySelectedSkillKey, out var config))
            {
                if (((ConfigEntry<bool>)config).Value) return;

                ((ConfigEntry<bool>)config).Value = true;
                Plugin.ProgressionFile.Save();
                pendingPurchaseAnimation = true;
                newCurrencyAmount = ((ConfigEntry<float>)ParkourKnuckle.Plugin.SkillConfigs["HeightCurrency"]).Value - cost;
                hasPurchased = true;

                if (purchaseButton != null)
                {
                    purchaseButton.interactable = false;
                    if (purchaseButtonText != null) purchaseButtonText.text = "ACTIVE";
                }

                if (perkCurrencyText != null) perkCurrencyText.gameObject.SetActive(false);
                if (perkCurrencyIcon != null) perkCurrencyIcon.SetActive(false);
            }
        }

        private void ReturnFromDescriptionPage()
        {
            string nodeToAnimate = currentlySelectedSkillKey;
            bool triggerEffects = pendingPurchaseAnimation;

            if (forwardButtonObj != null) forwardButtonObj.SetActive(true);
            if (backButtonObj != null) backButtonObj.SetActive(false);

            if (textHeaderComponent != null) textHeaderComponent.gameObject.SetActive(true);

            if (descriptionPanelObj != null)
            {
                RectTransform descRect = descriptionPanelObj.GetComponent<RectTransform>();
                StartCoroutine(AnimateSlide(descriptionPanelObj, Vector2.zero, new Vector2(1000f, 0f), 0.3f));
            }

            if (contentTransform != null) contentTransform.gameObject.SetActive(false);

            StartCoroutine(DelayedReturnLogic(nodeToAnimate, triggerEffects));
        }

        private IEnumerator DelayedReturnLogic(string nodeToAnimate, bool triggerEffects)
        {
            yield return new WaitForSeconds(0.3f);

            if (descriptionPanelObj != null) descriptionPanelObj.SetActive(false);

            if (contentTransform != null)
            {
                CanvasGroup cg = contentTransform.GetComponent<CanvasGroup>();
                if (cg == null) cg = contentTransform.gameObject.AddComponent<CanvasGroup>();

                contentTransform.gameObject.SetActive(true);
                cg.alpha = 0f;

                StartCoroutine(FadeCanvasGroup(cg, 0f, 1f, 0.3f));
            }

            RefreshAllNodeVisuals();

            if (triggerEffects && !string.IsNullOrEmpty(nodeToAnimate))
            {
                if (skillButtons.TryGetValue(nodeToAnimate, out Button btn))
                {
                    Image btnImage = btn?.GetComponent<Image>();
                    if (btnImage != null) btnImage.color = new Color(0.2f, 0.75f, 0.2f, 1f);

                    TriggerNodeVisualFeedback(nodeToAnimate, btn.gameObject, true);
                    activeAnimations.Add(StartCoroutine(AnimateOutgoingLinesSequence(nodeToAnimate)));
                }
            }
        }

        private IEnumerator FadeCanvasGroup(CanvasGroup cg, float startAlpha, float endAlpha, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                cg.alpha = Mathf.Lerp(startAlpha, endAlpha, elapsed / duration);
                yield return null;
            }
            cg.alpha = endAlpha;
        }

        private IEnumerator AnimateOutgoingLinesSequence(string unlockedParentKey)
        {
            isAnimatingBranch = true;

            List<Image> linesToAnimate = new List<Image>();
            List<string> childKeysToPop = new List<string>();

            foreach (var kvp in skillDependencies)
            {
                string childKey = kvp.Key;
                string[] parents = kvp.Value;

                foreach (string p in parents)
                {
                    if (p == unlockedParentKey)
                    {
                        childKeysToPop.Add(childKey);
                        if (skillLines.TryGetValue(childKey, out var lineImg))
                        {
                            linesToAnimate.Add(lineImg);
                        }
                    }
                }
            }

            float elapsed = 0f;
            float duration = 0.25f;
            bool hasUnlockedNextTier = false;

            foreach (var line in linesToAnimate)
            {
                line.color = Color.white;
            }

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float currentFill = Mathf.Clamp01(elapsed / duration);

                foreach (var line in linesToAnimate)
                {
                    line.fillAmount = currentFill;
                }

                if (!hasUnlockedNextTier && currentFill >= percentLineFillRequiredToUnlock)
                {
                    hasUnlockedNextTier = true;
                    isAnimatingBranch = false;
                    RefreshAllNodeVisuals();

                    foreach (string childKey in childKeysToPop)
                    {
                        if (skillButtons.TryGetValue(childKey, out var childBtn))
                        {
                            TriggerNodeVisualFeedback(childKey, childBtn.gameObject, false);
                        }
                    }

                    isAnimatingBranch = true;
                }

                yield return null;
            }

            foreach (var line in linesToAnimate)
            {
                line.fillAmount = 1f;
            }

            isAnimatingBranch = false;
            RefreshAllNodeVisuals();
        }

        private void TriggerNodeVisualFeedback(string configKey, GameObject buttonObj, bool doShakeEffect)
        {
            if (activeButtonCoroutines.TryGetValue(configKey, out var activeRoutine))
            {
                if (activeRoutine != null) StopCoroutine(activeRoutine);
            }

            activeButtonCoroutines[configKey] = StartCoroutine(AnimateNodeJuice(configKey, buttonObj, doShakeEffect));
        }

        private IEnumerator AnimateNodeJuice(string configKey, GameObject targetNode, bool isShakePop)
        {
            RectTransform rect = targetNode.GetComponent<RectTransform>();
            if (rect == null) yield break;

            float elapsed = 0f;
            Vector2 originPosition = rect.anchoredPosition;

            while (elapsed < nodeAnimateDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / nodeAnimateDuration);

                float scaleValue = 1f + Mathf.Sin(t * Mathf.PI) * 0.25f;
                rect.localScale = new Vector3(scaleValue, scaleValue, 1f);

                if (isShakePop)
                {
                    float shakeStrength = Mathf.Sin(t * Mathf.PI * 6f) * (20f * (1f - t));
                    rect.anchoredPosition = originPosition + new Vector2(shakeStrength, 0f);
                }

                yield return null;
            }

            rect.localScale = Vector3.one;
            rect.anchoredPosition = originPosition;
            activeButtonCoroutines[configKey] = null;
        }

        public void ToggleUIPanel(bool showMainPanel)
        {
            if (showMainPanel && GameObject.Find("CL_Player") != null) return;

            Plugin.isUIVisible = showMainPanel;

            if (activePanelPopCoroutine != null) StopCoroutine(activePanelPopCoroutine);
            if (activeButtonPopCoroutine != null) StopCoroutine(activeButtonPopCoroutine);

            if (showMainPanel)
            {
                SwitchSubMenuContext(true);

                if (contentTransform != null)
                {
                    contentTransform.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
                }

                foreach (var anim in activeAnimations)
                {
                    if (anim != null) StopCoroutine(anim);
                }
                activeAnimations.Clear();

                foreach (var kvp in activeButtonCoroutines)
                {
                    if (kvp.Value != null) StopCoroutine(kvp.Value);
                }
                activeButtonCoroutines.Clear();
                isAnimatingBranch = false;

                RefreshAllNodeVisuals();

                activePanelPopCoroutine = StartCoroutine(AnimatePopScale(skillTreePanel, true, popInDuration));
                activeButtonPopCoroutine = StartCoroutine(AnimatePopScale(openButtonObj, false, popOutDuration));

                clickOutsideBlocker?.SetActive(true);
            }
            else
            {
                currentlySelectedSkillKey = string.Empty;
                pendingPurchaseAnimation = false;

                activePanelPopCoroutine = StartCoroutine(AnimatePopScale(skillTreePanel, false, popOutDuration));
                activeButtonPopCoroutine = StartCoroutine(AnimatePopScale(openButtonObj, true, popInDuration));

                clickOutsideBlocker?.SetActive(false);
            }
        }

        private IEnumerator AnimatePopScale(GameObject targetObj, bool visualShow, float duration)
        {
            if (targetObj == null) yield break;

            if (visualShow) targetObj.SetActive(true);

            float elapsed = 0f;
            Vector3 startScale = targetObj.transform.localScale;
            Vector3 targetScale = visualShow ? Vector3.one : Vector3.zero;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                if (visualShow)
                {
                    float bounceCurve = 1f - Mathf.Pow(1f - t, 2f) * (1f - (t * 2.5f));
                    targetObj.transform.localScale = Vector3.LerpUnclamped(Vector3.zero, Vector3.one, bounceCurve);
                }
                else
                {
                    float shrinkCurve = t * t;
                    targetObj.transform.localScale = Vector3.Lerp(startScale, targetScale, shrinkCurve);
                }

                yield return null;
            }

            targetObj.transform.localScale = targetScale;

            if (!visualShow) targetObj.SetActive(false);
        }

        public void RefreshAllNodeVisuals()
        {
            if (isAnimatingBranch) return;

            foreach (var kvp in skillButtons)
            {
                string configKey = kvp.Key;
                Button btn = kvp.Value;
                Image btnImage = btn.GetComponent<Image>();

                if (btnImage == null) continue;

                bool allowedToUnlock = AreDependenciesMet(configKey);

                if (Plugin.SkillConfigs.TryGetValue(configKey, out var config))
                {
                    if (((ConfigEntry<bool>)config).Value)
                    {
                        btnImage.color = new Color(0.2f, 0.75f, 0.2f, 1f);
                        btn.interactable = true;

                        if (skillLines.TryGetValue(configKey, out var lineImg))
                        {
                            lineImg.color = Color.white;
                            lineImg.fillAmount = 1f;
                        }
                    }
                    else if (allowedToUnlock)
                    {
                        btnImage.color = Color.white;
                        btn.interactable = true;

                        if (skillLines.TryGetValue(configKey, out var lineImg))
                        {
                            lineImg.color = Color.white;
                            lineImg.fillAmount = 1f;
                        }
                    }
                    else
                    {
                        btnImage.color = new Color(0.25f, 0.25f, 0.25f, 0.5f);
                        btn.interactable = false;

                        if (skillLines.TryGetValue(configKey, out var lineImg))
                        {
                            lineImg.fillAmount = 0f;
                        }
                    }
                }
            }

            if (Plugin.SkillConfigs.TryGetValue("QuickTurning", out var qtConfig) && ((ConfigEntry<bool>)qtConfig).Value)
            {
                if (skillLines.TryGetValue("QuickTurning", out var slideLine))
                {
                    slideLine.color = Color.white;
                    slideLine.fillAmount = 1f;
                }
            }
            else
            {
                if (skillLines.TryGetValue("QuickTurning", out var slideLine))
                {
                    slideLine.fillAmount = 0f;
                }
            }
        }
    }
}