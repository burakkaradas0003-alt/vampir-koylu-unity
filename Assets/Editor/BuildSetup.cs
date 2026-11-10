// Unity Build Automation bu projeyi Editor açmadan derler. Derlemeden hemen önce
// "Pre-Export Method" olarak BuildSetup.PreExport çağrılır ve projeyi hazırlar:
// URP grafik hattı, malzemeler, sahne, Android ayarları. Elle de çalıştırılabilir: menü Zuplay > Kurulum.
#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class BuildSetup
{
    public static void PreExport() { Setup(); }

    // GitHub Actions: tarayıcıda açılan web sürümü (sıkıştırmasız, her sunucuda çalışır)
    public static void BuildWebGL()
    {
        Setup();
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
        PlayerSettings.WebGL.decompressionFallback = true; // sunucu sıkıştırma başlığı göndermese de açılır
        PlayerSettings.WebGL.dataCaching = false;
        PlayerSettings.runInBackground = true;
        var opt = new BuildPlayerOptions { scenes = new[] { "Assets/Scenes/Main.unity" }, locationPathName = "Builds/WebGL", target = BuildTarget.WebGL, options = BuildOptions.None };
        var rep = BuildPipeline.BuildPlayer(opt);
        Debug.Log("[Zuplay] WebGL derlemesi: " + rep.summary.result + " · " + rep.summary.totalErrors + " hata · " + (rep.summary.totalSize / 1048576) + " MB");
        if (rep.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded) EditorApplication.Exit(1);
    }

    public static void BuildAndroid()
    {
        Setup();
        var opt = new BuildPlayerOptions { scenes = new[] { "Assets/Scenes/Main.unity" }, locationPathName = "Builds/Android/VampirKoylu.apk", target = BuildTarget.Android, options = BuildOptions.None };
        var rep = BuildPipeline.BuildPlayer(opt);
        if (rep.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded) EditorApplication.Exit(1);
    }

    [MenuItem("Zuplay/Kurulum")]
    public static void Setup()
    {
        ReimportModels();
        foreach (var g in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Resources/Emoji" })) AssetDatabase.ImportAsset(AssetDatabase.GUIDToAssetPath(g), ImportAssetOptions.ForceUpdate);
        var urp = SetupPipeline();
        SetupMaterials();
        SetupScene();
        SetupPlayer();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Zuplay] Kurulum tamam. URP: " + (urp != null));
    }

    static void ReimportModels()
    {
        foreach (var p in new[] {
            "Assets/Resources/Character/Creative_Character_free.fbx",
            "Assets/Resources/Campfire/Campfire.fbx",
            "Assets/Resources/Grass/wild_grass.fbx",
            "Assets/Resources/Campfire/campfire_normal.jpg" })
            if (File.Exists(p)) AssetDatabase.ImportAsset(p, ImportAssetOptions.ForceUpdate);
    }

    static UniversalRenderPipelineAsset SetupPipeline()
    {
        Directory.CreateDirectory("Assets/Settings");
        const string rpPath = "Assets/Settings/URP.asset", rdPath = "Assets/Settings/URP_Renderer.asset";
        var urp = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(rpPath);
        if (urp == null)
        {
            var rd = ScriptableObject.CreateInstance<UniversalRendererData>();
            AssetDatabase.CreateAsset(rd, rdPath);
            urp = UniversalRenderPipelineAsset.Create(rd);
            AssetDatabase.CreateAsset(urp, rpPath);
        }
        urp.supportsHDR = true;
        urp.msaaSampleCount = 2;
        urp.shadowDistance = 32f;
        urp.shadowCascadeCount = 1;
        urp.renderScale = 1f;
        EditorUtility.SetDirty(urp);
        GraphicsSettings.defaultRenderPipeline = urp;
        int cur = QualitySettings.GetQualityLevel();
        for (int i = 0; i < QualitySettings.names.Length; i++)
        {
            QualitySettings.SetQualityLevel(i, false);
            QualitySettings.renderPipeline = urp;
        }
        QualitySettings.SetQualityLevel(cur, false);
        return urp;
    }

    // Çalışma anında Shader.Find yerine bu malzemeler kopyalanır (gölgelendiriciler derlemeye kesin girsin)
    static void SetupMaterials()
    {
        Directory.CreateDirectory("Assets/Resources/Mats");
        MakeMat("Lit", "Universal Render Pipeline/Lit", null);
        MakeMat("Unlit", "Universal Render Pipeline/Unlit", null);
        MakeMat("Grass", "Zu/Grass", m => m.enableInstancing = true);
        MakeMat("Flame", "Zu/Flame", null);
        MakeMat("Glow", "Zu/Glow", null);
        MakeMat("Ghost", "Universal Render Pipeline/Lit", m =>
        {
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 0f);
            m.SetOverrideTag("RenderType", "Transparent");
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            m.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0f);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)RenderQueue.Transparent;
            m.SetColor("_BaseColor", new Color(0.81f, 0.88f, 1f, 0.36f));
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", new Color(0.35f, 0.47f, 1f) * 0.55f);
        });
        MakeMat("LitEmissive", "Universal Render Pipeline/Lit", m => m.EnableKeyword("_EMISSION"));
        MakeMat("LitNormal", "Universal Render Pipeline/Lit", m => { m.EnableKeyword("_NORMALMAP"); m.EnableKeyword("_EMISSION"); m.EnableKeyword("_OCCLUSIONMAP"); });
    }

    static void MakeMat(string name, string shader, System.Action<Material> cfg)
    {
        var path = "Assets/Resources/Mats/" + name + ".mat";
        var sh = Shader.Find(shader);
        if (sh == null) { Debug.LogError("[Zuplay] Gölgelendirici bulunamadı: " + shader); return; }
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null) { m = new Material(sh); AssetDatabase.CreateAsset(m, path); }
        m.shader = sh;
        cfg?.Invoke(m);
        EditorUtility.SetDirty(m);
    }

    static void SetupScene()
    {
        Directory.CreateDirectory("Assets/Scenes");
        const string path = "Assets/Scenes/Main.unity";
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var go = new GameObject("Oyun");
        go.AddComponent<VK.Boot>();
        EditorSceneManager.SaveScene(scene, path);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(path, true) };
    }

    static void SetupPlayer()
    {
        PlayerSettings.companyName = "Zuplay";
        PlayerSettings.productName = "Vampir Köylü";
        PlayerSettings.colorSpace = ColorSpace.Linear;
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
        PlayerSettings.allowedAutorotateToLandscapeLeft = true;
        PlayerSettings.allowedAutorotateToLandscapeRight = true;
        PlayerSettings.allowedAutorotateToPortrait = false;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.zuplay.vampirkoylu");
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, "com.zuplay.vampirkoylu");
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24;
        PlayerSettings.bundleVersion = "0.1.0";
        PlayerSettings.Android.bundleVersionCode = 1;
        QualitySettings.vSyncCount = 0;
    }
}

