using MelonLoader;
using UnityEngine;
using UnityEngine.UI;
using System;

namespace ClockMod
{
    public static class Menu
    {
        // GUI Elements
        private static int selectedPosition = 1;
        private static float sliderValue = 0f;
        private static bool isClockEnabled = true;
        private static bool settingsChanged = false;
        private static ClockMod clockMod;
        
        // GUI state
        private static bool isMenuOpen = false;
        private static Rect windowRect = new Rect(100, 100, 300, 250);
        private static readonly string[] positionNames = new string[] { "Top-Left", "Top-Right", "Bottom-Left", "Bottom-Right" };
        
        // Initialize the menu system
        public static void Initialize(ClockMod mod)
        {
            clockMod = mod;
            selectedPosition = mod.CurrentPositionIndex;
            sliderValue = mod.ClockSize;
            isClockEnabled = mod.IsClockEnabled;
            
            MelonLoader.MelonEvents.OnGUI.Subscribe(OnGUI, 200);
            MelonLogger.Msg("Clock menu system initialized");
        }
        
        // Handle key presses for menu
        public static bool HandleKeyInput()
        {
            // Toggle menu with keybind
            if (Input.GetKeyDown(KeyCode.F12) && Input.GetKey(KeyCode.LeftShift))
            {
                isMenuOpen = !isMenuOpen;
                return true;
            }
            
            // Close menu with ESC
            if (isMenuOpen && Input.GetKeyDown(KeyCode.Escape))
            {
                isMenuOpen = false;
                return true;
            }
            
            return false;
        }
        
        // Main GUI drawing
        private static void OnGUI()
        {
            if (!isMenuOpen) return;
            
            // Set solid black background (not see-through)
            GUI.backgroundColor = Color.black;
            GUI.contentColor = Color.white;
            
            // Create a style with solid background
            GUIStyle windowStyle = new GUIStyle(GUI.skin.window);
            windowStyle.normal.background = MakeSolidTexture(2, 2, Color.black);
            
            windowRect = GUI.Window(12345, windowRect, (GUI.WindowFunction)DrawWindow, "Clock Settings", windowStyle);
            
            // Apply settings if changed
            if (settingsChanged)
            {
                ApplySettings();
                settingsChanged = false;
            }
        }
        
        // Helper method to create solid texture
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
        
        // Draw window contents
        private static void DrawWindow(int windowID)
        {
            // Position dropdown
            GUILayout.Space(10);
            GUILayout.Label("Position:");
            int newPosition = GUILayout.SelectionGrid(selectedPosition, positionNames, 2);
            if (newPosition != selectedPosition)
            {
                selectedPosition = newPosition;
                settingsChanged = true;
            }
            
            GUILayout.Space(15);
            
            // Make size slider stand out more
            GUI.backgroundColor = Color.gray;
            GUILayout.BeginVertical(GUI.skin.box);
            GUI.backgroundColor = Color.black;
            
            // Size slider with larger height and bigger label
            GUIStyle sliderLabelStyle = new GUIStyle(GUI.skin.label);
            sliderLabelStyle.fontSize = 14;
            sliderLabelStyle.fontStyle = FontStyle.Bold;
            
            GUILayout.Label("Size (-10 to +10): " + sliderValue.ToString("F1"), sliderLabelStyle);
            
            // Custom slider style for better visibility
            GUIStyle sliderStyle = new GUIStyle(GUI.skin.horizontalSlider);
            sliderStyle.fixedHeight = 15;
            
            GUIStyle thumbStyle = new GUIStyle(GUI.skin.horizontalSliderThumb);
            thumbStyle.fixedHeight = 25;
            thumbStyle.fixedWidth = 15;
            
            float newSize = GUILayout.HorizontalSlider(sliderValue, -10f, 10f, sliderStyle, thumbStyle);
            if (Math.Abs(newSize - sliderValue) > 0.1f)
            {
                sliderValue = newSize;
                settingsChanged = true;
            }
            GUILayout.EndVertical();
            
            GUILayout.Space(10);
            
            // Enable/disable toggle
            bool newEnabled = GUILayout.Toggle(isClockEnabled, "Display Clock");
            if (newEnabled != isClockEnabled)
            {
                isClockEnabled = newEnabled;
                settingsChanged = true;
            }
            
            GUILayout.Space(20);
            
            // Buttons
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Save", GUILayout.Height(25)))
            {
                ApplySettings();
                SaveSettings();
                isMenuOpen = false;
            }
            
            if (GUILayout.Button("Close", GUILayout.Height(25)))
            {
                isMenuOpen = false;
            }
            GUILayout.EndHorizontal();
            
            // Make the window draggable
            GUI.DragWindow();
        }
        
        // Apply settings to the mod
        private static void ApplySettings()
        {
            if (clockMod != null)
            {
                clockMod.SetClockPosition(selectedPosition);
                clockMod.SetClockSize(sliderValue);
                clockMod.SetClockEnabled(isClockEnabled);
            }
        }
        
        // Save settings to the configuration file
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