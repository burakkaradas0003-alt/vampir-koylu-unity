// Stüdyo: karakter portreleri (lobi kartları, profil, sonuç ekranı) ve canlı vitrin (özelleştirme).
// Sahnenin çok yukarısında, yalnız kendi katmanını gören kameralarla saydam arka plana çizer.
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace VK
{
    public class Studio : MonoBehaviour
    {
        public static Studio I;
        public const int LAYER = 8;
        static readonly Vector3 ORIGIN = new Vector3(0, 320, 0);
        Camera pcam, vcam; Light key, rim;
        readonly Dictionary<string, RenderTexture> cache = new Dictionary<string, RenderTexture>();
        readonly Queue<Action> jobs = new Queue<Action>(); bool busy;

        public static Studio Make()
        {
            var go = new GameObject("Stüdyo"); I = go.AddComponent<Studio>(); I.Build(); return I;
        }
        Camera Cam(string n)
        {
            var g = new GameObject(n); g.transform.SetParent(transform, false);
            var c = g.AddComponent<Camera>(); c.clearFlags = CameraClearFlags.SolidColor; c.backgroundColor = new Color(0, 0, 0, 0);
            c.cullingMask = 1 << LAYER; c.fieldOfView = 26; c.nearClipPlane = 0.05f; c.farClipPlane = 30; c.enabled = false; c.allowHDR = false; c.allowMSAA = true;
            var d = g.AddComponent<UniversalAdditionalCameraData>(); d.renderPostProcessing = false; d.renderShadows = false; d.antialiasing = AntialiasingMode.None;
            return c;
        }
        void Build()
        {
            if (World.I != null && World.I.cam != null) World.I.cam.cullingMask &= ~(1 << LAYER);
            pcam = Cam("portre kamerası"); vcam = Cam("vitrin kamerası");
            // stüdyo ışıkları yalnız stüdyo katmanını aydınlatır
            key = new GameObject("anahtar ışık").AddComponent<Light>(); key.type = LightType.Point; key.range = 12; key.intensity = 2.2f; key.color = new Color(1f, 0.95f, 0.9f); key.cullingMask = 1 << LAYER; key.transform.SetParent(transform, false);
            rim = new GameObject("kenar ışık").AddComponent<Light>(); rim.type = LightType.Point; rim.range = 10; rim.intensity = 2.6f; rim.color = new Color(0.62f, 0.5f, 1f); rim.cullingMask = 1 << LAYER; rim.transform.SetParent(transform, false);
        }
        static void SetLayer(GameObject g) { foreach (var t in g.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = LAYER; }

        // ---------------------------------------------------------------- portre (bir kez çizilir, önbellekte tutulur)
        public RenderTexture Portrait(Look look, int size = 160)
        {
            string k = look.Json() + "#" + size;
            if (cache.TryGetValue(k, out var rt) && rt != null) return rt;
            rt = new RenderTexture(size, size, 16, RenderTextureFormat.ARGB32) { antiAliasing = 2, name = "portre" }; rt.Create();
            cache[k] = rt;
            var l = look.Clone();
            jobs.Enqueue(() => StartCoroutine(Shoot(l, rt)));
            if (!busy) StartCoroutine(Pump());
            return rt;
        }
        IEnumerator Pump()
        {
            busy = true;
            while (jobs.Count > 0) { jobs.Dequeue()(); yield return null; yield return null; yield return null; }
            busy = false;
        }
        IEnumerator Shoot(Look l, RenderTexture rt)
        {
            var spot = ORIGIN + new Vector3(-30, 0, 0);
            var rig = Rig.Make(l, 1, false, false, false); rig.preview = true; rig.transform.position = spot; rig.transform.rotation = Quaternion.Euler(0, 180, 0); SetLayer(rig.gameObject);
            yield return null; // poz uygulansın
            var head = rig.transform.Find("model");
            Vector3 face = spot + new Vector3(0, 1.47f, 0);
            pcam.transform.position = face + new Vector3(0.18f, 0.05f, -1.35f); pcam.transform.LookAt(face + new Vector3(0, -0.03f, 0)); pcam.fieldOfView = 24;
            Lamp(face, -1);
            pcam.targetTexture = rt; pcam.enabled = true;
            yield return null; yield return null;
            pcam.enabled = false; pcam.targetTexture = null;
            Destroy(rig.gameObject);
        }
        void Lamp(Vector3 at, float facing)
        {
            key.transform.position = at + new Vector3(1.2f, 1.0f, 1.8f * facing); rim.transform.position = at + new Vector3(-1.4f, 0.8f, -1.6f * facing);
        }

        // ---------------------------------------------------------------- canlı vitrin (özelleştirme ve profil)
        Rig show; RenderTexture showRT; public float yaw = 0; Look showLook; float bob;
        public RenderTexture Showcase(Look look, int w = 512, int h = 640, bool withSet = false)
        {
            useSet = withSet; if (withSet) BuildSet(); if (set != null) set.SetActive(withSet);
            vcam.backgroundColor = withSet ? new Color(0.1f, 0.08f, 0.22f) : new Color(0, 0, 0, 0);
            vcam.GetComponent<UniversalAdditionalCameraData>().renderShadows = withSet;
            if (showRT == null || showRT.width != w || showRT.height != h) { if (showRT != null) showRT.Release(); showRT = new RenderTexture(w, h, 16, RenderTextureFormat.ARGB32) { antiAliasing = 4, name = "vitrin" }; showRT.Create(); }
            SetShow(look);
            vcam.targetTexture = showRT; vcam.enabled = true;
            return showRT;
        }
        public void SetShow(Look look)
        {
            if (show != null && showLook != null && showLook.Json() == look.Json()) return;
            if (show != null) Destroy(show.gameObject);
            showLook = look.Clone();
            show = Rig.Make(showLook, 1, false, false, false); show.preview = true; SetLayer(show.gameObject);
            show.transform.position = ORIGIN; bob = 1;
        }
        // giyinme seti (bir kez kurulur): köy evinin önü, taş zemin, çimen, fener, fıçı
        GameObject set; bool useSet; Light lantern; MaterialPropertyBlock ringMpb; MeshRenderer ringR; float sunI; Color sunC; bool dimmed;
        // vitrin kamerası çizerken güneş kısılır: set gece gibi aydınlanır (oyun sahnesi etkilenmez)
        void OnEnable() { UnityEngine.Rendering.RenderPipelineManager.beginCameraRendering += BeginCam; UnityEngine.Rendering.RenderPipelineManager.endCameraRendering += EndCam; }
        void OnDisable() { UnityEngine.Rendering.RenderPipelineManager.beginCameraRendering -= BeginCam; UnityEngine.Rendering.RenderPipelineManager.endCameraRendering -= EndCam; }
        void BeginCam(UnityEngine.Rendering.ScriptableRenderContext ctx, Camera c)
        {
            if (c != vcam || !useSet || World.I == null || World.I.Sun == null) return;
            var s = World.I.Sun; sunI = s.intensity; sunC = s.color; dimmed = true;
            s.intensity = 0.42f; s.color = new Color(0.55f, 0.6f, 1f);
        }
        void EndCam(UnityEngine.Rendering.ScriptableRenderContext ctx, Camera c)
        {
            if (!dimmed || c != vcam || World.I == null || World.I.Sun == null) return;
            dimmed = false; var s = World.I.Sun; s.intensity = sunI; s.color = sunC;
        }
        void BuildSet()
        {
            if (set != null || World.I == null) return;
            set = new GameObject("giyinme seti"); set.transform.SetParent(transform, false); set.transform.position = ORIGIN;
            var w = World.I;
            void P(string n, Vector3 local, float yaw, float sc) { var g = w.PutVillage(n, set.transform, local, yaw, sc); }
            // karakter değirmenin kapısının önünde durur (yoksa köy evi)
            if (w.PutMedieval("windmill", set.transform, new Vector3(0.3f, 0, 4.6f), 180, 0.85f) == null) P("House_01", new Vector3(0.6f, 0, 4.9f), 180, 0.88f);
            P("Street_Lamp", new Vector3(-2.2f, 0, 1.6f), 90, 0.95f);
            P("Vooden_Barrel", new Vector3(1.9f, 0, 1.3f), 30, 1.15f);
            P("Wooden_Case", new Vector3(2.5f, 0, 0.6f), 10, 1.1f);
            P("Bushes_01", new Vector3(-3.4f, 0, 2.6f), 0, 1.5f);
            P("Bushes_03", new Vector3(3.6f, 0, 2.9f), 40, 1.4f);
            P("Flower_Yellow", new Vector3(-1.2f, 0, 1.9f), 0, 2f); P("Flower_Purple", new Vector3(-0.9f, 0, 2.1f), 50, 2f); P("Flower_Blue", new Vector3(2.0f, 0, 2.0f), 0, 2f);
            P("Tree_01", new Vector3(-5.5f, 0, 7.5f), 0, 1.1f); P("Tree_02", new Vector3(6.5f, 0, 8.5f), 0, 1.6f); P("Tree_02", new Vector3(-8f, 0, 10f), 0, 1.8f);
            w.StudioGround(set.transform);
            // gece seti: malzemeler kopyalanıp akşam mavisine çekilir, pencereler sıcak yanar
            var nightTint = new Color(0.46f, 0.44f, 0.62f);
            var copies = new Dictionary<Material, Material>();
            foreach (var r in set.GetComponentsInChildren<Renderer>(true))
            {
                var ms = r.sharedMaterials;
                for (int i = 0; i < ms.Length; i++)
                {
                    var m = ms[i]; if (m == null) continue;
                    if (!copies.TryGetValue(m, out var c))
                    {
                        c = new Material(m);
                        if (c.HasProperty("_BaseColor")) c.SetColor("_BaseColor", c.GetColor("_BaseColor") * nightTint);
                        if (c.HasProperty("_Tint")) c.SetColor("_Tint", nightTint);
                        if (c.IsKeywordEnabled("_EMISSION")) c.SetColor("_EmissionColor", new Color(1f, 0.58f, 0.24f) * 2.4f);
                        copies[m] = c;
                    }
                    ms[i] = c;
                }
                r.sharedMaterials = ms;
            }
            // altın parıltılı taş kaide
            var ped = GameObject.CreatePrimitive(PrimitiveType.Cylinder); Destroy(ped.GetComponent<Collider>()); ped.name = "kaide"; ped.transform.SetParent(set.transform, false);
            ped.transform.localPosition = new Vector3(0, 0.03f, 0); ped.transform.localScale = new Vector3(1.75f, 0.03f, 1.75f);
            var pm = new Material(Resources.Load<Material>("Mats/LitMatte") ?? Resources.Load<Material>("Mats/Lit")); pm.SetColor("_BaseColor", new Color(0.3f, 0.29f, 0.32f)); ped.GetComponent<MeshRenderer>().sharedMaterial = pm;
            var ringGo = new GameObject("kaide ışığı"); ringGo.transform.SetParent(set.transform, false); ringGo.transform.localPosition = new Vector3(0, 0.07f, 0); ringGo.transform.localRotation = Quaternion.Euler(90, 0, 0); ringGo.transform.localScale = Vector3.one * 1.9f;
            var qm = new Mesh(); qm.vertices = new[] { new Vector3(-0.5f, -0.5f, 0), new Vector3(0.5f, -0.5f, 0), new Vector3(-0.5f, 0.5f, 0), new Vector3(0.5f, 0.5f, 0) }; qm.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1) }; qm.triangles = new[] { 0, 2, 1, 1, 2, 3 }; qm.RecalculateBounds();
            ringGo.AddComponent<MeshFilter>().sharedMesh = qm;
            var gm = new Material(Resources.Load<Material>("Mats/Glow")); gm.SetTexture("_MainTex", Resources.Load<Texture2D>("UI/pedestal_ring")); gm.enableInstancing = true;
            var rr = ringGo.AddComponent<MeshRenderer>(); rr.sharedMaterial = gm; var mpb = new MaterialPropertyBlock(); mpb.SetColor("_Color", new Color(1f, 0.78f, 0.38f, 0.95f)); rr.SetPropertyBlock(mpb); ringMpb = mpb; ringR = rr;
            // ay (sol üstte, uzakta)
            var moon = new GameObject("ay"); moon.transform.SetParent(set.transform, false); moon.transform.localPosition = new Vector3(-5.5f, 6.5f, 14f); moon.transform.localScale = Vector3.one * 2.2f;
            moon.AddComponent<MeshFilter>().sharedMesh = qm; var mr = moon.AddComponent<MeshRenderer>(); var mm = new Material(Resources.Load<Material>("Mats/Glow")); mm.SetTexture("_MainTex", Resources.Load<Texture2D>("UI/moon") != null ? Resources.Load<Texture2D>("UI/moon") : null); mr.sharedMaterial = mm;
            var mpb2 = new MaterialPropertyBlock(); mpb2.SetColor("_Color", new Color(0.85f, 0.88f, 1f, 0.9f)); mr.SetPropertyBlock(mpb2);
            // gece göğü: mor-lacivert degrade ve yıldızlar (düz siyah yerine)
            var skyT = new Texture2D(4, 128, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < 128; y++) { float k = y / 127f; var cc = Color.Lerp(new Color(0.32f, 0.2f, 0.42f), new Color(0.07f, 0.06f, 0.2f), Mathf.Pow(k, 0.7f)); for (int x = 0; x < 4; x++) skyT.SetPixel(x, y, cc); }
            skyT.Apply();
            var sky = GameObject.CreatePrimitive(PrimitiveType.Quad); Destroy(sky.GetComponent<Collider>()); sky.name = "gök"; sky.transform.SetParent(set.transform, false);
            sky.transform.localPosition = new Vector3(1, 9, 26); sky.transform.localScale = new Vector3(90, 34, 1);
            var skm = new Material(Resources.Load<Material>("Mats/Unlit")); skm.SetTexture("_BaseMap", skyT); skm.SetColor("_BaseColor", Color.white); sky.GetComponent<MeshRenderer>().sharedMaterial = skm;
            var rs = new System.Random(4);
            for (int i = 0; i < 60; i++)
            {
                var st = new GameObject("yıldız"); st.transform.SetParent(set.transform, false); st.transform.localPosition = new Vector3((float)rs.NextDouble() * 60 - 28, 5 + (float)rs.NextDouble() * 18, 25.5f); st.transform.localScale = Vector3.one * (0.12f + (float)rs.NextDouble() * 0.18f);
                st.AddComponent<MeshFilter>().sharedMesh = qm; var sr2 = st.AddComponent<MeshRenderer>(); sr2.sharedMaterial = Resources.Load<Material>("Mats/Glow") is Material gmat ? new Material(gmat) { mainTexture = Resources.Load<Texture2D>("UI/moon") } : null;
                var sp = new MaterialPropertyBlock(); sp.SetColor("_Color", new Color(0.9f, 0.9f, 1f, 0.5f + (float)rs.NextDouble() * 0.5f)); sr2.SetPropertyBlock(sp);
            }
            // pencere önlerinde sıcak ışıklar (ev cephesi ve zemin aydınlansın)
            foreach (var wp in new[] { new Vector3(-1.6f, 2.4f, 3.2f), new Vector3(2.2f, 2.4f, 3.2f), new Vector3(0.4f, 1.2f, 3.4f) })
            {
                var wl = new GameObject("pencere ışığı").AddComponent<Light>(); wl.transform.SetParent(set.transform, false); wl.transform.localPosition = wp;
                wl.type = LightType.Point; wl.range = 5.5f; wl.intensity = 2.4f; wl.color = new Color(1f, 0.6f, 0.28f); wl.cullingMask = 1 << LAYER;
            }
            // önden yumuşak ay ışığı dolgusu (karakterin yüzü ve zemin seçilsin)
            var front = new GameObject("ön dolgu").AddComponent<Light>(); front.transform.SetParent(set.transform, false); front.transform.localPosition = new Vector3(0.5f, 3.2f, -3.5f);
            front.type = LightType.Point; front.range = 12; front.intensity = 1.6f; front.color = new Color(0.66f, 0.66f, 1f); front.cullingMask = 1 << LAYER;
            // fener ve pencerelerden gelen sıcak ışık, arkadan mor dolgu
            lantern = new GameObject("fener ışığı").AddComponent<Light>(); lantern.transform.SetParent(set.transform, false); lantern.transform.localPosition = new Vector3(-2.0f, 2.3f, 1.4f);
            lantern.type = LightType.Point; lantern.range = 7; lantern.intensity = 3.2f; lantern.color = new Color(1f, 0.62f, 0.3f); lantern.cullingMask = 1 << LAYER;
            var fill = new GameObject("mor dolgu").AddComponent<Light>(); fill.transform.SetParent(set.transform, false); fill.transform.localPosition = new Vector3(2.5f, 3f, 3.5f);
            fill.type = LightType.Point; fill.range = 9; fill.intensity = 2.2f; fill.color = new Color(0.55f, 0.4f, 1f); fill.cullingMask = 1 << LAYER;
            foreach (var t in set.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = LAYER;
        }
        public void Cheer(float k) { if (show != null) show.cheer = k; }
        public void HideShowcase() { vcam.enabled = false; if (show != null) { Destroy(show.gameObject); show = null; showLook = null; } }
        void LateUpdate() { try { LateUpdateInner(); } catch (System.Exception e) { ZuLog.Err("Studio.LateUpdate", e); } }
        void LateUpdateInner()
        {
            if (show == null || !vcam.enabled) return;
            bob = Mathf.MoveTowards(bob, 0, Time.deltaTime * 3);
            show.transform.rotation = Quaternion.Euler(0, 180 + yaw, 0);
            show.transform.position = ORIGIN + Vector3.up * (Mathf.Sin(bob * Mathf.PI) * 0.06f);
            var c = ORIGIN + new Vector3(0, 0.88f, 0);
            if (useSet)
            {
                // giyinme seti: karakter evin kapısı önünde, kamera hafif aşağıdan ve soldan
                // görüntü tüm ekranı kaplar; karakter ekranın soldan ~%31'inde durur (sağda seçenek paneli)
                float asp = showRT != null ? showRT.width / (float)showRT.height : 1.6f, dist = 4.9f;
                float halfW = Mathf.Tan(34 * 0.5f * Mathf.Deg2Rad) * dist * asp, shift = halfW * 2 * (0.5f - 0.31f);
                vcam.transform.position = c + new Vector3(0.4f + shift, 0.45f, -dist); vcam.transform.LookAt(c + new Vector3(shift, -0.05f, 0)); vcam.fieldOfView = 34;
                if (ringMpb != null) { ringMpb.SetColor("_Color", new Color(1f, 0.74f, 0.34f, 0.5f + Mathf.Sin(Time.time * 2.2f) * 0.12f)); ringR.SetPropertyBlock(ringMpb); }
                if (lantern != null) lantern.intensity = 3.2f + Mathf.Sin(Time.time * 9f) * 0.25f + Mathf.Sin(Time.time * 4.3f) * 0.2f;
            }
            else { vcam.transform.position = c + new Vector3(0, 0.12f, -4.3f); vcam.transform.LookAt(c); vcam.fieldOfView = 25; }
            Lamp(c, -1);
        }
    }
}
