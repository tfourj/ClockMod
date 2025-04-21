using MelonLoader;
using UnityEngine;

namespace ClockMod
{
    public static class Menu
    {
        private static int selectedPosition = 1;
        private static float sliderValue = 0f;
        private static bool isClockEnabled = true;
        private static bool settingsChanged = false;
        private static ClockMod clockMod;
        private static bool isMenuOpen = false;
        private static Rect windowRect = new Rect(100, 100, 320, 400); // Increased height for new options
        private static bool isResizing = false;
        private static Vector2 resizeStartPos;
        private static Rect originalRect;
        private static readonly string[] positionNames = new string[] { "Top-Left", "Top-Right", "Bottom-Left", "Bottom-Right", "Custom" };
        private static int selectedStyle = 0;
        private static float customX = 0.5f;
        private static float customY = 0.5f;
        private static readonly string[] styleNames = new string[] { "Classic", "Modern" };
        private static readonly Color backgroundColor = new Color(0.1f, 0.1f, 0.1f, 1f);
        private static readonly Color elementBackgroundColor = new Color(0.3f, 0.3f, 0.3f);

        public static void Initialize(ClockMod mod)
        {
            clockMod = mod;
            selectedPosition = mod.CurrentPositionIndex;
            sliderValue = mod.ClockSize;
            isClockEnabled = mod.IsClockEnabled;
            selectedStyle = mod.ClockStyle;
            customX = mod.CustomXPosition;
            customY = mod.CustomYPosition;

            MelonLoader.MelonEvents.OnGUI.Subscribe(OnGUI, 200);
            MelonLogger.Msg("Clock Mod initialized");
        }

        public static bool HandleKeyInput()
        {
            // Use configured hotkeys from ClockMod
            if (clockMod != null)
            {
                KeyCode modifierKey;
                KeyCode mainKey;
                try
                {
                    modifierKey = (KeyCode)Enum.Parse(typeof(KeyCode), clockMod.HotkeyModifier.Replace(" ", ""));
                    mainKey = (KeyCode)Enum.Parse(typeof(KeyCode), clockMod.HotkeyKey.Replace(" ", ""));
                }
                catch
                {
                    MelonLogger.Msg($"Hotkey parsing failed, using default hotkey LeftShift + F12.");
                    modifierKey = KeyCode.LeftShift;
                    mainKey = KeyCode.F12;
                }

                if (Input.GetKeyDown(mainKey) && Input.GetKey(modifierKey))
                {
                    isMenuOpen = !isMenuOpen;
                    return true;
                }
            }

            if (isMenuOpen && Input.GetKeyDown(KeyCode.Escape))
            {
                isMenuOpen = false;
                return true;
            }

            return false;
        }

        private static void OnGUI()
        {
            if (!isMenuOpen) return;

            GUI.backgroundColor = backgroundColor;
            GUI.contentColor = Color.white;

            // Create a custom window style
            GUIStyle windowStyle = new GUIStyle(GUI.skin.window);
            windowStyle.normal.background = MakeSolidTexture(2, 2, backgroundColor);
            windowStyle.hover.background = windowStyle.normal.background;
            windowStyle.active.background = windowStyle.normal.background;
            windowStyle.focused.background = windowStyle.normal.background;
            windowStyle.onNormal.background = windowStyle.normal.background;
            windowStyle.onHover.background = windowStyle.normal.background;
            windowStyle.onActive.background = windowStyle.normal.background;
            windowStyle.onFocused.background = windowStyle.normal.background;
            windowStyle.border = new RectOffset(2, 2, 2, 2);
            windowStyle.padding = new RectOffset(10, 10, 10, 10);
            windowStyle.normal.textColor = Color.white;
            windowStyle.hover.textColor = Color.white;
            windowStyle.active.textColor = Color.white;
            windowStyle.focused.textColor = Color.white;
            windowStyle.onNormal.textColor = Color.white;
            windowStyle.onHover.textColor = Color.white;
            windowStyle.onActive.textColor = Color.white;
            windowStyle.onFocused.textColor = Color.white;
            windowStyle.alignment = TextAnchor.MiddleCenter; // Ensure title fits

            // Draw the window with a custom title
            windowRect = GUI.Window(12345, windowRect, (GUI.WindowFunction)DrawWindow, "", windowStyle);
            // Draw resize handle
            Rect resizeHandleRect = new Rect(windowRect.width + windowRect.x - 20, windowRect.height + windowRect.y - 20, 20, 20);
            GUI.Box(resizeHandleRect, "↘", new GUIStyle() { alignment = TextAnchor.MiddleCenter });

            HandleResizing(resizeHandleRect);

            if (settingsChanged)
            {
                ApplySettings();
                settingsChanged = false;
            }
        }

        // Handle resizing of the window
        private static void HandleResizing(Rect resizeHandleRect)
        {
            Event currentEvent = Event.current;
            if (currentEvent.type == EventType.MouseDown && currentEvent.button == 0 && resizeHandleRect.Contains(currentEvent.mousePosition))
            {
                isResizing = true;
                resizeStartPos = currentEvent.mousePosition;
                originalRect = windowRect;
                currentEvent.Use();
            }
            if (isResizing && currentEvent.type == EventType.MouseDrag)
            {
                float width = Mathf.Max(320, originalRect.width + (currentEvent.mousePosition.x - resizeStartPos.x));
                float height = Mathf.Max(400, originalRect.height + (currentEvent.mousePosition.y - resizeStartPos.y));

                windowRect.width = width;
                windowRect.height = height;

                currentEvent.Use();
            }
            if (isResizing && currentEvent.type == EventType.MouseUp)
            {
                isResizing = false;
                currentEvent.Use();
            }
        }

