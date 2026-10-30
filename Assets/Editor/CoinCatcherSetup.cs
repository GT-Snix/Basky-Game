using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Creates the Coin Catcher scene and configures a macOS build.
// Run from the menu (Coin Catcher > ...) or in batch mode with -executeMethod.
public static class CoinCatcherSetup
{
    const string TemplateScene = "Assets/Scenes/SampleScene.unity";
    const string GameScene = "Assets/Scenes/CoinCatcher.unity";
    const string MacBuildPath = "Builds/macOS/CoinCatcher.app";

    [MenuItem("Coin Catcher/Create Scene")]
    public static void CreateScene()
    {
        // Start from the 2D URP sample scene so the camera and Global Light 2D come preconfigured.
        var scene = EditorSceneManager.OpenScene(TemplateScene, OpenSceneMode.Single);
        if (Object.FindAnyObjectByType<GameManager>() == null)
            new GameObject("Game").AddComponent<GameManager>();
        EditorSceneManager.SaveScene(scene, GameScene);

        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(GameScene, true) };

        PlayerSettings.productName = "Coin Catcher";
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.defaultScreenWidth = 1280;
        PlayerSettings.defaultScreenHeight = 800;
        PlayerSettings.resizableWindow = true;

        AssetDatabase.SaveAssets();
        Debug.Log("Coin Catcher scene created at " + GameScene);
    }

    [MenuItem("Coin Catcher/Build for macOS")]
    public static void BuildMac()
    {
        if (!File.Exists(GameScene)) CreateScene();
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { GameScene },
            locationPathName = MacBuildPath,
            target = BuildTarget.StandaloneOSX,
            options = BuildOptions.None
        });
        Debug.Log("macOS build: " + report.summary.result + " -> " + MacBuildPath);
        if (Application.isBatchMode && report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            EditorApplication.Exit(1);
    }
}
