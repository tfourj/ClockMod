using MelonLoader;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[assembly: MelonInfo(typeof(ClockMod.ClockMod), "ClockMod", "1.1.0", "TfourJ")]
[assembly: MelonGame("TVGS", "Schedule I")]

namespace ClockMod
{
    public class ClockMod : MelonMod
    {
        private Text timeText;
        private const int MaxAttempts = 5;
        private const float AttemptInterval = 1f;
        private string filepath = "UserData/ClockMod.cfg";
        private bool guiSubscribed = false;
        private int currentPositionIndex;
        private const KeyCode ToggleKey = KeyCode.F12;
        private MelonPreferences_Category myCategory;
        private MelonPreferences_Entry<int> clockPosition;
        private float keyCooldownTime = 0.5f;
        private float lastKeyPressTime = 0f;
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

            if (System.IO.File.Exists(filepath))
            {
                myCategory.LoadFromFile();
            }
            else
            {
                LoggerInstance.Msg("Config file does not exist. Creating default configuration.");
                myCategory.SaveToFile();
            }

            LoadTimePosition();
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
                GameObject timeObject = GameObject.Find("Player (0)/Player_Local/CameraContainer/Camera/OverlayCamera/GameplayMenu/Phone/phone/HomeScreen/InfoBar/Time");
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
            if (timeText != null)
            {
                HandleKeyInput();

                GUIStyle boxStyle = GUI.skin.box;
                Vector2 textSize = boxStyle.CalcSize(new GUIContent(timeText.text));
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
                GUI.Box(boxRect, timeText.text, boxStyle);
            }
        }

        private void HandleKeyInput()
        {
            if (Time.time - lastKeyPressTime >= keyCooldownTime)
            {
                if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(ToggleKey))
                {
                    CycleTimePosition();
                    lastKeyPressTime = Time.time;
                }
            }
        }

        private void CycleTimePosition()
        {
            currentPositionIndex = (currentPositionIndex + 1) % positionOptions.Length;
            LoggerInstance.Msg($"Time position changed to: {positionOptions[currentPositionIndex]}");
            SaveTimePosition();
        }

        private void SaveTimePosition()
        {
            clockPosition.Value = currentPositionIndex;
            myCategory.SaveToFile();
            LoggerInstance.Msg($"Saved position: {currentPositionIndex}");
        }

        private void LoadTimePosition()
        {
            currentPositionIndex = clockPosition.Value;
            LoggerInstance.Msg($"Loaded position: {currentPositionIndex}");
        }
    }
}