        private static Texture2D MakeSolidTexture(int width, int height, Color color)
        {
            Color[] pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }
            Texture2D texture = new Texture2D(width, height);
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        private static void DrawWindow(int id)
        {
            GUILayout.Space(10);

            // Style selection
            GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
            labelStyle.normal.textColor = Color.white;
            labelStyle.fontSize = 14;

            GUIStyle middleLabelStyle = new GUIStyle(labelStyle);
            middleLabelStyle.alignment = TextAnchor.MiddleCenter;
            middleLabelStyle.fontSize = 16;
            middleLabelStyle.normal.textColor = Color.white;
            GUILayout.Label("Clock Mod Settings", middleLabelStyle);

            GUILayout.Label("Clock Style:", labelStyle);
            int newStyle = GUILayout.SelectionGrid(selectedStyle, styleNames, 2);
            if (newStyle != selectedStyle)
            {
                selectedStyle = newStyle;
                settingsChanged = true;
            }

            GUILayout.Space(15);

            // Position selection
            GUILayout.Label("Position:", labelStyle);
            int newPosition = GUILayout.SelectionGrid(selectedPosition, positionNames, 2);
            if (newPosition != selectedPosition)
            {
                selectedPosition = newPosition;
                settingsChanged = true;

                if (selectedPosition == 4)
                {
                    windowRect.height = 500;
                }
                else
                {
                    windowRect.height = 400;
                }
            }

            // Custom position sliders
            if (selectedPosition == 4) // Custom position
            {
                GUILayout.Space(10);
                GUILayout.Label("Custom Position:", labelStyle);
                GUILayout.BeginHorizontal();
                GUILayout.Label("X:", GUILayout.Width(20));
                float newX = GUILayout.HorizontalSlider(customX, 0f, 1f);
                GUILayout.Label(customX.ToString("F2"), GUILayout.Width(40));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("Y:", GUILayout.Width(20));
                float newY = GUILayout.HorizontalSlider(customY, 0f, 1f);
                GUILayout.Label(customY.ToString("F2"), GUILayout.Width(40));
                GUILayout.EndHorizontal();

                if (Math.Abs(newX - customX) > 0.01f || Math.Abs(newY - customY) > 0.01f)
                {
                    customX = newX;
                    customY = newY;
                    settingsChanged = true;
                }
            }

            GUILayout.Space(15);

            // Size slider
            GUILayout.Label("Size (-10 to +10): " + sliderValue.ToString("F1"), labelStyle);
            GUI.backgroundColor = elementBackgroundColor;
            GUILayout.BeginVertical(GUI.skin.box);

            GUIStyle sliderStyle = new GUIStyle(GUI.skin.horizontalSlider);
            sliderStyle.normal.background = MakeSolidTexture(2, 2, new Color(0.4f, 0.4f, 0.4f));

            GUIStyle thumbStyle = new GUIStyle(GUI.skin.horizontalSliderThumb);
            thumbStyle.normal.background = MakeSolidTexture(10, 20, new Color(0.6f, 0.6f, 0.6f));
            thumbStyle.fixedHeight = 20;
            thumbStyle.fixedWidth = 15;
            thumbStyle.margin = new RectOffset(0, 0, -8, 0); // Center slider thumb

            float newSize = GUILayout.HorizontalSlider(sliderValue, -10f, 10f, sliderStyle, thumbStyle);
            GUILayout.EndVertical();
            GUI.backgroundColor = backgroundColor;

            if (Math.Abs(newSize - sliderValue) > 0.1f)
            {
                sliderValue = newSize;
                settingsChanged = true;
            }

            GUILayout.Space(20);

            // Enable/Disable toggle
            GUI.backgroundColor = elementBackgroundColor;
            GUILayout.BeginVertical(GUI.skin.box);

            GUIStyle toggleStyle = new GUIStyle(GUI.skin.toggle);
            toggleStyle.fontSize = 16;
            toggleStyle.normal.textColor = Color.white;
            toggleStyle.onNormal.textColor = Color.white;

            bool newEnabled = GUILayout.Toggle(isClockEnabled, "Display Clock", toggleStyle);

            GUILayout.EndVertical();
            GUI.backgroundColor = backgroundColor;

            if (newEnabled != isClockEnabled)
            {
                isClockEnabled = newEnabled;
                settingsChanged = true;
            }

            GUILayout.Space(20);

            // Buttons
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            GUI.backgroundColor = elementBackgroundColor;
            if (GUILayout.Button("Save", GUILayout.Width(90), GUILayout.Height(30)))
            {
                ApplySettings();
                SaveSettings();
                isMenuOpen = false;
            }

            GUILayout.Space(10);

            if (GUILayout.Button("Close", GUILayout.Width(90), GUILayout.Height(30)))
            {
                isMenuOpen = false;
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUI.DragWindow(new Rect(0, 0, windowRect.width, 20));
        }

        private static void ApplySettings()
        {
            if (clockMod != null)
            {
                clockMod.SetClockPosition(selectedPosition);
                clockMod.SetClockSize(sliderValue);
                clockMod.SetClockEnabled(isClockEnabled);
                clockMod.SetClockStyle(selectedStyle);
                if (selectedPosition == 4) // Custom position
                {
                    clockMod.SetCustomPosition(customX, customY);
                }
            }
        }

        private static void SaveSettings()
        {
            if (clockMod != null)
            {
                clockMod.SaveSettings();
                MelonLogger.Msg("Clock settings saved");
            }
        }
    }
}