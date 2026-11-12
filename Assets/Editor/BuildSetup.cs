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
        foreach (var g in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Resources/Emoji", "Assets/Resources/Items", "Assets/Resources/UI" })) AssetDatabase.ImportAsset(AssetDatabase.GUIDToAssetPath(g), ImportAssetOptions.ForceUpdate);
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
        urp.shadowCascadeCount = 2;
        urp.renderScale = 1f;
        var so = new SerializedObject(urp);
        void SetI(string n, int v) { var p = so.FindProperty(n); if (p != null) p.intValue = v; else Debug.LogWarning("[Zuplay] yok: " + n); }
        void SetB(string n, bool v) { var p = so.FindProperty(n); if (p != null) p.boolValue = v; else Debug.LogWarning("[Zuplay] yok: " + n); }
        SetI("m_MainLightShadowmapResolution", 1024);
        SetB("m_SoftShadowsSupported", true);
        SetB("m_AdditionalLightShadowsSupported", true);
        SetI("m_AdditionalLightsShadowmapResolution", 1024);
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(urp);
        AddSSAO(AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rdPath));
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

    // ortam perdelemesi (SSAO): mobil için yarım çözünürlük, yalnız derinlikten, düşük örnek
    static void AddSSAO(UniversalRendererData rd)
    {
        if (rd == null) return;
        var t = System.Type.GetType("UnityEngine.Rendering.Universal.ScreenSpaceAmbientOcclusion, Unity.RenderPipelines.Universal.Runtime");
        if (t == null) { Debug.LogWarning("[Zuplay] SSAO sınıfı bulunamadı"); return; }
        ScriptableRendererFeature f = rd.rendererFeatures.Find(x => x != null && x.GetType() == t);
        if (f == null)
        {
            f = (ScriptableRendererFeature)ScriptableObject.CreateInstance(t); f.name = "SSAO";
            AssetDatabase.AddObjectToAsset(f, rd); rd.rendererFeatures.Add(f);
            var rso = new SerializedObject(rd); var map = rso.FindProperty("m_RendererFeatureMap");
            if (map != null && AssetDatabase.TryGetGUIDAndLocalFileIdentifier(f, out string _, out long lid)) { map.arraySize++; map.GetArrayElementAtIndex(map.arraySize - 1).longValue = lid; rso.ApplyModifiedPropertiesWithoutUndo(); }
        }
        var so = new SerializedObject(f);
        void B(string n, bool v) { var p = so.FindProperty("m_Settings." + n); if (p != null) p.boolValue = v; }
        void F(string n, float v) { var p = so.FindProperty("m_Settings." + n); if (p != null) p.floatValue = v; }
        void E(string n, int v) { var p = so.FindProperty("m_Settings." + n); if (p != null) p.enumValueIndex = v; }
        B("Downsample", true); B("AfterOpaque", false); E("Source", 0); E("NormalSamples", 0);
        F("Intensity", 1.1f); F("DirectLightingStrength", 0.3f); F("Radius", 0.45f); F("Falloff", 40f);
        E("Samples", 2); E("BlurQuality", 2); E("AOMethod", 0);
        so.ApplyModifiedPropertiesWithoutUndo();
        f.SetActive(true);
        EditorUtility.SetDirty(f); EditorUtility.SetDirty(rd);
        Debug.Log("[Zuplay] SSAO eklendi");
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
        MakeMat("Cloud", "Zu/Cloud", m => m.enableInstancing = true);
        MakeMat("Sky", "Skybox/Procedural", m => { m.SetFloat("_SunDisk", 2f); m.EnableKeyword("_SUNDISK_HIGH_QUALITY"); });
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
        // mat yüzeyler: parlak yansıma ve plastik parlama yok (kumaş, ahşap, sıva)
        System.Action<Material> matte = m => { m.SetFloat("_SpecularHighlights", 0f); m.EnableKeyword("_SPECULARHIGHLIGHTS_OFF"); m.SetFloat("_EnvironmentReflections", 0f); m.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF"); m.SetFloat("_Smoothness", 0.1f); };
        MakeMat("LitMatte", "Universal Render Pipeline/Lit", matte);
        MakeMat("LitMatteEmissive", "Universal Render Pipeline/Lit", m => { matte(m); m.EnableKeyword("_EMISSION"); });
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
        // açılıştaki Unity logosu (Unity 6'da Personal lisansta da kapatılabilir)
        PlayerSettings.SplashScreen.show = false;
        PlayerSettings.SplashScreen.showUnityLogo = false;
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
        if (assetPath.Contains("/Village/") || assetPath.Contains("/Medieval/")) { mi.animationType = ModelImporterAnimationType.None; mi.importBlendShapes = false; mi.meshCompression = ModelImporterMeshCompression.Off; }
    }

    void OnPreprocessTexture()
    {
        if (!assetPath.Contains("/Resources/")) return;
        var ti = (TextureImporter)assetImporter;
        if (assetPath.Contains("_normal")) ti.textureType = TextureImporterType.NormalMap;
        if (assetPath.Contains("/Emoji/") || assetPath.Contains("/Items/")) { ti.textureType = TextureImporterType.Sprite; ti.spriteImportMode = SpriteImportMode.Single; ti.alphaIsTransparency = true; ti.mipmapEnabled = false; ti.textureCompression = TextureImporterCompression.Uncompressed; }
        if (assetPath.Contains("/Resources/UI/"))
        {
            ti.textureType = TextureImporterType.Sprite; ti.spriteImportMode = SpriteImportMode.Single; ti.alphaIsTransparency = true; ti.mipmapEnabled = false; ti.textureCompression = TextureImporterCompression.Uncompressed;
            var n = System.IO.Path.GetFileNameWithoutExtension(assetPath); // kenarlar: sol, alt, sağ, üst
            ti.spriteBorder = n == "ornate" ? new Vector4(72, 72, 72, 72) : n == "tile" || n == "tile_on" ? new Vector4(28, 28, 28, 28) : n == "btn_purple" || n == "btn_dark" || n == "pill_dark" ? new Vector4(44, 30, 44, 30) : n == "frame" ? new Vector4(72, 72, 72, 72) : n == "plaque" ? new Vector4(70, 0, 70, 0) : n.StartsWith("ribbon") ? new Vector4(26, 0, 26, 0) : n == "btn_gold" ? new Vector4(70, 0, 70, 0) : Vector4.zero;
        }
        if (assetPath.Contains("/Resources/Cards/") || assetPath.Contains("/Resources/Rail/"))
        {
            // kullanıcının rol kartları: dosyaya dokunulmaz, yalnız içe aktarırken sıkıştırılır
            ti.textureType = TextureImporterType.Sprite; ti.spriteImportMode = SpriteImportMode.Single; ti.alphaIsTransparency = true; ti.mipmapEnabled = false;
            ti.textureCompression = TextureImporterCompression.CompressedHQ; ti.maxTextureSize = assetPath.Contains("/Rail/") ? 256 : 1024;
        }
        if (assetPath.Contains("turf")) { ti.wrapMode = TextureWrapMode.Mirror; ti.isReadable = true; }
        if (assetPath.Contains("flames")) ti.wrapMode = TextureWrapMode.Clamp;
        if (assetPath.Contains("/Village/")) { ti.textureCompression = TextureImporterCompression.Uncompressed; ti.wrapMode = TextureWrapMode.Repeat; ti.filterMode = FilterMode.Bilinear; ti.mipmapEnabled = true; ti.sRGBTexture = true; }
        if (!assetPath.Contains("/Resources/Cards/") && !assetPath.Contains("/Resources/Rail/")) ti.maxTextureSize = 2048;
    }
}
#endif
