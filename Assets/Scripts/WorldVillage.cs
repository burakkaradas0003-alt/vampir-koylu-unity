// Köy: kullanıcının low-poly köy paketi (Resources/Village/VillageUnity.fbx + renk paleti).
// Paketteki her parça (ev, ahır, değirmen, kuyu, fener, tezgâh, ağaç...) adıyla bulunur,
// tabanı yere oturacak şekilde ortalanır ve meydanın çevresine yerleştirilir.
// Gece pencereler ve fenerler paletin sarı hücrelerinden yayılan ışıkla parlar.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace VK
{
    public partial class World
    {
        GameObject villageSrc; Material villageMat; Dictionary<string, Transform> vparts;
        System.Random vr = new System.Random(11);
        float VR(float a, float b) => a + (float)vr.NextDouble() * (b - a);

        public GameObject PutVillage(string name, Transform parent, Vector3 localPos, float yaw, float scale) { var g = Put(name, parent, localPos, yaw, scale); return g; }
        // giyinme seti zemini: düz gölgeli taş döşeme + çevresinde çimen
        public void StudioGround(Transform parent)
        {
            var rnd = new System.Random(3); float F(float a, float b) => a + (float)rnd.NextDouble() * (b - a);
            var v = new List<Vector3>(); var col = new List<Color>(); var tr = new List<int>();
            void Quad(Vector3 c, float sx, float sz, float rot, Color cc)
            {
                var q = Quaternion.Euler(0, rot, 0); int i0 = v.Count;
                foreach (var o in new[] { new Vector3(-sx, 0, -sz), new Vector3(-sx, 0, sz), new Vector3(sx, 0, sz), new Vector3(sx, 0, -sz) }) { v.Add(c + q * o + new Vector3(0, F(0, 0.02f), 0)); col.Add(cc); }
                tr.Add(i0); tr.Add(i0 + 1); tr.Add(i0 + 2); tr.Add(i0); tr.Add(i0 + 2); tr.Add(i0 + 3);
            }
            // zemin: koyu toprak
            int s0 = v.Count; foreach (var o in new[] { new Vector3(-14, 0, -6), new Vector3(-14, 0, 14), new Vector3(14, 0, 14), new Vector3(14, 0, -6) }) { v.Add(o); col.Add(new Color(0.42f, 0.55f, 0.28f)); }
            tr.Add(s0); tr.Add(s0 + 1); tr.Add(s0 + 2); tr.Add(s0); tr.Add(s0 + 2); tr.Add(s0 + 3);
            // kaldırım taşları (kapıya giden yol ve meydan)
            for (int i = 0; i < 70; i++)
            {
                float x = F(-3.2f, 3.2f), z = F(-2.2f, 2.6f); if (Mathf.Abs(x) > 1.4f && z > 1.2f) continue;
                float g = F(0.42f, 0.6f); Quad(new Vector3(x, 0.015f, z), F(0.22f, 0.38f), F(0.18f, 0.3f), F(0, 90), new Color(g, g * 0.98f, g * 0.95f));
            }
            for (int i = 0; i < 26; i++) { float z = F(2.0f, 4.2f), x = F(-0.8f, 0.8f); float g = F(0.45f, 0.6f); Quad(new Vector3(x, 0.018f, z), F(0.2f, 0.32f), F(0.16f, 0.26f), F(0, 90), new Color(g, g, g * 0.95f)); }
            var m = new Mesh { indexFormat = IndexFormat.UInt32 }; m.SetVertices(v); m.SetColors(col); m.SetTriangles(tr, 0); m.RecalculateNormals(); m.RecalculateBounds();
            var mat = new Material(matGrass); mat.enableInstancing = false; mat.SetFloat("_Wind", 0); mat.SetColor("_Tint", Color.white);
            var go = MeshGO("set zemini", m, mat, parent, false); go.GetComponent<MeshRenderer>().receiveShadows = true;
            go.transform.localPosition = Vector3.zero;
        }
        bool LoadVillage()
        {
            villageSrc = Resources.Load<GameObject>("Village/VillageUnity");
            if (villageSrc == null) return false;
            vparts = new Dictionary<string, Transform>();
            foreach (var t in villageSrc.GetComponentsInChildren<Transform>(true))
                if (t.GetComponent<MeshFilter>() != null && !vparts.ContainsKey(t.name)) vparts[t.name] = t;
            if (vparts.Count < 10) return false;
            villageMat = new Material(Resources.Load<Material>("Mats/LitEmissive"));
            villageMat.SetTexture("_BaseMap", Resources.Load<Texture2D>("Village/palette")); villageMat.SetColor("_BaseColor", Color.white);
            villageMat.SetTexture("_EmissionMap", Resources.Load<Texture2D>("Village/emission")); villageMat.SetColor("_EmissionColor", Color.black);
            villageMat.EnableKeyword("_EMISSION"); villageMat.SetFloat("_Smoothness", 0.12f); villageMat.SetFloat("_Metallic", 0);
            windowMats.Add(villageMat); // gece pencere ışığı ApplyTOD'da ayarlanır
            return true;
        }

        // bir parçayı yerleştir: taban ortası 'pos'a, ön yüzü (+Z) 'yaw' yönüne
        GameObject Put(string name, Transform parent, Vector3 pos, float yaw, float scale = 1, bool shadows = true)
        {
            if (vparts == null || !vparts.TryGetValue(name, out var src)) return null;
            var mesh = src.GetComponent<MeshFilter>().sharedMesh; if (mesh == null) return null;
            var g = new GameObject(name); g.transform.SetParent(parent, false);
            var m = new GameObject("model"); m.transform.SetParent(g.transform, false);
            var rot = src.rotation; var scl = src.lossyScale;
            m.transform.localRotation = rot; m.transform.localScale = scl;
            // dönmüş ve ölçeklenmiş sınırlar
            var b = mesh.bounds; var tr = Matrix4x4.TRS(Vector3.zero, rot, scl);
            Vector3 mn = Vector3.one * 1e9f, mx = Vector3.one * -1e9f;
            for (int i = 0; i < 8; i++)
            {
                var c = b.center + Vector3.Scale(b.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                var p = tr.MultiplyPoint3x4(c); mn = Vector3.Min(mn, p); mx = Vector3.Max(mx, p);
            }
            m.transform.localPosition = -new Vector3((mn.x + mx.x) / 2, mn.y, (mn.z + mx.z) / 2);
            m.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = m.AddComponent<MeshRenderer>(); r.sharedMaterial = villageMat; r.shadowCastingMode = shadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
            g.transform.localPosition = new Vector3(pos.x, pos.y, pos.z); g.transform.localRotation = Quaternion.Euler(0, yaw, 0); g.transform.localScale = Vector3.one * scale;
            return g;
        }
        // ---------------------------------------------------------------- ortaçağ ev paketi (Resources/Medieval/pack.fbx)
        // yarı ahşap evler + değirmen; düz renkli malzemeler adlarına göre bizim ışıklı malzemelerimizle değişir,
        // pencere camı ve fenerler gece sıcak turuncu parlar
        // paket önceden ikili ağ dosyasına çevrildi (Resources/Medieval/medieval.bytes): ölçek 8.2 m ev yüksekliği,
        // taban y=0, ortalanmış, kapı +Z. Her nesne malzeme adına göre alt ağlara ayrılmış.
        Dictionary<string, (Mesh mesh, string[] mats)> medObj; Dictionary<string, Material> medMats;
        bool LoadMedieval()
        {
            var ta = Resources.Load<TextAsset>("Medieval/medieval"); if (ta == null) return false;
            medObj = new Dictionary<string, (Mesh, string[])>(); medMats = new Dictionary<string, Material>();
            using (var br = new System.IO.BinaryReader(new System.IO.MemoryStream(ta.bytes)))
            {
                string Str() { int n = br.ReadInt32(); return System.Text.Encoding.UTF8.GetString(br.ReadBytes(n)); }
                int cnt = br.ReadInt32();
                for (int o = 0; o < cnt; o++)
                {
                    string nm = Str(); int vc = br.ReadInt32();
                    var pos = new Vector3[vc]; var nor = new Vector3[vc];
                    for (int i = 0; i < vc; i++) pos[i] = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
                    for (int i = 0; i < vc; i++) nor[i] = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
                    int sc = br.ReadInt32(); var mats = new string[sc]; var subs = new int[sc][];
                    for (int k = 0; k < sc; k++) { mats[k] = Str(); int ic = br.ReadInt32(); var ix = new int[ic]; for (int i = 0; i < ic; i++) ix[i] = br.ReadInt32(); subs[k] = ix; }
                    var m = new Mesh { name = nm, indexFormat = IndexFormat.UInt32 }; m.vertices = pos; m.normals = nor; m.subMeshCount = sc;
                    for (int k = 0; k < sc; k++) m.SetTriangles(subs[k], k);
                    m.RecalculateBounds(); m.UploadMeshData(true);
                    medObj[nm] = (m, mats);
                }
            }
            return medObj.Count > 0;
        }
        Material MedMat(string raw)
        {
            string k = ""; foreach (char ch in raw) if (char.IsLetterOrDigit(ch)) k += ch;
            if (medMats.TryGetValue(k, out var m)) return m;
            Color c; bool glow = false; float sm = 0.08f;
            switch (k)
            {
                case "Mat": c = new Color(0.36f, 0.35f, 0.34f); break;              // taş temel, baca
                case "Mat1": c = new Color(0.2f, 0.19f, 0.18f); glow = true; sm = 0.5f; break; // pencere camı
                case "Mat2": c = new Color(0.33f, 0.21f, 0.12f); break;             // ahşap, çatı
                case "Mat5": c = new Color(0.3f, 0.42f, 0.22f); break;              // çimen
                case "Mat6": c = new Color(0.78f, 0.74f, 0.66f); break;             // sıva ve değirmen kanatları
                case "Mat7": c = new Color(0.9f, 0.55f, 0.18f); glow = true; break; // fener
                default: c = new Color(0.7f, 0.68f, 0.64f); break;
            }
            m = new Material(Resources.Load<Material>(glow ? "Mats/LitEmissive" : "Mats/Lit")); m.SetColor("_BaseColor", c); m.SetFloat("_Smoothness", sm); m.SetFloat("_Metallic", 0);
            if (glow) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", Color.black); windowMats.Add(m); }
            medMats[k] = m; return m;
        }
        // bir parçayı yerleştir: taban ortası 'pos'a, kapısı (+Z) 'yaw' yönüne
        GameObject PutMed(string name, Transform parent, Vector3 pos, float yaw, float scale = 1)
        {
            if (medObj == null || !medObj.TryGetValue(name, out var o)) return null;
            var g = new GameObject(name); g.transform.SetParent(parent, false);
            var m = new GameObject("model"); m.transform.SetParent(g.transform, false); m.transform.localScale = Vector3.one * scale;
            m.AddComponent<MeshFilter>().sharedMesh = o.mesh;
            var r = m.AddComponent<MeshRenderer>(); var ms = new Material[o.mats.Length]; for (int i = 0; i < ms.Length; i++) ms[i] = MedMat(o.mats[i]);
            r.sharedMaterials = ms; r.shadowCastingMode = ShadowCastingMode.On; r.receiveShadows = true;
            g.transform.localPosition = pos; g.transform.localRotation = Quaternion.Euler(0, yaw, 0);
            return g;
        }

        static float Yaw(Vector3 from, Vector3 to) { var d = to - from; return Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg; }
        Vector3 Ring(float angRad, float r) { float x = Mathf.Cos(angRad) * r, z = Mathf.Sin(angRad) * r; return new Vector3(x, GroundY(x, z), z); }

        // fener camının (paletteki parlak sarı hücre) dünya konumu: hale buraya konur
        static Vector3 GlowPoint(GameObject g)
        {
            var mf = g.GetComponentInChildren<MeshFilter>(); var mesh = mf.sharedMesh;
            if (mesh == null || !mesh.isReadable) return g.transform.position + Vector3.up * 3;
            var v = mesh.vertices; var uv = mesh.uv; var sum = Vector3.zero; int n = 0;
            for (int i = 0; i < v.Length && i < uv.Length; i++)
            {
                float u = uv[i].x - Mathf.Floor(uv[i].x), w = uv[i].y - Mathf.Floor(uv[i].y);
                int cx = Mathf.Clamp((int)(u * 8), 0, 7), cy = Mathf.Clamp((int)((1 - w) * 8), 0, 7);
                if (cy == 5 && (cx == 5 || cx == 6)) { sum += v[i]; n++; }
            }
            if (n == 0) { var bb = g.GetComponentInChildren<MeshRenderer>().bounds; return new Vector3(bb.center.x, bb.max.y - 0.7f, bb.center.z); }
            return mf.transform.TransformPoint(sum / n);
        }

        bool BuildAssetVillage(Transform root)
        {
            if (!LoadVillage()) return false;
            var center = Vector3.zero;
            bool med = LoadMedieval();
            string[] medHouses = { "house1", "house4", "house2", "house5", "house3", "house1", "house2", "house4" };
            // evler: kapıları meydana bakar
            string[] houses = { "House_01", "House_04", "House_02", "House_06", "Barn_03", "House_05", "House_03", "House_07" };
            for (int i = 0; i < houses.Length; i++)
            {
                bool big = houses[i] == "House_04" || houses[i] == "House_05" || houses[i] == "House_06" || houses[i] == "House_07";
                float a = (i + 0.5f) / houses.Length * Mathf.PI * 2 + 0.2f, d = big ? 21.5f : 18.5f;
                var p = Ring(a, d);
                if (med) { big = medHouses[i] == "house1" || medHouses[i] == "house5"; d = big ? 21.5f : 19f; p = Ring(a, d); }
                var h = med ? PutMed(medHouses[i], root, p, Yaw(p, center)) : null;
                if (h == null) h = Put(houses[i], root, p, Yaw(p, center), big ? 0.82f : 0.88f);
                if (h == null) continue;
                // evin yanında küçük eşyalar
                var side = Quaternion.Euler(0, Yaw(p, center), 0) * Vector3.right;
                var front = (center - p).normalized;
                string[] small = { "Vooden_Barrel", "Wooden_Case", "Box", "Chest" };
                var sp = p + side * (big ? 5.2f : 4.2f) * (i % 2 == 0 ? 1 : -1) + front * 1.2f; sp.y = GroundY(sp.x, sp.z);
                Put(small[i % small.Length], root, sp, VR(0, 360), 1.1f);
                if (i % 3 == 0) { var sp2 = sp + side * 0.9f; Put("Box", root, sp2, VR(0, 360), 1.1f); }
                // kapı önüne çiçekler
                string[] fl = { "Flower_Yellow", "Flower_Purple", "Flower_Blue" };
                for (int k = 0; k < 4; k++)
                {
                    var fp = p + front * (big ? 4.6f : 3.9f) + side * ((k < 2 ? -1 : 1) * (1.6f + (k % 2) * 0.35f)) + new Vector3(VR(-0.15f, 0.15f), 0, VR(-0.15f, 0.15f));
                    fp.y = 0; Put(fl[(k + i) % 3], root, fp, VR(0, 360), VR(1.6f, 2.1f), false);
                }
                var bp = p + side * (big ? -5.6f : -4.6f) * (i % 2 == 0 ? 1 : -1) - front * 1.0f; bp.y = GroundY(bp.x, bp.z);
                Put(i % 2 == 0 ? "Bushes_01" : "Bushes_03", root, bp, VR(0, 360), VR(1.2f, 1.6f));
            }
            // sokak fenerleri (gece haleleri bunların camına konur)
            for (int i = 0; i < 6; i++)
            {
                float a = i / 6f * Mathf.PI * 2 + 0.5f; var p = Ring(a, 11.5f);
                var l = Put("Street_Lamp", root, p, Yaw(p, center) + 90, 0.95f);
                if (l != null) lanternPos.Add(GlowPoint(l));
            }
            // meydan eşyaları
            { var p = Ring(1.05f, 12.6f); Put("Well", root, p, Yaw(p, center), 1.25f); }
            { var p = Ring(3.1f, 13.4f); Put("Wood_Car", root, p, Yaw(p, center) + 70, 1.3f); }
            { var p = Ring(5.2f, 12.8f); Put("Woods", root, p, Yaw(p, center) + 90, 1.4f); var q = Ring(5.32f, 12.2f); Put("Wood_02", root, q, VR(0, 360), 1.3f); var q2 = Ring(5.08f, 13.4f); Put("Billet_01", root, q2, Yaw(q2, center) + 80, 1.2f); }
            { var p = Ring(2.1f, 14.6f); Put("Stand_Orange", root, p, Yaw(p, center), 1.05f); }
            { var p = Ring(4.15f, 14.6f); Put("Stand_Purple", root, p, Yaw(p, center), 1.05f); }
            { var p = Ring(0.0f, 14.8f); Put("Stand_Wood", root, p, Yaw(p, center), 1.15f); var q = Ring(0.12f, 14.8f); Put("Stand_Empty_Wood", root, q, Yaw(q, center), 1.15f); }
            foreach (float a in new[] { 1.55f, 3.65f, 5.75f }) { var p = Ring(a, 13.6f); Put("Bank_01", root, p, Yaw(p, center), 1.05f); }
            { var p = Ring(2.65f, 16.2f); Put("Bank_02", root, p, Yaw(p, center) + 20, 1f); }
            // çalılar ve taşlar
            for (int i = 0; i < 18; i++) { var p = Ring(VR(0, 6.283f), VR(15.5f, 26)); Put(new[] { "Bushes_01", "Bushes_02", "Bushes_03" }[i % 3], root, p, VR(0, 360), VR(1.1f, 1.8f)); }
            for (int i = 0; i < 10; i++) { var p = Ring(VR(0, 6.283f), VR(9, 27)); Put("Stone_0" + (1 + i % 6), root, p, VR(0, 360), VR(2.2f, 4f), i % 2 == 0); }
            // uzak çevre: değirmen, su kulesi, gözetleme kulesi, ahırlar, tarlalar, çitler
            { var p = Ring(0.78f, 42); if ((med ? PutMed("windmill", root, p, Yaw(p, center)) : null) == null) Put("Windmill", root, p, Yaw(p, center), 1.1f); }
            { var p = Ring(2.55f, 34); Put("Water_Tank", root, p, Yaw(p, center), 1f); }
            { var p = Ring(4.35f, 31); Put("Tower", root, p, Yaw(p, center), 1f); }
            { var p = Ring(1.95f, 31); Put("Barn_01", root, p, Yaw(p, center), 0.95f); }
            { var p = Ring(3.55f, 32); Put("Barn_02", root, p, Yaw(p, center), 0.95f); }
            { var p = Ring(5.55f, 40); Put("Sunflower_Field", root, p, Yaw(p, center), 1f, false); }
            { var p = Ring(0.3f, 35); Put("Empty_Field", root, p, Yaw(p, center) + 90, 1f, false); }
            string[] fences = { "Fence", "Fence_Bold", "Fence_Crack", "Fence_Small" };
            for (int i = 0; i < 14; i++)
            {
                float a = 2.75f + i * 0.072f; var p = Ring(a, 27.5f);
                Put(fences[i % fences.Length], root, p, Yaw(p, center) + 90, 1.05f, false);
            }
            // ağaçlar: yakında yapraklı, uzakta çam ormanı, arada kuru ağaçlar (gece ürkütücü siluet)
            for (int i = 0; i < (low ? 8 : 14); i++) { var p = Ring(VR(0, 6.283f), VR(24, 34)); Put(i % 2 == 0 ? "Tree_01" : "Tree_03", root, p, VR(0, 360), VR(0.9f, 1.25f)); }
            for (int i = 0; i < (low ? 45 : 80); i++) { var p = Ring(VR(0, 6.283f), i < 25 ? VR(26, 40) : VR(40, 78)); Put("Tree_02", root, p, VR(0, 360), VR(1.1f, 1.9f)); }
            for (int i = 0; i < 6; i++) { var p = Ring(VR(0, 6.283f), VR(22, 30)); Put(i % 2 == 0 ? "Dry_Tree_01" : "Dry_Tree_02", root, p, VR(0, 360), VR(1.3f, 1.8f)); }
            StepStones(root);
            return true;
        }

        void StepStones(Transform root)
        {
            var parts = new List<CombineInstance>(); var stoneMesh = Ico(1);
            for (int i = 0; i < 8; i++)
            {
                float a = (i + 0.5f) / 8 * Mathf.PI * 2 + 0.2f, end = 15.2f;
                for (float r = 6.2f; r < end; r += 0.85f)
                {
                    float j = Mathf.Sin(r * 3.1f + i) * 0.22f;
                    var pos = new Vector3(Mathf.Cos(a) * r - Mathf.Sin(a) * j, 0.01f, Mathf.Sin(a) * r + Mathf.Cos(a) * j);
                    parts.Add(new CombineInstance { mesh = stoneMesh, transform = Matrix4x4.TRS(pos, Quaternion.Euler(0, VR(0, 360), 0), new Vector3(VR(0.32f, 0.42f), 0.05f, VR(0.26f, 0.36f))) });
                }
            }
            var path = new Mesh { indexFormat = IndexFormat.UInt32 }; path.CombineMeshes(parts.ToArray(), true, true); path.RecalculateBounds();
            MeshGO("basamak taşları", path, Lit(new Color(0.55f, 0.53f, 0.5f), 0.05f), root, false);
        }
    }
}
