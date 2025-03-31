using MelonLoader;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[assembly: MelonInfo(typeof(ClockMod.ClockMod), "ClockMod", "1.1.2", "TfourJ")]
[assembly: MelonGame("TVGS", "Schedule I")]

namespace ClockMod
{
    public class ClockMod : MelonMod
    {
        private Text timeText;
        private const int MaxAttempts = 10;
        private const float AttemptInterval = 1f;
        private string filepath = "UserData/ClockMod.cfg";
        private bool guiSubscribed = false;
        private int currentPositionIndex;
        private MelonPreferences_Category myCategory;
        private MelonPreferences_Entry<int> clockPosition;
        private MelonPreferences_Entry<float> clockSizePreference;
        private MelonPreferences_Entry<bool> clockEnabledPreference;
        private float keyCooldownTime = 0.5f;
        private float lastKeyPressTime = 0f;
        private float clockSize = 0f; // -10 to +10, 0 is default
        private bool isClockEnabled = true;
        
        public int CurrentPositionIndex => currentPositionIndex;
        public float ClockSize => clockSize;
        public bool IsClockEnabled => isClockEnabled;

        private readonly Vector2[] positionOptions =
        {
            new Vector2(0f, 0f), // Top-left
            new Vector2(1f, 0f), // Top-right
            new Vector2(0f, 1f), // Bottom-left
            new Vector2(1f, 1f)  // Bottom-right
        };

        public override void OnInitializeMelon()
        {
            LoggerInstance.Msg("ClockMod Initialized.");

            myCategory = MelonPreferences.CreateCategory("ClockMod_Settings");
            myCategory.SetFilePath(filepath);

            clockPosition = myCategory.CreateEntry<int>("Clock_Position", 1);
            clockSizePreference = myCategory.CreateEntry<float>("Clock_Size", 0f);
            clockEnabledPreference = myCategory.CreateEntry<bool>("Clock_Enabled", true);

            if (System.IO.File.Exists(filepath))
            {
                myCategory.LoadFromFile();
            }
            else
            {
                LoggerInstance.Msg("Config file does not exist. Creating default configuration.");
                myCategory.SaveToFile();
            }

            LoadSettings();
            
            Menu.Initialize(this);
            
            myCategory.SaveToFile();
        }

        public override void OnSceneWasInitialized(int buildIndex, string sceneName)
        {
            if (sceneName == "Main")
            {
                LoggerInstance.Msg($"Scene {sceneName} loaded. Starting to find clock.");
                MelonCoroutines.Start(StartClockFind());
            }
        }

        public override void OnSceneWasUnloaded(int buildIndex, string sceneName)
        {
            if (sceneName == "Main")
            {
                LoggerInstance.Msg($"Main scene was unloaded. Stopping clock display.");
                if (guiSubscribed)
                {
                    MelonEvents.OnGUI.Unsubscribe(DrawClock);
                    guiSubscribed = false;
                }
                timeText = null;
            }
        }

        private IEnumerator StartClockFind()
        {
            yield return new WaitForSeconds(2f);
            MelonCoroutines.Start(FindClockComponent());
        }

        private IEnumerator FindClockComponent()
        {
            int attempts = 0;
            while (attempts < MaxAttempts && timeText == null)
            {
                attempts++;

                // Find the Player_Local GameObject directly
                GameObject playerLocalObject = GameObject.Find("Player_Local");
                if (playerLocalObject == null)
                {
                    LoggerInstance.Warning($"Attempt {attempts}: Player_Local object not found. Retrying in {AttemptInterval} seconds...");
                    yield return new WaitForSeconds(AttemptInterval);
                    continue;
                }

                Transform parentTransform = playerLocalObject.transform.parent;
                if (parentTransform == null)
                {
                    LoggerInstance.Warning($"Attempt {attempts}: Parent of Player_Local not found. Retrying in {AttemptInterval} seconds...");
                    yield return new WaitForSeconds(AttemptInterval);
                    continue;
                }

                LoggerInstance.Msg($"Parent of Player_Local found: {parentTransform.name}");

                // Traverse the hierarchy from Player_Local to find the Time GameObject
                Transform infoBarTransform = playerLocalObject.transform.Find("CameraContainer/Camera/OverlayCamera/GameplayMenu/Phone/phone/HomeScreen/InfoBar");
                if (infoBarTransform != null)
                {
                    GameObject timeObject = infoBarTransform.Find("Time")?.gameObject;
                    if (timeObject != null)
                    {
                        timeText = timeObject.GetComponent<Text>();
                        if (timeText != null)
                        {
                            LoggerInstance.Msg("Clock component found.");
                            if (!guiSubscribed)
                            {
                                MelonEvents.OnGUI.Subscribe(DrawClock, 100);
                                guiSubscribed = true;
                            }
                            yield break;
                        }
                    }
                }

                LoggerInstance.Warning($"Attempt {attempts}: Clock not found. Retrying in {AttemptInterval} seconds...");
                yield return new WaitForSeconds(AttemptInterval);
            }

            if (timeText == null)
            {
                LoggerInstance.Error("Max attempts reached. Failed to find the Clock component.");
            }
        }