// Resources altındaki modeller çalışma anında okunabilsin (köşe renkleri, parmak eksenleri için)
public class ZuImport : AssetPostprocessor
{
    void OnPreprocessModel()
    {
        if (!assetPath.Contains("/Resources/")) return;
        var mi = (ModelImporter)assetImporter;
        mi.isReadable = true;
        mi.importAnimation = false;
        mi.importCameras = false;
        mi.importLights = false;
        mi.materialImportMode = ModelImporterMaterialImportMode.None;
        mi.animationType = ModelImporterAnimationType.Generic;
    }

    void OnPreprocessTexture()
    {
        if (!assetPath.Contains("/Resources/")) return;
        var ti = (TextureImporter)assetImporter;
        if (assetPath.Contains("_normal")) ti.textureType = TextureImporterType.NormalMap;
        if (assetPath.Contains("/Emoji/")) { ti.textureType = TextureImporterType.Sprite; ti.spriteImportMode = SpriteImportMode.Single; ti.alphaIsTransparency = true; ti.mipmapEnabled = false; ti.textureCompression = TextureImporterCompression.Uncompressed; }
        if (assetPath.Contains("turf")) { ti.wrapMode = TextureWrapMode.Mirror; ti.isReadable = true; }
        if (assetPath.Contains("flames")) ti.wrapMode = TextureWrapMode.Clamp;
        ti.maxTextureSize = 2048;
    }
}
#endif
