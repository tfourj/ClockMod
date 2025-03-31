using MelonLoader;
using UnityEngine;
using UnityEngine.UI;
using System;

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
        private static Rect windowRect = new Rect(100, 100, 320, 280); // Increased size to avoid cropping
        private static bool isResizing = false;
        private static Vector2 resizeStartPos;
        private static Rect originalRect;
        private static readonly string[] positionNames = new string[] { "Top-Left", "Top-Right", "Bottom-Left", "Bottom-Right" };
        
        public static void Initialize(ClockMod mod)
        {
            clockMod = mod;
            selectedPosition = mod.CurrentPositionIndex;
            sliderValue = mod.ClockSize;
            isClockEnabled = mod.IsClockEnabled;
            
            MelonLoader.MelonEvents.OnGUI.Subscribe(OnGUI, 200);
            MelonLogger.Msg("Clock menu system initialized");
        }
        
        public static bool HandleKeyInput()
        {
            if (Input.GetKeyDown(KeyCode.F12) && Input.GetKey(KeyCode.LeftShift))
            {
                isMenuOpen = !isMenuOpen;
                return true;
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
            
            GUI.backgroundColor = Color.black;
            GUI.contentColor = Color.white;
            
            GUIStyle windowStyle = new GUIStyle(GUI.skin.window);
            windowStyle.normal.background = MakeSolidTexture(2, 2, Color.black);
            
            windowRect = GUI.Window(12345, windowRect, (GUI.WindowFunction)DrawWindow, "Clock Settings", windowStyle);
            
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
                float height = Mathf.Max(280, originalRect.height + (currentEvent.mousePosition.y - resizeStartPos.y));
                
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
        
        private static void DrawWindow(int windowID)
        {
            GUILayout.Space(10);
            GUILayout.Label("Position:");
            int newPosition = GUILayout.SelectionGrid(selectedPosition, positionNames, 2);
            if (newPosition != selectedPosition)
            {
                selectedPosition = newPosition;
                settingsChanged = true;
            }
            
            GUILayout.Space(15);
            GUILayout.Label("Size (-10 to +10): " + sliderValue.ToString("F1"));
            GUI.backgroundColor = new Color(0.5f, 0.5f, 0.5f); // Medium gray
            GUILayout.BeginVertical(GUI.skin.box);
            
            GUIStyle sliderStyle = new GUIStyle(GUI.skin.horizontalSlider);
            sliderStyle.normal.background = MakeSolidTexture(2, 2, new Color(0.6f, 0.6f, 0.6f));
            
            GUIStyle thumbStyle = new GUIStyle(GUI.skin.horizontalSliderThumb);
            thumbStyle.normal.background = MakeSolidTexture(10, 20, new Color(0.8f, 0.8f, 0.8f));
            thumbStyle.fixedHeight = 20;
            thumbStyle.fixedWidth = 15;
            
            float newSize = GUILayout.HorizontalSlider(sliderValue, -10f, 10f, sliderStyle, thumbStyle);
            GUILayout.EndVertical();
            GUI.backgroundColor = Color.black;
            
            if (Math.Abs(newSize - sliderValue) > 0.1f)
            {
                sliderValue = newSize;
                settingsChanged = true;
            }
            
            GUILayout.Space(20);
            GUI.backgroundColor = new Color(0.5f, 0.5f, 0.5f);
            GUILayout.BeginVertical(GUI.skin.box);
            
            GUIStyle toggleStyle = new GUIStyle(GUI.skin.toggle);
            toggleStyle.normal.textColor = Color.white;
            toggleStyle.onNormal.textColor = Color.white;
            
            bool newEnabled = GUILayout.Toggle(isClockEnabled, "Display Clock", toggleStyle);
            
            GUILayout.EndVertical();
            GUI.backgroundColor = Color.black;
            
            if (newEnabled != isClockEnabled)
            {
                isClockEnabled = newEnabled;
                settingsChanged = true;
            }
            
            GUILayout.Space(20);
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            
            GUI.backgroundColor = new Color(0.5f, 0.5f, 0.5f);
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