        private void DrawClock()
        {
            HandleKeyInput();
            if (timeText == null || !isClockEnabled) return;

            GUIStyle boxStyle = new GUIStyle(GUI.skin.box);
            boxStyle.padding = new RectOffset(8, 8, 4, 4);
            boxStyle.alignment = TextAnchor.MiddleCenter;
            
            Vector2 textSize = boxStyle.CalcSize(new GUIContent(timeText.text));
            float sizeMultiplier = 1f + (clockSize / 10f); // Convert -10 to +10 range to 0.0 to 2.0 multiplier
            GUIStyle clockStyle = new GUIStyle(boxStyle);
            clockStyle.fontSize = Mathf.RoundToInt(18 * sizeMultiplier); // Fixed base size of 18
            textSize = clockStyle.CalcSize(new GUIContent(timeText.text));
            textSize.x += 16 * sizeMultiplier;
            textSize.y += 8 * sizeMultiplier;
            
            float padding = 10f;
            float xPosition = 0f;
            float yPosition = 0f;

            switch (currentPositionIndex)
            {
                case 0: // Top-left
                    xPosition = padding;
                    yPosition = padding;
                    break;
                case 1: // Top-right
                    xPosition = Screen.width - textSize.x - padding;
                    yPosition = padding;
                    break;
                case 2: // Bottom-left
                    xPosition = padding;
                    yPosition = Screen.height - textSize.y - padding;
                    break;
                case 3: // Bottom-right
                    xPosition = Screen.width - textSize.x - padding;
                    yPosition = Screen.height - textSize.y - padding;
                    break;
            }

            Rect boxRect = new Rect(xPosition, yPosition, textSize.x, textSize.y);
            GUI.Box(boxRect, timeText.text, clockStyle);
        }

        private void HandleKeyInput()
        {
            if (Time.time - lastKeyPressTime >= keyCooldownTime)
            {
                if (Menu.HandleKeyInput())
                {
                    lastKeyPressTime = Time.time;
                    return;
                }
            }
        }

        public void SetClockPosition(int positionIndex)
        {
            if (positionIndex >= 0 && positionIndex < positionOptions.Length)
            {
                currentPositionIndex = positionIndex;
                LoggerInstance.Msg($"Clock position set to {positionOptions[currentPositionIndex]}");
            }
        }
        
        public void SetClockSize(float size)
        {
            clockSize = Mathf.Clamp(size, -10f, 10f);
            LoggerInstance.Msg($"Clock size set to {clockSize}");
        }
        
        public void SetClockEnabled(bool enabled)
        {
            isClockEnabled = enabled;
            LoggerInstance.Msg($"Clock display {(enabled ? "enabled" : "disabled")}");
        }

        public void SaveSettings()
        {
            clockPosition.Value = currentPositionIndex;
            clockSizePreference.Value = clockSize;
            clockEnabledPreference.Value = isClockEnabled;
            myCategory.SaveToFile();
            LoggerInstance.Msg("Settings saved to file");
        }

        private void LoadSettings()
        {
            currentPositionIndex = clockPosition.Value;
            clockSize = clockSizePreference.Value;
            isClockEnabled = clockEnabledPreference.Value;
            LoggerInstance.Msg($"Loaded settings - Position: {currentPositionIndex}, Size: {clockSize}, Enabled: {isClockEnabled}");
        }
    }
}
