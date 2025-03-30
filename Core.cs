using MelonLoader;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[assembly: MelonInfo(typeof(ClockMod.Core), "ClockMod", "1.0.0", "TfourJ")]
[assembly: MelonGame("TVGS", "Schedule I")]

namespace ClockMod
{
    public class Core : MelonMod
    {
        private Text timeText;
        private const int MaxAttempts = 5;
        private const float AttemptInterval = 1f;
        private bool guiSubscribed = false;
        private int frameCounter = 0;

        public override void OnInitializeMelon()
        {
            LoggerInstance.Msg("ClockMod Initialized.");
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
                GUIStyle boxStyle = GUI.skin.box;
                Vector2 textSize = boxStyle.CalcSize(new GUIContent(timeText.text));
                float padding = 10f;
                Rect boxRect = new Rect(Screen.width - textSize.x - padding, Screen.height - textSize.y - padding, textSize.x, textSize.y);
                GUI.Box(boxRect, timeText.text, boxStyle);
            }
        }
    }
}
