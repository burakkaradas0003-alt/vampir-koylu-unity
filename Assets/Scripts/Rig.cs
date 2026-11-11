// Karakter: tek iskelete bağlı parçalı model (beden, 3 yüz ifadesi, saç, şapka, gözlük, kıyafet...).
// Her oyuncu bir kombin giyer. Oyun her karede kumanda değerlerini ayarlar (kafa yönü, eğilme,
// parmakla gösterme, el sallama, mimik); LateUpdate bunları kemiklere uygular.
// Yön kuralı: karakter +Z'ye (ateşe) bakar, +X karakterin SAĞI, +Y yukarı.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace VK
{
    public class Face { public float smile, open, up, angry, sad, think, laugh, surprise, talk; }

    public class Rig : MonoBehaviour
    {
        public class Outfit { public string[] parts; public Color tint; public Outfit(float s, params string[] p) { parts = p; tint = s == 0 ? Color.white : Color.HSVToRGB(Mathf.Repeat(s, 1f), 0.35f, 1f); } }

        // shift: kıyafet renk paletinde kayma (atlas sütunları farklı renkler)
        public static readonly Outfit[] OUTFITS =
        {
            new Outfit(0f, "Hairstyle_male_010", "T_Shirt_009", "Pants_010", "Shoe_Sneakers_009"),
            new Outfit(0f, "Hairstyle_male_012", "Outerwear_029", "Pants_014", "Shoe_Slippers_002", "Glasses_004", "Moustache_001"),
            new Outfit(0f, "Hat_010", "Outerwear_036", "Shorts_003", "Socks_008", "Shoe_Slippers_005", "Glasses_006"),
            new Outfit(0f, "Costume_10_001", "Hat_049", "Clown_nose_001"),
            new Outfit(0.25f, "Hairstyle_male_010", "Outerwear_036", "Pants_010", "Shoe_Sneakers_009", "Moustache_002", "Gloves_006"),
            new Outfit(0.375f, "Hairstyle_male_012", "T_Shirt_009", "Shorts_003", "Socks_008", "Shoe_Slippers_002", "Headphones_002"),
            new Outfit(0f, "Costume_6_001", "Hat_057"),
            new Outfit(-0.125f, "Hat_049", "T_Shirt_009", "Pants_014", "Shoe_Sneakers_009"),
            new Outfit(0.5f, "Hairstyle_male_010", "Outerwear_029", "Pants_010", "Shoe_Slippers_005", "Glasses_006", "Gloves_014"),
        };
        static readonly string[] ALWAYS = { "Body_010", "Male_emotion_usual_001", "Male_emotion_happy_002", "Male_emotion_angry_003" };
        static readonly string[] KNOWN =
        {
            "Body_010", "Male_emotion_usual_001", "Male_emotion_happy_002", "Male_emotion_angry_003",
            "Hairstyle_male_010", "Hairstyle_male_012", "Hat_010", "Hat_049", "Hat_057", "Headphones_002", "Glasses_004", "Glasses_006",
            "Moustache_001", "Moustache_002", "T_Shirt_009", "Outerwear_029", "Outerwear_036", "Costume_10_001", "Costume_6_001",
            "Pants_010", "Pants_014", "Shorts_003", "Shoe_Sneakers_009", "Shoe_Slippers_002", "Shoe_Slippers_005", "Socks_008",
            "Gloves_006", "Gloves_014", "Clown_nose_001", "Pacifier_001",
        };
        static GameObject BASE; static Texture2D ATLAS;

        // ---- kumanda değerleri (oyun ayarlar)
        public float headYaw, headPitch, upperPitch, upperRoll;
        public float pointRaise, pointYaw;     // 0..1 kol kalkışı, hedef açısı (radyan, + sağ)
        public float wave, waveWiggle;         // 0..1 el sallama
        public Face face = new Face();
        public bool redEyes;
        public Transform crown;
        public GameObject body;                // ölünce gizlenir
        public float crownBase;
        public Material ghostMat;

        bool ghost, fp; float phase;
        float bThink, bLaugh, bSurp, bAngry, bSad, bTalk, bCross, bChin, idleNext; int idleKind;
        // ağırlıklı yön karışımı: temel + (ağırlık, yön) çiftleri
        static Vector3 Blend(float time, Vector3 basis, params (float w, Vector3 d)[] parts)
        {
            float tw = 0; var v = Vector3.zero;
            foreach (var p in parts) { float w = Mathf.Clamp01(p.w); v += p.d.normalized * w; tw += w; }
            v += basis.normalized * Mathf.Max(0, 1 - tw);
            return v.sqrMagnitude < 1e-6f ? basis.normalized : v.normalized;
        }
        public float reach = 0.5f; // birinci şahıs: omuzdan işaret parmağına uzunluk
        readonly Dictionary<string, Transform> B = new Dictionary<string, Transform>();
        readonly List<KeyValuePair<Transform, Quaternion>> rest = new List<KeyValuePair<Transform, Quaternion>>();
        readonly Dictionary<Transform, Vector3> curlAxis = new Dictionary<Transform, Vector3>();
        readonly Dictionary<string, List<Renderer>> faces = new Dictionary<string, List<Renderer>>();
        string curFace = "";
        Transform eyes;

        static string Nm(string n) { int k = n.LastIndexOf(':'); return k >= 0 ? n.Substring(k + 1) : n; }

        public Look look;
        public static Rig Make(Look look, float size, bool ghost = false, bool fp = false, bool stool = true)
        {
            if (BASE == null) { BASE = Resources.Load<GameObject>("Character/Creative_Character_free"); ATLAS = Resources.Load<Texture2D>("Character/Textures_4"); }
            var root = new GameObject(ghost ? "Hayalet" : fp ? "Kolum" : "Köylü");
            var rig = root.AddComponent<Rig>();
            rig.ghost = ghost; rig.fp = fp; rig.stool = stool; rig.look = (look ?? new Look()).Clone();
            rig.Setup(rig.look, size);
            return rig;
        }
        bool stool = true;

        void Setup(Look lk, float size)
        {
            Color skinTint = Look.SKINS[Mathf.Clamp(lk.skin, 0, Look.SKINS.Length - 1)];
            Color topT = Look.TINTS[Mathf.Clamp(lk.topColor, 0, Look.TINTS.Length - 1)], botT = Look.TINTS[Mathf.Clamp(lk.bottomColor, 0, Look.TINTS.Length - 1)], hatT = Look.TINTS[Mathf.Clamp(lk.hatColor, 0, Look.TINTS.Length - 1)];
            phase = Random.value * 6.28f;
            bool sy = !string.IsNullOrEmpty(lk.synty) && SyntyBase() != null;
            var syVis = new List<Renderer>();
            var model = sy ? SyntyBuild(lk, syVis) : Instantiate(BASE, transform); model.name = "model"; body = model;
            foreach (var t in model.GetComponentsInChildren<Transform>()) { var n = Nm(t.name); if (!B.ContainsKey(n)) B[n] = t; }
            if (sy) SyntyAlias(); isSynty = sy;

            // malzemeler
            var lit = Resources.Load<Material>("Mats/Lit"); var matte = Resources.Load<Material>("Mats/LitMatte") ?? lit;
            Material Cloth(float shift) { var m = new Material(matte); m.SetTexture("_BaseMap", ATLAS); m.SetTextureOffset("_BaseMap", new Vector2(shift, 0)); m.SetFloat("_Smoothness", 0.14f); return m; }
            var skin = Cloth(0); skin.SetColor("_BaseColor", skinTint);
            var cloth = Cloth(0); cloth.SetColor("_BaseColor", topT); var plain = Cloth(0);
            var clothB = Cloth(0); clothB.SetColor("_BaseColor", botT); var clothH = Cloth(0); clothH.SetColor("_BaseColor", hatT);
            var glass = new Material(lit); glass.SetColor("_BaseColor", new Color(0.08f, 0.12f, 0.16f)); glass.SetFloat("_Smoothness", 0.9f);
            if (ghost && ghostMat == null) ghostMat = new Material(Resources.Load<Material>("Mats/Ghost"));

            var show = new HashSet<string>(ALWAYS); foreach (var p in lk.Parts()) show.Add(p);
            var known = new HashSet<string>(KNOWN);
            var visible = new List<Renderer>();
            if (sy) visible.AddRange(syVis);
            else foreach (var r in model.GetComponentsInChildren<Renderer>(true))
            {
                string key = Nm(r.name);
                if (!known.Contains(key) && r.transform.parent != null && known.Contains(Nm(r.transform.parent.name))) key = Nm(r.transform.parent.name);
                bool on = show.Contains(key);
                r.enabled = on;
                if (r is SkinnedMeshRenderer smr) smr.updateWhenOffscreen = true;
                if (!on) continue;
                bool isSkin = key == "Body_010" || key.Contains("emotion");
                var baseM = isSkin ? skin : (key.Contains("Hair") || key.Contains("Moustache") || key.Contains("Clown") || key.Contains("Pacifier")) ? plain
                    : (key.StartsWith("Pants") || key.StartsWith("Shorts") || key.StartsWith("Socks")) ? clothB : key.StartsWith("Hat") ? clothH : (key.StartsWith("Shoe") || key.StartsWith("Glasses") || key.StartsWith("Gloves") || key.StartsWith("Headphones")) ? plain : cloth;
                var arr = new Material[Mathf.Max(1, r.sharedMaterials.Length)];
                for (int i = 0; i < arr.Length; i++) arr[i] = ghost ? ghostMat : (key.StartsWith("Glasses") && i == arr.Length - 1 && arr.Length > 1) ? glass : baseM;
                r.sharedMaterials = arr;
                r.shadowCastingMode = ghost || fp ? ShadowCastingMode.Off : ShadowCastingMode.On;
                if (key.Contains("emotion")) { string f = key.Contains("happy") ? "happy" : key.Contains("angry") ? "angry" : "usual"; if (!faces.ContainsKey(f)) faces[f] = new List<Renderer>(); faces[f].Add(r); }
                visible.Add(r);
            }
            SetFace("usual");

            // yön ve boy: karakter +Z'ye baksın, kafa kemiği 1.49 m (x boy çarpanı)
            model.transform.localRotation = Quaternion.identity;
            if (B.ContainsKey("LeftToeBase") && B.ContainsKey("LeftFoot") && B["LeftToeBase"].position.z < B["LeftFoot"].position.z) model.transform.localRotation = Quaternion.Euler(0, 180, 0);
            float headH = B["Head"].position.y - B["LeftToeBase"].position.y;
            if (headH > 0.01f) model.transform.localScale *= 1.49f * size / headH;
            foreach (var t in model.GetComponentsInChildren<Transform>()) if (t != model.transform) rest.Add(new KeyValuePair<Transform, Quaternion>(t, t.localRotation));
            ComputeCurlAxes(); if (sy) SyntyLog();

            // kafaya bağlı çerçeve: göz parıltısı ve taç
            var hb = new GameObject("kafa çerçevesi").transform; hb.SetParent(B["Head"], false);
            hb.rotation = transform.rotation; hb.localScale = new Vector3(1 / B["Head"].lossyScale.x, 1 / B["Head"].lossyScale.y, 1 / B["Head"].lossyScale.z);
            float sz = size;
            var eyeM = new Material(Resources.Load<Material>("Mats/Unlit")); eyeM.SetColor("_BaseColor", new Color(4f, 0.25f, 0.3f));
            eyes = new GameObject("gözler").transform; eyes.SetParent(hb, false); eyes.gameObject.SetActive(false);
            foreach (float s in new[] { -1f, 1f })
            {
                var e = GameObject.CreatePrimitive(PrimitiveType.Sphere); Destroy(e.GetComponent<Collider>());
                e.transform.SetParent(eyes, false); e.transform.localPosition = new Vector3(s * 0.045f, 0.105f, 0.125f) * sz; e.transform.localScale = Vector3.one * 0.028f * sz;
                e.GetComponent<MeshRenderer>().sharedMaterial = eyeM; e.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            }
            // başın (ya da şapkanın) üstü
            float top = B["Head"].position.y + 0.25f * sz;
            foreach (var r in visible) { var b = r.bounds; if (b.min.y > B["Neck"].position.y - 0.02f) top = Mathf.Max(top, b.max.y); }
            var ch = new GameObject("taç yeri").transform; ch.SetParent(hb, false);
            ch.position = new Vector3(B["Head"].position.x, top - 0.02f, B["Head"].position.z);
            crown = BuildCrown(ch); crown.gameObject.SetActive(false);
            if (!ghost && !fp) BuildAccessories(hb, ch.localPosition, sz, lk);

            if (fp) { MakeFirstPerson(visible); return; }

            Pose(0);
            // ayak tabanı yere, kalça tabureye
            if (ghost)
            {
                model.transform.position += Vector3.down * (B["LeftToeBase"].position.y - transform.position.y - 0.02f);
            }
            else if (!stool)
            {
                float low0 = Mathf.Min(B["LeftToeBase"].position.y, B["RightToeBase"].position.y, B["LeftFoot"].position.y - 0.06f, B["RightFoot"].position.y - 0.06f);
                model.transform.position += Vector3.down * (low0 - transform.position.y - 0.02f);
                Vector3 hips0 = transform.InverseTransformPoint(B["Hips"].position);
                model.transform.localPosition -= new Vector3(hips0.x, 0, hips0.z);
                seatHeight = Mathf.Max(0.2f, hips0.y - 0.08f);
            }
            else
            {
                float low = Mathf.Min(B["LeftToeBase"].position.y, B["RightToeBase"].position.y, B["LeftFoot"].position.y - 0.06f, B["RightFoot"].position.y - 0.06f);
                low = MeshBottom(visible, low); // ayak tabanı tam yere bassın (kemik değil, gerçek mesh altı)
                model.transform.position += Vector3.down * (low - transform.position.y - 0.02f);
                Vector3 hips = transform.InverseTransformPoint(B["Hips"].position);
                float seatH = Mathf.Max(0.2f, hips.y - 0.08f);
                model.transform.localPosition -= new Vector3(0, 0, hips.z + 0.02f);
                var lit2 = new Material(lit); lit2.SetColor("_BaseColor", new Color(0.42f, 0.29f, 0.18f));
                if (!MakeChair(seatH))
                {
                var seat = GameObject.CreatePrimitive(PrimitiveType.Cylinder); Destroy(seat.GetComponent<Collider>());
                seat.transform.SetParent(transform, false); seat.transform.localPosition = new Vector3(0, seatH - 0.035f, 0); seat.transform.localScale = new Vector3(0.5f, 0.035f, 0.5f);
                seat.GetComponent<MeshRenderer>().sharedMaterial = lit2;
                for (int i = 0; i < 3; i++)
                {
                    float a = i * 2.094f + 0.5f; var leg = GameObject.CreatePrimitive(PrimitiveType.Cube); Destroy(leg.GetComponent<Collider>());
                    leg.transform.SetParent(transform, false); leg.transform.localPosition = new Vector3(Mathf.Cos(a) * 0.15f, (seatH - 0.06f) / 2, Mathf.Sin(a) * 0.15f);
                    leg.transform.localScale = new Vector3(0.06f, seatH - 0.06f, 0.06f); leg.transform.localEulerAngles = new Vector3(-Mathf.Sin(a) * 7, 0, Mathf.Cos(a) * 7);
                    leg.GetComponent<MeshRenderer>().sharedMaterial = lit2;
                }
                }
                var hit = gameObject.AddComponent<BoxCollider>(); hit.center = new Vector3(0, 0.75f, 0.1f); hit.size = new Vector3(0.8f, 1.5f, 0.9f);
            }
        }

        Transform BuildCrown(Transform parent)
        {
            var gold = new Material(Resources.Load<Material>("Mats/LitEmissive")); gold.SetColor("_BaseColor", new Color(0.96f, 0.77f, 0.26f));
            gold.SetFloat("_Metallic", 0.8f); gold.SetFloat("_Smoothness", 0.7f); gold.SetColor("_EmissionColor", new Color(0.79f, 0.56f, 0.1f) * 0.6f);
            var c = new GameObject("taç").transform; c.SetParent(parent, false);
            var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder); Destroy(ring.GetComponent<Collider>());
            ring.transform.SetParent(c, false); ring.transform.localScale = new Vector3(0.24f, 0.033f, 0.24f); ring.GetComponent<MeshRenderer>().sharedMaterial = gold;
            for (int i = 0; i < 6; i++)
            {
                float a = i / 6f * Mathf.PI * 2; var sp = GameObject.CreatePrimitive(PrimitiveType.Cube); Destroy(sp.GetComponent<Collider>());
                sp.transform.SetParent(c, false); sp.transform.localPosition = new Vector3(Mathf.Cos(a) * 0.11f, 0.06f, Mathf.Sin(a) * 0.11f);
                sp.transform.localScale = new Vector3(0.035f, 0.07f, 0.035f); sp.transform.localEulerAngles = new Vector3(0, -a * Mathf.Rad2Deg + 45, 0);
                sp.GetComponent<MeshRenderer>().sharedMaterial = gold;
            }
            return c;
        }

        // ---------------------------------------------------------------- kozmetik: maske, kafa aksesuarı, efekt, zafer pozu
        public float cheer;            // 0..1 zafer pozu ağırlığı
        public bool preview;           // vitrin/portre karakteri: efekt parçacığı üretmez
        public bool hang;              // darağacında: ayakta sarkık poz
        public bool limp;              // ip gerildikten sonra baş düşer
        public Transform Bone(string n) => B.TryGetValue(n, out var t) ? t : transform;
        public float seatHeight;       // taburesiz (ayakta değil, gösterim) kalça yüksekliği
        readonly List<GameObject> acc = new List<GameObject>();
        GameObject AccPrim(PrimitiveType t, Transform parent, Material m, Vector3 pos, Vector3 scale, Vector3 rot = default)
        {
            var g = GameObject.CreatePrimitive(t); Destroy(g.GetComponent<Collider>());
            g.transform.SetParent(parent, false); g.transform.localPosition = pos; g.transform.localScale = scale; g.transform.localEulerAngles = rot;
            var mr = g.GetComponent<MeshRenderer>(); mr.sharedMaterial = m; mr.shadowCastingMode = ShadowCastingMode.Off; acc.Add(g); return g;
        }
        static Mesh coneMesh, torusMesh;
        static Mesh ConeM()
        {
            if (coneMesh != null) return coneMesh;
            var v = new List<Vector3>(); var tr = new List<int>(); int seg = 10;
            for (int i = 0; i < seg; i++)
            {
                float a0 = i * Mathf.PI * 2 / seg, a1 = (i + 1) * Mathf.PI * 2 / seg; int k = v.Count;
                v.Add(new Vector3(Mathf.Cos(a0) * 0.5f, 0, Mathf.Sin(a0) * 0.5f)); v.Add(new Vector3(0, 1, 0)); v.Add(new Vector3(Mathf.Cos(a1) * 0.5f, 0, Mathf.Sin(a1) * 0.5f));
                tr.Add(k); tr.Add(k + 1); tr.Add(k + 2);
            }
            coneMesh = new Mesh(); coneMesh.SetVertices(v); coneMesh.SetTriangles(tr, 0); coneMesh.RecalculateNormals(); coneMesh.RecalculateBounds(); return coneMesh;
        }
        static Mesh TorusM()
        {
            if (torusMesh != null) return torusMesh;
            int a = 28, b = 8; float R = 0.5f, r = 0.07f; var v = new List<Vector3>(); var tr = new List<int>();
            for (int i = 0; i <= a; i++) for (int j = 0; j <= b; j++)
                {
                    float u = i / (float)a * Mathf.PI * 2, w = j / (float)b * Mathf.PI * 2;
                    v.Add(new Vector3((R + r * Mathf.Cos(w)) * Mathf.Cos(u), r * Mathf.Sin(w), (R + r * Mathf.Cos(w)) * Mathf.Sin(u)));
                }
            for (int i = 0; i < a; i++) for (int j = 0; j < b; j++)
                {
                    int k = i * (b + 1) + j, n = (i + 1) * (b + 1) + j;
                    tr.Add(k); tr.Add(k + 1); tr.Add(n); tr.Add(n); tr.Add(k + 1); tr.Add(n + 1);
                }
            torusMesh = new Mesh(); torusMesh.SetVertices(v); torusMesh.SetTriangles(tr, 0); torusMesh.RecalculateNormals(); torusMesh.RecalculateBounds(); return torusMesh;
        }
        GameObject AccMesh(Mesh mesh, Transform parent, Material m, Vector3 pos, Vector3 scale, Vector3 rot = default)
        {
            var g = new GameObject("aks"); g.transform.SetParent(parent, false); g.transform.localPosition = pos; g.transform.localScale = scale; g.transform.localEulerAngles = rot;
            g.AddComponent<MeshFilter>().sharedMesh = mesh; var mr = g.AddComponent<MeshRenderer>(); mr.sharedMaterial = m; mr.shadowCastingMode = ShadowCastingMode.Off; acc.Add(g); return g;
        }
        void BuildAccessories(Transform hb, Vector3 topLocal, float sz, Look lk)
        {
            var lit = Resources.Load<Material>("Mats/Lit");
            Material M(Color c, float sm = 0.35f, float em = 0) { var m = new Material(em > 0 ? Resources.Load<Material>("Mats/LitEmissive") : lit); m.SetColor("_BaseColor", c); m.SetFloat("_Smoothness", sm); if (em > 0) m.SetColor("_EmissionColor", c * em); return m; }
            var mi = Catalog.Get(lk.mask);
            if (mi != null && string.IsNullOrEmpty(lk.costume))
            {
                var mm = M(mi.tint, 0.6f); var hole = M(new Color(0.03f, 0.03f, 0.05f), 0.2f);
                AccPrim(PrimitiveType.Cube, hb, mm, new Vector3(0, 0.108f, 0.118f) * sz, new Vector3(0.215f, 0.06f, 0.035f) * sz);
                foreach (float x in new[] { -1f, 1f })
                {
                    AccPrim(PrimitiveType.Cube, hb, mm, new Vector3(x * 0.112f, 0.118f, 0.1f) * sz, new Vector3(0.04f, 0.07f, 0.06f) * sz, new Vector3(0, 0, x * 18));
                    AccPrim(PrimitiveType.Sphere, hb, hole, new Vector3(x * 0.046f, 0.108f, 0.137f) * sz, new Vector3(0.042f, 0.03f, 0.01f) * sz);
                }
            }
            var hi = Catalog.Get(lk.head);
            if (hi != null)
            {
                var top = topLocal;
                switch (hi.id)
                {
                    case "head_halo": AccMesh(TorusM(), hb, M(hi.tint, 0.8f, 1.6f), top + new Vector3(0, 0.12f, 0) * sz, Vector3.one * 0.26f * sz); break;
                    case "head_horns":
                        foreach (float x in new[] { -1f, 1f }) AccMesh(ConeM(), hb, M(hi.tint, 0.5f), top + new Vector3(x * 0.075f, -0.045f, 0.02f) * sz, new Vector3(0.05f, 0.11f, 0.05f) * sz, new Vector3(0, 0, -x * 22));
                        break;
                    case "head_cat":
                        foreach (float x in new[] { -1f, 1f })
                        {
                            AccMesh(ConeM(), hb, M(hi.tint, 0.3f), top + new Vector3(x * 0.075f, -0.05f, 0) * sz, new Vector3(0.085f, 0.1f, 0.035f) * sz, new Vector3(0, 0, -x * 16));
                            AccMesh(ConeM(), hb, M(new Color(1f, 0.7f, 0.78f), 0.3f), top + new Vector3(x * 0.075f, -0.042f, 0.012f) * sz, new Vector3(0.05f, 0.07f, 0.02f) * sz, new Vector3(0, 0, -x * 16));
                        }
                        break;
                    case "head_bunny":
                        foreach (float x in new[] { -1f, 1f }) AccPrim(PrimitiveType.Capsule, hb, M(hi.tint, 0.3f), top + new Vector3(x * 0.05f, 0.08f, -0.01f) * sz, new Vector3(0.045f, 0.1f, 0.022f) * sz, new Vector3(0, 0, -x * 12));
                        break;
                }
            }
        }

        // zafer pozları: kolların mevcut yönünü hedefe doğru karıştırır
        void Victory(float time, float k)
        {
            string kind = look != null && !string.IsNullOrEmpty(look.victory) ? look.victory : "vic_cheer";
            Vector3 uL, fL; float s = Mathf.Sin(time * 7f), s2 = Mathf.Sin(time * 12f);
            switch (kind)
            {
                case "vic_clap": uL = new Vector3(-0.2f, -0.35f, 1); fL = new Vector3(0.62f + s2 * 0.22f, 0.25f, 0.7f); break;
                case "vic_flex": uL = new Vector3(-1, 0.08f, 0.1f); fL = new Vector3(-0.1f, 1, 0.2f + s * 0.06f); break;
                case "vic_wave": uL = new Vector3(-0.45f, 0.95f, 0.15f); fL = new Vector3(-0.15f - s * 0.5f, 1, 0.15f); break;
                default: uL = new Vector3(-0.42f, 1, 0.08f + s * 0.05f); fL = new Vector3(-0.28f, 1, 0.05f); break;
            }
            bool bothSides = kind != "vic_wave";
            void Seg(string a, string b, Vector3 dirLocal)
            {
                if (!B.TryGetValue(a, out var ta) || !B.TryGetValue(b, out var tb)) return;
                var cur = (tb.position - ta.position).normalized; var want = rootQ * dirLocal.normalized;
                Aim(a, b, Vector3.Slerp(cur, want, k));
            }
            Seg("LeftArm", "LeftForeArm", uL); Seg("LeftForeArm", "LeftHand", fL);
            if (bothSides) { Seg("RightArm", "RightForeArm", new Vector3(-uL.x, uL.y, uL.z)); Seg("RightForeArm", "RightHand", new Vector3(-fL.x, fL.y, fL.z)); }
        }

        // efekt: karakterin çevresinde küçük parıltılar (kozmetik, oyuna etkisi yok)
        float fxAcc;
        void Update() { try { UpdateInner(); } catch (System.Exception e) { ZuLog.Err("Rig.Update", e); } }
        void UpdateInner()
        {
            if (preview || ghost || fp || look == null || string.IsNullOrEmpty(look.effect) || World.I == null || body == null || !body.activeInHierarchy) return;
            var it = Catalog.Get(look.effect); if (it == null) return;
            fxAcc += Time.deltaTime * (World.I.level >= 3 ? 2.5f : 5f);
            while (fxAcc > 1)
            {
                fxAcc--;
                float a = Random.value * 6.283f, r = 0.35f + Random.value * 0.3f;
                var p = transform.position + new Vector3(Mathf.Cos(a) * r, 0.3f + Random.value * 1.2f, Mathf.Sin(a) * r);
                var c = it.tint; if (it.id == "fx_bats") c = Random.value < 0.5f ? new Color(0.55f, 0.25f, 1f) : new Color(0.25f, 0.1f, 0.45f);
                World.I.Emit(p, new Vector3((Random.value - 0.5f) * 0.1f, 0.25f + Random.value * 0.25f, (Random.value - 0.5f) * 0.1f), 1.4f + Random.value, 0.07f, 0.02f, c, 0, 0.9f);
            }
        }

        // kullanıcının rustik sandalyesi: oturma yüzeyi kalça yüksekliğine gelecek şekilde ölçeklenir, sırtlık arkada
        static float chairYaw = float.NaN; static Mesh chairMesh; static Quaternion chairRot; static Vector3 chairScl; static Material chairMat; static bool chairTried;
        bool MakeChair(float seatH)
        {
            if (!chairTried)
            {
                chairTried = true;
                var src = Resources.Load<GameObject>("Chair/Chair");
                if (src != null) foreach (var f in src.GetComponentsInChildren<MeshFilter>(true)) { chairMesh = f.sharedMesh; chairRot = f.transform.rotation; chairScl = f.transform.lossyScale; break; }
                if (chairMesh != null)
                {
                    chairMat = new Material(Resources.Load<Material>("Mats/LitNormal"));
                    chairMat.SetTexture("_BaseMap", Resources.Load<Texture2D>("Chair/chair_base")); chairMat.SetColor("_BaseColor", new Color(1.15f, 1.08f, 1f));
                    chairMat.SetTexture("_BumpMap", Resources.Load<Texture2D>("Chair/chair_normal")); chairMat.SetFloat("_BumpScale", 1f); chairMat.SetFloat("_Smoothness", 0.12f);
                    chairMat.SetColor("_EmissionColor", Color.black); chairMat.SetTexture("_OcclusionMap", null);
                }
            }
            if (chairMesh == null) return false;
            // modelin tabanı ve yüksekliği (dönmüş/ölçekli sınırlar)
            var b = chairMesh.bounds; var tr = Matrix4x4.TRS(Vector3.zero, chairRot, chairScl);
            Vector3 mn = Vector3.one * 1e9f, mx = Vector3.one * -1e9f;
            for (int i = 0; i < 8; i++)
            {
                var c = b.center + Vector3.Scale(b.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                var p = tr.MultiplyPoint3x4(c); mn = Vector3.Min(mn, p); mx = Vector3.Max(mx, p);
            }
            float h = mx.y - mn.y; if (h < 1e-4f) return false;
            float k = 2f * seatH / h; // oturak, toplam yüksekliğin yarısında
            // sırtlığın yönünü modelden bul: üst kısmın köşeleri sırtlıktır; sırtlık karakterin arkasına (-Z) gelecek şekilde döndür
            if (float.IsNaN(chairYaw))
            {
                chairYaw = 180;
                if (chairMesh.isReadable)
                {
                    var vs = chairMesh.vertices; Vector3 sum = Vector3.zero, all = Vector3.zero; int n = 0;
                    float cut = mn.y + (mx.y - mn.y) * 0.72f;
                    foreach (var v0 in vs) { var p = tr.MultiplyPoint3x4(v0); all += p; if (p.y > cut) { sum += p; n++; } }
                    if (n > 10)
                    {
                        var c0 = all / vs.Length; var d = sum / n - c0; d.y = 0;
                        if (d.sqrMagnitude > 1e-6f) chairYaw = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg; // sırtlığın şu anki yönü
                        chairYaw = 180 - chairYaw; // onu -Z'ye çevir
                        // en yakın 90°'ye oturt (model eksenleri düzgünse)
                        float snap = Mathf.Round(chairYaw / 90f) * 90f; if (Mathf.Abs(Mathf.DeltaAngle(snap, chairYaw)) < 20) chairYaw = snap;
                    }
                }
            }
            var g = new GameObject("sandalye"); g.transform.SetParent(transform, false); g.transform.localPosition = new Vector3(0, 0, -0.04f); g.transform.localScale = Vector3.one * k; g.transform.localRotation = Quaternion.Euler(0, chairYaw, 0); // sırtlık arkada, oturak öne bakar
            var m = new GameObject("model"); m.transform.SetParent(g.transform, false); m.transform.localRotation = chairRot; m.transform.localScale = chairScl;
            m.transform.localPosition = -new Vector3((mn.x + mx.x) / 2, mn.y, (mn.z + mx.z) / 2);
            m.AddComponent<MeshFilter>().sharedMesh = chairMesh; var r = m.AddComponent<MeshRenderer>(); r.sharedMaterial = chairMat;
            return true;
        }

        // ---------------------------------------------------------------- Synty Fantasy Hero (deneme): parçalı karakter
        static GameObject SBASE; static bool sTried; static readonly Dictionary<string, Material> sMats = new Dictionary<string, Material>();
        static Transform sRoot; static Dictionary<string, SkinnedMeshRenderer> sParts;
        public static bool SyntyAvailable => SyntyBase() != null;
        static GameObject SyntyBase()
        {
            if (sTried) return SBASE; sTried = true;
            SBASE = Resources.Load<GameObject>("Synty/ModularCharacters");
            if (SBASE == null) return null;
            sParts = new Dictionary<string, SkinnedMeshRenderer>();
            foreach (var r in SBASE.GetComponentsInChildren<SkinnedMeshRenderer>(true)) if (!sParts.ContainsKey(r.name)) sParts[r.name] = r;
            foreach (var t in SBASE.GetComponentsInChildren<Transform>(true)) if (t.name == "Root") { sRoot = t; break; }
            if (sRoot == null || sParts.Count == 0) SBASE = null;
            return SBASE;
        }
        static string PathOf(Transform t, Transform root)
        {
            if (t == null) return null; if (t == root) return "";
            var s = t.name; t = t.parent;
            while (t != null && t != root) { s = t.name + "/" + s; t = t.parent; }
            return s;
        }
        static void CopyLocal(Transform dst, Transform src) { dst.localPosition = src.localPosition; dst.localRotation = src.localRotation; dst.localScale = src.localScale; }
        // Synty kemik adları -> bu karakter sisteminin kullandığı adlar
        void SyntyAlias()
        {
            void A(string ours, string theirs) { if (B.TryGetValue(theirs, out var t)) B[ours] = t; }
            A("Spine", "Spine_01"); A("Spine1", "Spine_02");
            foreach (var (s, x) in new[] { ("Left", "_L"), ("Right", "_R") })
            {
                A(s + "Shoulder", "Clavicle" + x); A(s + "Arm", "Shoulder" + x); A(s + "ForeArm", "Elbow" + x); A(s + "Hand", "Hand" + x);
                A(s + "UpLeg", "UpperLeg" + x); A(s + "Leg", "LowerLeg" + x); A(s + "Foot", "Ankle" + x); A(s + "ToeBase", "Ball" + x);
                if (!B.TryGetValue("Hand" + x, out var hand)) continue;
                foreach (var t in hand.GetComponentsInChildren<Transform>())
                {
                    var n = Nm(t.name);
                    for (int k = 1; k <= 4; k++)
                    {
                        if (n == "IndexFinger_0" + k) B[s + "HandIndex" + k] = t;
                        if (n == "Finger_0" + k) B[s + "HandMiddle" + k] = t;
                        if (n == "Thumb_0" + k) B[s + "HandThumb" + k] = t;
                    }
                }
            }
        }
        static bool sLogged;
        void SyntyLog()
        {
            if (sLogged) return; sLogged = true;
            var ks = new[] { "RightArm", "RightForeArm", "RightHand", "RightHandIndex1", "RightHandIndex2", "RightHandMiddle1", "RightHandThumb1" };
            Debug.Log("[Zuplay] synty kemik: " + string.Join(" ", System.Array.ConvertAll(ks, k => k + "=" + (B.TryGetValue(k, out var t) ? t.name : "-") + (t != null && curlAxis.ContainsKey(t) ? "*" : ""))));
        }
        // hazır kombinler: gövde, kol, el, kalça, bacak aynı numaralı takım; baş, saç, kaş, sakal ayrı
        public static readonly string[] SYNTY_PRESETS = { "m3", "m13", "m6", "m8", "m27", "m14", "m26", "m4", "f5", "f13", "f28", "f7" };
        static readonly int[] HEADS = { 1, 2, 4, 5, 6, 7, 8, 9, 3, 10, 11, 12 }, HAIRS = { 5, 12, 2, 9, 14, 3, 16, 18, 24, 31, 27, 21 }, FACIAL = { 2, 5, 0, 8, 0, 3, 10, 0, 0, 0, 0, 0 };
        static readonly string[] TEX = { "01_A", "02_A", "03_A", "04_A" };
        // yalnız iskelet + seçili parçalar kurulur (720 parçalı modelin tamamı kopyalanmaz)
        GameObject SyntyBuild(Look lk, List<Renderer> visible)
        {
            if (ghost && ghostMat == null) ghostMat = new Material(Resources.Load<Material>("Mats/Ghost"));
            var model = new GameObject("model"); model.transform.SetParent(transform, false);
            var skel = Instantiate(sRoot.gameObject, model.transform); skel.name = "Root"; CopyLocal(skel.transform, sRoot);
            var map = new Dictionary<string, Transform>();
            foreach (var t in skel.GetComponentsInChildren<Transform>(true)) { var pth = PathOf(t, skel.transform); if (!map.ContainsKey(pth)) map[pth] = t; }
            int pi = System.Array.IndexOf(SYNTY_PRESETS, lk.synty); if (pi < 0) pi = 0;
            string g = lk.synty.StartsWith("f") ? "Female" : "Male"; int o = 3; int.TryParse(lk.synty.Substring(1), out o);
            string N(string part, int idx, int max) => "Chr_" + part + "_" + g + "_" + Mathf.Clamp(idx, 0, max).ToString("00");
            var want = new List<string>
            {
                N("Torso", o, 28), N("Hips", o, 28), N("ArmUpperRight", o, 20), N("ArmUpperLeft", o, 20), N("ArmLowerRight", o, 18), N("ArmLowerLeft", o, 18),
                N("HandRight", o, 17), N("HandLeft", o, 17), N("LegRight", o, 19), N("LegLeft", o, 19), N("Head", HEADS[pi], 22),
                "Chr_Hair_" + HAIRS[pi].ToString("00"), "Chr_Eyebrow_" + g + "_" + (1 + pi % (g == "Male" ? 10 : 7)).ToString("00"),
            };
            if (g == "Male" && FACIAL[pi] > 0) want.Add("Chr_FacialHair_Male_" + FACIAL[pi].ToString("00"));
            string tk = TEX[pi % TEX.Length];
            if (!sMats.TryGetValue(tk, out var mat))
            {
                mat = new Material(Resources.Load<Material>("Mats/LitMatte") ?? Resources.Load<Material>("Mats/Lit")); mat.SetTexture("_BaseMap", Resources.Load<Texture2D>("Synty/Tex_" + tk)); mat.SetColor("_BaseColor", new Color(0.96f, 0.95f, 0.93f)); mat.SetFloat("_Smoothness", 0.12f);
                sMats[tk] = mat;
            }
            foreach (var name in want)
            {
                if (!sParts.TryGetValue(name, out var src) || src.sharedMesh == null) continue;
                var go = new GameObject(name); go.transform.SetParent(model.transform, false); CopyLocal(go.transform, src.transform);
                var smr = go.AddComponent<SkinnedMeshRenderer>();
                var bones = new Transform[src.bones.Length];
                for (int i = 0; i < bones.Length; i++) { var pth = PathOf(src.bones[i], sRoot); bones[i] = pth != null && map.TryGetValue(pth, out var bt) ? bt : null; }
                smr.sharedMesh = src.sharedMesh; smr.bones = bones;
                var rp = PathOf(src.rootBone, sRoot); smr.rootBone = rp != null && map.TryGetValue(rp, out var rb) ? rb : skel.transform;
                smr.localBounds = src.localBounds; smr.updateWhenOffscreen = true;
                var arr = new Material[Mathf.Max(1, src.sharedMaterials.Length)]; for (int i = 0; i < arr.Length; i++) arr[i] = ghost ? ghostMat : mat;
                smr.sharedMaterials = arr; smr.shadowCastingMode = ghost || fp ? ShadowCastingMode.Off : ShadowCastingMode.On;
                visible.Add(smr);
            }
            return model;
        }

        // şu anki pozda ayakkabı tabanının yüksekliği (darağacında ayağı tahtaya oturtmak için)
        public float FeetY()
        {
            var l = new List<Renderer>(); foreach (var r in GetComponentsInChildren<SkinnedMeshRenderer>()) if (r.enabled && r.gameObject.activeInHierarchy) l.Add(r);
            float fb = Mathf.Min(B["LeftToeBase"].position.y, B["RightToeBase"].position.y) - 0.03f;
            return MeshBottom(l, fb);
        }
        // pozlanmış mesh'in en alt noktası (ayakkabı tabanı); bulunamazsa kemik tahmini
        static Mesh bakeTmp;
        float MeshBottom(List<Renderer> vis, float fallback)
        {
            float best = float.MaxValue; if (bakeTmp == null) bakeTmp = new Mesh();
            foreach (var r in vis)
            {
                if (!(r is SkinnedMeshRenderer smr) || smr.sharedMesh == null || !r.enabled) continue;
                try
                {
                    smr.BakeMesh(bakeTmp, true);
                    var m = Matrix4x4.TRS(smr.transform.position, smr.transform.rotation, Vector3.one);
                    var v = bakeTmp.vertices;
                    for (int i = 0; i < v.Length; i++) { float y = m.MultiplyPoint3x4(v[i]).y; if (y < best) best = y; }
                }
                catch { }
            }
            return best < float.MaxValue && best > fallback - 0.4f ? best : fallback;
        }

        // oturan karakterin elleri tam dizlerine: iki kemikli ters kinematik (omuz-dirsek-el), dirsek hafif dışa ve geriye
        Vector3 kUpL, kFoL, kUpR, kFoR;
        void KneeIK(string side, float sx, out Vector3 upLocal, out Vector3 foLocal)
        {
            upLocal = new Vector3(0.12f * sx, -0.8f, 0.62f); foLocal = new Vector3(-0.02f * sx, -0.6f, 0.82f);
            if (!B.TryGetValue(side + "Arm", out var sh) || !B.TryGetValue(side + "ForeArm", out var el) || !B.TryGetValue(side + "Hand", out var ha) || !B.TryGetValue(side + "Leg", out var knee)) return;
            float a = Vector3.Distance(sh.position, el.position), b = Vector3.Distance(el.position, ha.position);
            if (a < 1e-4f || b < 1e-4f) return;
            // el bileği dizin biraz üstünde ve önünde: avuç dizin üstüne gelir
            var target = knee.position + rootQ * new Vector3(-0.02f * sx, 0.07f, -0.04f) * (a + b) / 0.55f;
            var S = sh.position; var to = target - S; float d = Mathf.Clamp(to.magnitude, Mathf.Abs(a - b) + 0.01f, a + b - 0.002f);
            var dir = to.normalized;
            float cosA = Mathf.Clamp((a * a + d * d - b * b) / (2 * a * d), -1, 1); float x = a * cosA, h = a * Mathf.Sqrt(Mathf.Max(0, 1 - cosA * cosA));
            var hint = rootQ * new Vector3(sx * 0.8f, 0.1f, -0.6f); var perp = (hint - dir * Vector3.Dot(hint, dir)); if (perp.sqrMagnitude < 1e-6f) perp = rootQ * Vector3.right * sx; perp.Normalize();
            var elbow = S + dir * x + perp * h; var wrist = S + dir * d;
            var inv = Quaternion.Inverse(rootQ);
            upLocal = inv * (elbow - S).normalized; foLocal = inv * (wrist - elbow).normalized;
        }

        void ComputeCurlAxes()
        {
            foreach (var side in new[] { "Left", "Right" })
            {
                if (!B.ContainsKey(side + "Hand") || !B.ContainsKey(side + "HandMiddle1")) continue;
                Vector3 hand = B[side + "Hand"].position;
                Vector3 along = B[side + "HandMiddle1"].position - hand, across = B[side + "HandIndex1"].position - (B.ContainsKey(side + "HandPinky1") ? B[side + "HandPinky1"] : B[side + "HandMiddle1"]).position;
                Vector3 palm = Vector3.Cross(along, across).normalized;
                if (Vector3.Dot(palm, B["Hips"].position - hand) < 0) palm = -palm; // A duruşunda avuç gövdeye bakar
                palmLocal[side] = Quaternion.Inverse(B[side + "Hand"].rotation) * palm;
                foreach (var f in new[] { "Index", "Middle", "Ring", "Pinky", "Thumb" })
                    for (int k = 1; k <= 2; k++)
                    {
                        if (!B.TryGetValue(side + "Hand" + f + k, out var b)) continue;
                        Vector3 d = B.TryGetValue(side + "Hand" + f + (k + 1), out var nx) ? nx.position - b.position : b.position - b.parent.position;
                        Vector3 ax = Vector3.Cross(d.normalized, palm).normalized;
                        curlAxis[b] = Quaternion.Inverse(b.rotation) * ax;
                    }
            }
        }

        void SetFace(string f)
        {
            if (f == curFace) return; curFace = f;
            foreach (var kv in faces) foreach (var r in kv.Value) r.enabled = kv.Key == f;
        }

        // ---------------------------------------------------------------- poz
        Quaternion rootQ;
        Vector3 W(float x, float y, float z) => rootQ * new Vector3(x, y, z).normalized;
        void Aim(string bone, string child, Vector3 dirW)
        {
            if (!B.TryGetValue(bone, out var b) || !B.TryGetValue(child, out var c)) return;
            var cur = (c.position - b.position).normalized;
            if (cur.sqrMagnitude < 1e-6f) return;
            b.rotation = Quaternion.FromToRotation(cur, dirW) * b.rotation;
        }
        readonly Dictionary<string, Vector3> palmLocal = new Dictionary<string, Vector3>();
        bool isSynty; float glance, glanceTo, glanceT, glanceTilt, glanceNext;
        // kemiği çocuğuna doğru verilen dünya yönüne çevir, k oranında (0 = olduğu gibi)
        void AimK(string bone, string child, Vector3 dirW, float k)
        {
            if (k <= 0 || !B.TryGetValue(bone, out var b) || !B.TryGetValue(child, out var c)) return;
            var cur = (c.position - b.position).normalized; if (cur.sqrMagnitude < 1e-6f || dirW.sqrMagnitude < 1e-6f) return;
            var target = Quaternion.FromToRotation(cur, dirW.normalized) * b.rotation;
            b.rotation = Quaternion.Slerp(b.rotation, target, k);
        }
        // kahraman eli: işaret parmağı hedefe dümdüz, diğer parmaklar avuca kıvrık yumruk, başparmak yanında
        void SyntyPointHand(float r)
        {
            if (r <= 0.01f || !B.TryGetValue("RightHand", out var hand) || !B.TryGetValue("RightHandIndex1", out var i1) || !palmLocal.TryGetValue("Right", out var pl)) return;
            var dir = (i1.position - hand.position).normalized;
            if (B.TryGetValue("RightHandMiddle1", out var m1)) dir = ((i1.position + m1.position) * 0.5f - hand.position).normalized;
            var palm = (hand.rotation * pl).normalized;
            AimK("RightHandIndex1", "RightHandIndex2", dir, r); AimK("RightHandIndex2", "RightHandIndex3", dir, r); AimK("RightHandIndex3", "RightHandIndex4", dir, r);
            AimK("RightHandMiddle1", "RightHandMiddle2", palm * 0.85f + dir * 0.25f, r);
            AimK("RightHandMiddle2", "RightHandMiddle3", -dir + palm * 0.35f, r);
            AimK("RightHandMiddle3", "RightHandMiddle4", -dir - palm * 0.6f, r);
            AimK("RightHandThumb1", "RightHandThumb2", dir * 0.9f + palm * 0.22f, r); AimK("RightHandThumb2", "RightHandThumb3", dir * 0.85f + palm * 0.4f, r);
        }
        void Bend(string bone, float ang)
        {
            if (!B.TryGetValue(bone, out var b) || !curlAxis.TryGetValue(b, out var ax)) return;
            b.localRotation = b.localRotation * Quaternion.AngleAxis(ang * Mathf.Rad2Deg, ax);
        }
        void CharRot(string bone, float x, float y, float z)
        {
            if (!B.TryGetValue(bone, out var b)) return;
            var q = rootQ * Quaternion.Euler(x * Mathf.Rad2Deg, y * Mathf.Rad2Deg, z * Mathf.Rad2Deg) * Quaternion.Inverse(rootQ);
            b.rotation = q * b.rotation;
        }
        static Vector3 Lerp(Vector3 a, Vector3 b, float k) => Vector3.Lerp(a.normalized, b.normalized, k).normalized;
        static readonly string[] FING = { "Index", "Middle", "Ring", "Pinky" };

        void LateUpdate() { try { Pose(Time.time); } catch (System.Exception e) { ZuLog.Err("Rig.Pose " + name, e); } }

        void Pose(float time)
        {
            foreach (var kv in rest) kv.Key.localRotation = kv.Value;
            rootQ = transform.rotation;
            var c = face;
            if (fp)
            {
                Aim("RightArm", "RightForeArm", W(0, 0, 1)); Aim("RightForeArm", "RightHand", W(0, 0, 1)); Aim("RightHand", "RightHandMiddle1", W(0, -0.05f, 1));
                for (int i = 1; i < 4; i++) { Bend("RightHand" + FING[i] + "1", 1.5f); Bend("RightHand" + FING[i] + "2", 1.6f); }
                Bend("RightHandThumb1", 0.5f); Bend("RightHandThumb2", 0.6f);
                return;
            }
            if (!ghost && (!stool || hang))
            {
                Aim("LeftUpLeg", "LeftLeg", W(-0.07f, -1, 0.02f)); Aim("RightUpLeg", "RightLeg", W(0.07f, -1, 0.02f));
                Aim("LeftLeg", "LeftFoot", W(-0.04f, -1, -0.03f)); Aim("RightLeg", "RightFoot", W(0.04f, -1, -0.03f));
                Aim("LeftFoot", "LeftToeBase", W(-0.12f, -0.12f, 1)); Aim("RightFoot", "RightToeBase", W(0.12f, -0.12f, 1));
            }
            else if (!ghost)
            {
                Aim("LeftUpLeg", "LeftLeg", W(-0.12f, -0.04f, 1)); Aim("RightUpLeg", "RightLeg", W(0.12f, -0.04f, 1));
                Aim("LeftLeg", "LeftFoot", W(-0.05f, -1, 0.06f)); Aim("RightLeg", "RightFoot", W(0.05f, -1, 0.06f));
                Aim("LeftFoot", "LeftToeBase", W(-0.1f, -0.15f, 1)); Aim("RightFoot", "RightToeBase", W(0.1f, -0.15f, 1));
            }
            // gövde: eğilme + beden dili
            // tepki ağırlıkları yumuşakça hedefe gider (sert geçiş yok)
            float dt = Mathf.Clamp(Time.deltaTime, 0, 0.1f), kk = 1 - Mathf.Exp(-dt * 7f);
            bThink += (c.think - bThink) * kk; bLaugh += (Mathf.Max(c.laugh, c.open * c.smile) - bLaugh) * kk; bSurp += (c.surprise - bSurp) * kk;
            bAngry += (c.angry - bAngry) * kk; bSad += (c.sad - bSad) * kk; bTalk += (c.talk - bTalk) * kk;
            // bekleme: her karakterin kendi duruşu; ara sıra değişir
            if (time > idleNext) { idleNext = time + 7 + Random.value * 9; idleKind = Random.value < 0.8f ? 0 : Random.Range(1, 3); } // çoğunlukla eller dizlerde
            float idleCross = idleKind == 1 ? 1 : 0, idleChin = idleKind == 2 ? 1 : 0;
            bCross += (idleCross * (1 - bTalk) * (1 - bLaugh) - bCross) * kk * 0.5f;
            bChin += (idleChin * (1 - bTalk) - bChin) * kk * 0.5f;
            float laugh = bLaugh;
            float breath = Mathf.Sin(time * 1.7f + phase) * 0.018f, sway = Mathf.Sin(time * 0.45f + phase * 2) * 0.03f;
            float ux = upperPitch + bAngry * 0.14f + bSad * 0.12f - bSurp * 0.16f + Mathf.Sin(time * 13) * laugh * 0.07f - laugh * 0.06f + (ghost ? 0 : 0.05f) + breath + bTalk * Mathf.Sin(time * 2.3f + phase) * 0.04f;
            CharRot("Spine", ux * 0.5f, sway * 0.5f, upperRoll * 0.5f + sway * 0.3f); CharRot("Spine1", ux * 0.5f, sway * 0.5f, upperRoll * 0.5f);
            if (!ghost) { CharRot("LeftShoulder", 0, 0, -breath * 1.5f); CharRot("RightShoulder", 0, 0, breath * 1.5f); }
            float hx = headPitch + bSad * 0.3f - c.up * 0.08f - bSurp * 0.12f + bAngry * 0.06f - laugh * (0.18f + Mathf.Sin(time * 13) * 0.05f) + bThink * 0.1f + bChin * 0.08f;
            float hr = bThink * 0.16f + bChin * 0.1f + bTalk * Mathf.Sin(time * 1.7f + phase) * 0.06f;
            // bekleme canlılığı: ara sıra etrafa göz gezdirir (baş yavaşça döner, hafif eğilir), ağırlığını değiştirir
            if (time > glanceNext) { glanceNext = time + 2.2f + Random.value * 4.5f; glanceTo = Random.value < 0.35f ? 0 : (Random.value - 0.5f) * 0.9f; glanceTilt = (Random.value - 0.5f) * 0.12f; }
            float calm = (1 - bTalk) * (1 - laugh) * (ghost ? 0.3f : 1);
            glance += (glanceTo * calm - glance) * (1 - Mathf.Exp(-dt * 2.2f)); glanceT += (glanceTilt * calm - glanceT) * (1 - Mathf.Exp(-dt * 1.5f));
            float hy2 = headYaw + glance; float hr2 = hr + glanceT;
            CharRot("Neck", hx * 0.35f, hy2 * 0.35f, hr2 * 0.4f); CharRot("Head", hx * 0.65f, hy2 * 0.65f, hr2 * 0.6f);
            if (!ghost) { float ws = Mathf.Sin(time * 0.27f + phase * 1.3f) * 0.03f * calm; CharRot("Spine", 0, ws * 0.5f, ws); }
            // sol kol: dinlenme / el sallama / konuşurken jest
            float w = Mathf.Clamp01(wave);
            float ges = !ghost && w < 0.05f && c.open > 0.12f ? 0.35f + Mathf.Sin(time * 5.3f) * 0.15f : 0;
            if (ghost) { Aim("LeftArm", "LeftForeArm", W(-0.55f, -1, 0.1f)); Aim("LeftForeArm", "LeftHand", W(-0.35f, -1, 0.35f)); }
            else
            {
                float gs = Mathf.Sin(time * 4.1f + phase), gc = Mathf.Cos(time * 2.9f + phase);
                if (stool) KneeIK("Left", -1, out kUpL, out kFoL); else { kUpL = new Vector3(-0.2f, -1, 0.04f); kFoL = new Vector3(-0.08f, -1, 0.14f); }
                var upL = Blend(time, kUpL,
                    (bThink + bChin, new Vector3(-0.12f, -0.55f, 0.75f)), (bCross, new Vector3(-0.2f, -0.85f, 0.5f)), (laugh, new Vector3(-0.3f, -0.9f, 0.32f)),
                    (bSurp, new Vector3(-0.6f, -0.35f, 0.6f)), (bTalk * (1 - bThink), new Vector3(-0.42f, -0.72f, 0.62f + gs * 0.1f)));
                var foL = Blend(time, kFoL,
                    (bThink + bChin, new Vector3(0.32f, 0.95f, 0.3f)), (bCross, new Vector3(1f, 0.12f, 0.32f)), (laugh, new Vector3(0.85f, -0.05f, 0.55f)),
                    (bSurp, new Vector3(-0.15f, 0.9f, 0.4f)), (bTalk * (1 - bThink), new Vector3(-0.38f + gs * 0.25f, 0.2f + gc * 0.2f, 1)));
                var haL = Blend(time, stool ? new Vector3(0.05f, -0.9f, 0.5f) : new Vector3(0, -1, 0.12f), (bThink + bChin, new Vector3(0.35f, 0.75f, 0.55f)), (bCross, new Vector3(1, 0, 0.1f)), (bSurp, new Vector3(-0.1f, 1, 0.3f)), (bTalk, new Vector3(-0.3f, 0.5f, 1)));
                Aim("LeftArm", "LeftForeArm", rootQ * Lerp(upL, new Vector3(-0.45f, 0.95f, 0.15f), w));
                Aim("LeftForeArm", "LeftHand", rootQ * Lerp(foL, new Vector3(-0.15f - waveWiggle * 1.2f, 1, 0.15f), w));
                Aim("LeftHand", "LeftHandMiddle1", rootQ * Lerp(haL, new Vector3(-0.05f, 1, 0.2f), w));
            }
            float fist = bAngry * (1 - bTalk);
            foreach (var f in FING) { Bend("LeftHand" + f + "1", (0.35f + fist * 1.0f - bSurp * 0.3f) * (1 - w)); Bend("LeftHand" + f + "2", (0.4f + fist * 1.1f) * (1 - w)); }
            // sağ kol: dinlenme ↔ işaret parmağıyla gösterme
            float r = Mathf.Clamp01(pointRaise);
            var P = new Vector3(Mathf.Sin(pointYaw) + 0.08f, 0.16f, Mathf.Cos(pointYaw));
            if (ghost) { Aim("RightArm", "RightForeArm", W(0.55f, -1, 0.1f)); Aim("RightForeArm", "RightHand", W(0.35f, -1, 0.35f)); }
            else
            {
                float gs2 = Mathf.Sin(time * 3.3f + phase * 1.7f);
                if (stool) KneeIK("Right", 1, out kUpR, out kFoR); else { kUpR = new Vector3(0.2f, -1, 0.04f); kFoR = new Vector3(0.08f, -1, 0.14f); }
                var upR = Blend(time, kUpR, (bCross, new Vector3(0.2f, -0.85f, 0.5f)), (laugh, new Vector3(0.3f, -0.9f, 0.32f)),
                    (bSurp, new Vector3(0.6f, -0.35f, 0.6f)), (bTalk * 0.6f, new Vector3(0.4f, -0.75f, 0.6f)), (bAngry * (1 - bCross), new Vector3(0.3f, -0.95f, 0.25f)));
                var foR = Blend(time, kFoR, (bCross, new Vector3(-1f, 0.18f, 0.28f)), (laugh, new Vector3(-0.85f, -0.05f, 0.55f)),
                    (bSurp, new Vector3(0.15f, 0.9f, 0.4f)), (bTalk * 0.6f, new Vector3(0.35f + gs2 * 0.2f, 0.15f, 1)));
                Aim("RightArm", "RightForeArm", rootQ * Lerp(upR, P, r));
                Aim("RightForeArm", "RightHand", rootQ * Lerp(foR, P, r));
                Aim("RightHand", "RightHandMiddle1", rootQ * Lerp(stool ? new Vector3(-0.05f, -0.9f, 0.5f) : new Vector3(0, -1, 0.12f), P, r));
            }
            Bend("RightHandIndex1", 0.35f * (1 - r)); Bend("RightHandIndex2", 0.4f * (1 - r));
            for (int i = 1; i < 4; i++) { Bend("RightHand" + FING[i] + "1", 0.35f + r * 1.15f); Bend("RightHand" + FING[i] + "2", 0.4f + r * 1.2f); }
            Bend("RightHandThumb1", r * 0.5f); Bend("RightHandThumb2", r * 0.6f);
            if (isSynty && !ghost) SyntyPointHand(r);
            if (cheer > 0.01f && !ghost && !hang) Victory(time, Mathf.Clamp01(cheer));
            if (hang)
            {
                // idam: kollar yanda sarkık, baş öne ve yana düşmüş
                Aim("LeftArm", "LeftForeArm", W(-0.14f, -1, 0.03f)); Aim("LeftForeArm", "LeftHand", W(-0.06f, -1, 0.06f));
                Aim("RightArm", "RightForeArm", W(0.14f, -1, 0.03f)); Aim("RightForeArm", "RightHand", W(0.06f, -1, 0.06f));
                if (limp) CharRot("Head", 0.5f, 0, 0.32f);
            }
            // yüz: kızgın / mutlu / normal; konuşurken ağız açılıp kapanır
            string fc = cheer > 0.5f ? "happy" : "usual";
            if (cheer > 0.5f) { }
            else if (c.angry > 0.45f) fc = "angry";
            else if (c.smile > 0.55f) fc = "happy";
            else if (c.open > 0.12f && Mathf.FloorToInt(time * 7) % 2 == 0) fc = "happy";
            SetFace(fc);
            if (eyes != null && eyes.gameObject.activeSelf != redEyes) eyes.gameObject.SetActive(redEyes);
        }

        // birinci şahıs: yalnız sağ kolun üçgenleri (ten + kıyafet + eldiven)
        void MakeFirstPerson(List<Renderer> visible)
        {
            foreach (var r in visible)
            {
                r.enabled = false;
                if (!(r is SkinnedMeshRenderer smr) || smr.sharedMesh == null || !smr.sharedMesh.isReadable) continue;
                var src = smr.sharedMesh; var bw = src.boneWeights; if (bw.Length == 0) continue;
                var armRoot = B["RightArm"]; bool Arm(int bi) { var bt = smr.bones[bi]; return bt != null && (bt == armRoot || bt.IsChildOf(armRoot)); }
                var keep = new List<int>();
                for (int s = 0; s < src.subMeshCount; s++)
                {
                    var tr = src.GetTriangles(s);
                    for (int i = 0; i < tr.Length; i += 3) if (Arm(bw[tr[i]].boneIndex0) && Arm(bw[tr[i + 1]].boneIndex0) && Arm(bw[tr[i + 2]].boneIndex0)) { keep.Add(tr[i]); keep.Add(tr[i + 1]); keep.Add(tr[i + 2]); }
                }
                if (keep.Count == 0) continue;
                var m = Instantiate(src); m.subMeshCount = 1; m.SetTriangles(keep, 0);
                var go = new GameObject("kol"); go.transform.SetParent(smr.transform.parent, false);
                go.transform.localPosition = smr.transform.localPosition; go.transform.localRotation = smr.transform.localRotation; go.transform.localScale = smr.transform.localScale;
                var n2 = go.AddComponent<SkinnedMeshRenderer>(); n2.sharedMesh = m; n2.bones = smr.bones; n2.rootBone = smr.rootBone;
                var em = new Material(smr.sharedMaterial); em.SetFloat("_Cull", 0f); em.doubleSidedGI = true; em.EnableKeyword("_EMISSION"); em.SetTexture("_EmissionMap", ATLAS); em.SetColor("_EmissionColor", Color.white * 0.1f);
                n2.sharedMaterial = em; n2.shadowCastingMode = ShadowCastingMode.Off; n2.updateWhenOffscreen = true;
            }
            Pose(0);
            var model = body.transform; model.position += transform.position - B["RightArm"].position;
            string tip = B.ContainsKey("RightHandIndex2") ? "RightHandIndex2" : "RightHand";
            reach = Vector3.Distance(B["RightArm"].position, B[tip].position) / Mathf.Max(0.001f, transform.lossyScale.x);
        }
    }
}
