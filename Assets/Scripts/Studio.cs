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
        public RenderTexture Showcase(Look look, int w = 512, int h = 640)
        {
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
            vcam.transform.position = c + new Vector3(0, 0.12f, -4.3f); vcam.transform.LookAt(c); vcam.fieldOfView = 25;
            Lamp(c, -1);
        }
    }
}
