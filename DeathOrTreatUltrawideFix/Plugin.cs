using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using SaonaStudios.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DeathOrTreatUltrawideFix;

/// <summary>
/// A BepInEx plugin for fixing ultra-wide support in Death or Treat.
/// </summary>
[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
public class Plugin : BaseUnityPlugin
{
    private const string PluginGuid = "p1xel8ted.deathortreat.ultrawide";
    private const string PluginName = "Death or Treat Ultra-Wide Fix";
    private const string PluginVersion = "0.0.1";

    /// <summary>
    /// Custom scale factor for UI elements.
    /// </summary>
    internal static ConfigEntry<float>? CustomScale { get; private set; }

    /// <summary>
    /// Camera zoom level.
    /// </summary>
    private static ConfigEntry<int>? CameraZoom { get; set; }

    private const float BaseAspectRatio = 16f / 9f;
    private static float PlayerAspectRatio { get; set; }
    private static float ScaleFactor { get; set; }

    /// <summary>
    /// Collection of all CanvasScalers in the scene.
    /// </summary>
    internal static HashSet<CanvasScaler>? CanvasScalers { get; private set; }

    /// <summary>
    /// Updates the aspect ratio values.
    /// </summary>
    private static void UpdateAspectValues()
    {
        PlayerAspectRatio = (float) Display.main.systemWidth / Display.main.systemHeight; //3440 / 1440
        ScaleFactor = PlayerAspectRatio / BaseAspectRatio;
    }

    /// <summary>
    /// Updates the camera orthographic sizes.
    /// </summary>
    private static void UpdateCameras()
    {
        if (CameraZoom == null) return;

        foreach (var camera in Camera.allCameras)
        {
            if (!camera.orthographic) continue;
            camera.orthographicSize = CameraZoom.Value;

            var cbc = camera.GetComponent<Cinemachine.CinemachineBrain>();
            if (cbc == null) return;

            var vc = cbc.ActiveVirtualCamera.VirtualCameraGameObject.GetComponent<Cinemachine.CinemachineVirtualCamera>();
            if (vc == null) return;

            var lens = vc.m_Lens;
            lens.OrthographicSize = CameraZoom.Value;
            vc.m_Lens = lens;

            if (Instance != null)
            {
                Instance.Logger.LogWarning($"Camera Zoom of {cbc.name} and {vc.name} set to {CameraZoom.Value}");
            }
        }
    }

    /// <summary>
    /// Singleton instance of the plugin.
    /// </summary>
    private static Plugin? Instance { get; set; }

    /// <summary>
    /// Method called when the plugin is loaded. Sets up the configuration, harmony patches, and event listeners.
    /// </summary>
    private void Awake()
    {
        Instance = this;
        SceneManager.sceneLoaded += SceneLoaded;
        Application.runInBackground = true;
        CanvasScalers = new HashSet<CanvasScaler>();
        UpdateAspectValues();

        CameraZoom = Config.Bind("General", "Camera Zoom", 5, new ConfigDescription("Adjusts the zoom level of the camera.", new AcceptableValueRange<int>(1, 15)));
        CameraZoom.SettingChanged += (_, _) => { UpdateCameras(); };

        CustomScale = Config.Bind("General", "Custom Scale", 1 * ScaleFactor, new ConfigDescription("Custom scale factor for UI elements. It's calculated automatically based on a 16:9 aspect ratio and your display's aspect ratio.", new AcceptableValueRange<float>(0.5f, 2f)));
        CustomScale.SettingChanged += (_, _) =>
        {
            foreach (var scaler in CanvasScalers)
            {
                if (CustomScale.Value <= 1)
                {
                    scaler.scaleFactor = CustomScale.Value;
                }
                else
                {
                    scaler.scaleFactor = 1 * CustomScale.Value;
                }

                scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            }
        };

        Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), PluginGuid);
        Logger.LogInfo($"Plugin {PluginName} is loaded!");
    }

    /// <summary>
    /// Event handler for scene loaded. Skips the logo scene and updates the cameras.
    /// </summary>
    private static void SceneLoaded(Scene arg0, LoadSceneMode arg1)
    {
        if (arg0.name.Contains("Logo"))
        {
            SceneHandler.instance.LoadScene("MainMenu");
        }

        UpdateCameras();
    }

    private const string BepInExText = "\nAssembly = Assembly-CSharp.dll\nType = SceneHandler\nMethod = .cctor";

    /// <summary>
    /// Event handler for plugin destroyed. Logs an error message with instructions.
    /// </summary>
    private void OnDestroy()
    {
        Logger.LogError($"I've been disabled! Make sure that the entry point details in BepInEx.cfg are as so: {BepInExText}");
    }

    /// <summary>
    /// Event handler for plugin disabled. Logs an error message with instructions.
    /// </summary>
    private void OnDisable()
    {
        Logger.LogError($"I've been disabled! Make sure that the entry point details in BepInEx.cfg are as so: {BepInExText}");
    }
}