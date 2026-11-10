// Karakter: tek iskelete bağlı parçalı model (beden, 3 yüz ifadesi, saç, şapka, gözlük, kıyafet...).
// Her oyuncu bir kombin giyer. Oyun her karede kumanda değerlerini ayarlar (kafa yönü, eğilme,
// parmakla gösterme, el sallama, mimik); LateUpdate bunları kemiklere uygular.
// Yön kuralı: karakter +Z'ye (ateşe) bakar, +X karakterin SAĞI, +Y yukarı.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace VK
{
    public class Face { public float smile, open, up, angry, sad; }

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
        readonly Dictionary<string, Transform> B = new Dictionary<string, Transform>();
        readonly List<KeyValuePair<Transform, Quaternion>> rest = new List<KeyValuePair<Transform, Quaternion>>();
        readonly Dictionary<Transform, Vector3> curlAxis = new Dictionary<Transform, Vector3>();
        readonly Dictionary<string, List<Renderer>> faces = new Dictionary<string, List<Renderer>>();
        string curFace = "";
        Transform eyes;

        static string Nm(string n) { int k = n.LastIndexOf(':'); return k >= 0 ? n.Substring(k + 1) : n; }

        public static Rig Make(int outfitIndex, Color skinTint, float size, bool ghost = false, bool fp = false)
        {
            if (BASE == null) { BASE = Resources.Load<GameObject>("Character/Creative_Character_free"); ATLAS = Resources.Load<Texture2D>("Character/Textures_4"); }
            var root = new GameObject(ghost ? "Hayalet" : fp ? "Kolum" : "Köylü");
            var rig = root.AddComponent<Rig>();
            rig.ghost = ghost; rig.fp = fp;
            rig.Setup(OUTFITS[outfitIndex % OUTFITS.Length], skinTint, size);
            return rig;
        }

        void Setup(Outfit outfit, Color skinTint, float size)
        {
            phase = Random.value * 6.28f;
            var model = Instantiate(BASE, transform); model.name = "model"; body = model;
            foreach (var t in model.GetComponentsInChildren<Transform>()) { var n = Nm(t.name); if (!B.ContainsKey(n)) B[n] = t; }

            // malzemeler
            var lit = Resources.Load<Material>("Mats/Lit");
            Material Cloth(float shift) { var m = new Material(lit); m.SetTexture("_BaseMap", ATLAS); m.SetTextureOffset("_BaseMap", new Vector2(shift, 0)); m.SetFloat("_Smoothness", 0.25f); return m; }
            var skin = Cloth(0); skin.SetColor("_BaseColor", skinTint);
            var cloth = Cloth(0); cloth.SetColor("_BaseColor", outfit.tint); var plain = Cloth(0);
            var glass = new Material(lit); glass.SetColor("_BaseColor", new Color(0.08f, 0.12f, 0.16f)); glass.SetFloat("_Smoothness", 0.9f);
            if (ghost && ghostMat == null) ghostMat = new Material(Resources.Load<Material>("Mats/Ghost"));

            var show = new HashSet<string>(ALWAYS); foreach (var p in outfit.parts) show.Add(p);
            var known = new HashSet<string>(KNOWN);
            var visible = new List<Renderer>();
            foreach (var r in model.GetComponentsInChildren<Renderer>(true))
            {
                string key = Nm(r.name);
                if (!known.Contains(key) && r.transform.parent != null && known.Contains(Nm(r.transform.parent.name))) key = Nm(r.transform.parent.name);
                bool on = show.Contains(key);
                r.enabled = on;
                if (r is SkinnedMeshRenderer smr) smr.updateWhenOffscreen = true;
                if (!on) continue;
                bool isSkin = key == "Body_010" || key.Contains("emotion");
                var baseM = isSkin ? skin : (key.Contains("Hair") || key.Contains("Moustache") || key.Contains("Clown")) ? plain : cloth;
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
            ComputeCurlAxes();

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

            if (fp) { MakeFirstPerson(visible); return; }

            Pose(0);
            // ayak tabanı yere, kalça tabureye
            if (ghost)
            {
                model.transform.position += Vector3.down * (B["LeftToeBase"].position.y - transform.position.y - 0.02f);
            }
            else
            {
                float low = Mathf.Min(B["LeftToeBase"].position.y, B["RightToeBase"].position.y, B["LeftFoot"].position.y - 0.06f, B["RightFoot"].position.y - 0.06f);
                model.transform.position += Vector3.down * (low - transform.position.y - 0.02f);
                Vector3 hips = transform.InverseTransformPoint(B["Hips"].position);
                float seatH = Mathf.Max(0.2f, hips.y - 0.08f);
                model.transform.localPosition -= new Vector3(0, 0, hips.z + 0.02f);
                var lit2 = new Material(lit); lit2.SetColor("_BaseColor", new Color(0.42f, 0.29f, 0.18f));
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

        void ComputeCurlAxes()
        {
            foreach (var side in new[] { "Left", "Right" })
            {
                if (!B.ContainsKey(side + "Hand") || !B.ContainsKey(side + "HandMiddle1")) continue;
                Vector3 hand = B[side + "Hand"].position;
                Vector3 along = B[side + "HandMiddle1"].position - hand, across = B[side + "HandIndex1"].position - B[side + "HandPinky1"].position;
                Vector3 palm = Vector3.Cross(along, across).normalized;
                if (Vector3.Dot(palm, B["Hips"].position - hand) < 0) palm = -palm; // A duruşunda avuç gövdeye bakar
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

        void LateUpdate() { Pose(Time.time); }

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
            if (!ghost)
            {
                Aim("LeftUpLeg", "LeftLeg", W(-0.12f, -0.04f, 1)); Aim("RightUpLeg", "RightLeg", W(0.12f, -0.04f, 1));
                Aim("LeftLeg", "LeftFoot", W(-0.05f, -1, 0.06f)); Aim("RightLeg", "RightFoot", W(0.05f, -1, 0.06f));
                Aim("LeftFoot", "LeftToeBase", W(-0.1f, -0.15f, 1)); Aim("RightFoot", "RightToeBase", W(0.1f, -0.15f, 1));
            }
            // gövde: eğilme + beden dili
            float laugh = c.open * c.smile;
            float breath = Mathf.Sin(time * 1.7f + phase) * 0.018f, sway = Mathf.Sin(time * 0.45f + phase * 2) * 0.03f;
            float ux = upperPitch + c.angry * 0.12f + c.sad * 0.1f - c.up * 0.08f + Mathf.Sin(time * 12) * laugh * 0.06f + (ghost ? 0 : 0.05f) + breath;
            CharRot("Spine", ux * 0.5f, sway * 0.5f, upperRoll * 0.5f + sway * 0.3f); CharRot("Spine1", ux * 0.5f, sway * 0.5f, upperRoll * 0.5f);
            if (!ghost) { CharRot("LeftShoulder", 0, 0, -breath * 1.5f); CharRot("RightShoulder", 0, 0, breath * 1.5f); }
            float hx = headPitch + c.sad * 0.28f - c.up * 0.1f + c.angry * 0.06f;
            CharRot("Neck", hx * 0.35f, headYaw * 0.35f, 0); CharRot("Head", hx * 0.65f, headYaw * 0.65f, 0);
            // sol kol: dinlenme / el sallama / konuşurken jest
            float w = Mathf.Clamp01(wave);
            float ges = !ghost && w < 0.05f && c.open > 0.12f ? 0.35f + Mathf.Sin(time * 5.3f) * 0.15f : 0;
            if (ghost) { Aim("LeftArm", "LeftForeArm", W(-0.55f, -1, 0.1f)); Aim("LeftForeArm", "LeftHand", W(-0.35f, -1, 0.35f)); }
            else
            {
                Aim("LeftArm", "LeftForeArm", rootQ * Lerp(new Vector3(-0.25f, -1, 0.35f), new Vector3(-0.45f, 0.95f, 0.15f), w));
                var fore = Lerp(new Vector3(-0.02f, -0.45f, 1), new Vector3(-0.6f, 0.4f, 1), ges);
                Aim("LeftForeArm", "LeftHand", rootQ * Lerp(fore, new Vector3(-0.15f - waveWiggle * 1.2f, 1, 0.15f), w));
                Aim("LeftHand", "LeftHandMiddle1", rootQ * Lerp(new Vector3(0.1f, -0.7f, 1), new Vector3(-0.05f, 1, 0.2f), w));
            }
            foreach (var f in FING) { Bend("LeftHand" + f + "1", 0.35f * (1 - w)); Bend("LeftHand" + f + "2", 0.4f * (1 - w)); }
            // sağ kol: dinlenme ↔ işaret parmağıyla gösterme
            float r = Mathf.Clamp01(pointRaise);
            var P = new Vector3(Mathf.Sin(pointYaw) + 0.08f, 0.16f, Mathf.Cos(pointYaw));
            if (ghost) { Aim("RightArm", "RightForeArm", W(0.55f, -1, 0.1f)); Aim("RightForeArm", "RightHand", W(0.35f, -1, 0.35f)); }
            else
            {
                Aim("RightArm", "RightForeArm", rootQ * Lerp(new Vector3(0.25f, -1, 0.35f), P, r));
                Aim("RightForeArm", "RightHand", rootQ * Lerp(new Vector3(0.02f, -0.45f, 1), P, r));
                Aim("RightHand", "RightHandMiddle1", rootQ * Lerp(new Vector3(-0.1f, -0.7f, 1), P, r));
            }
            Bend("RightHandIndex1", 0.35f * (1 - r)); Bend("RightHandIndex2", 0.4f * (1 - r));
            for (int i = 1; i < 4; i++) { Bend("RightHand" + FING[i] + "1", 0.35f + r * 1.15f); Bend("RightHand" + FING[i] + "2", 0.4f + r * 1.2f); }
            Bend("RightHandThumb1", r * 0.5f); Bend("RightHandThumb2", r * 0.6f);
            // yüz: kızgın / mutlu / normal; konuşurken ağız açılıp kapanır
            string fc = "usual";
            if (c.angry > 0.45f) fc = "angry";
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
                bool Arm(int bi) { var n = Nm(smr.bones[bi].name); return n.StartsWith("RightArm") || n.StartsWith("RightForeArm") || n.StartsWith("RightHand"); }
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
        }
    }
}
