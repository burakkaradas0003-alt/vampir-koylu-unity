// Köy sahnesi: zemin (kum meydan + gerçek çim fotoğrafı), evler, ağaçlar, kamp ateşi (kullanıcının modeli),
// alev kartları, parçacıklar (köz, duman, patlamalar), sık çimen öbekleri ve gün/gece geçişi.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace VK
{
    public partial class World : MonoBehaviour
    {
        public static World I;
        public Camera cam;
        public float tod;              // 0 gündüz – 1 gece
        public bool low;
        Light sun, fireLight;
        Material matLit, matGrass, matFlame, matGlow, skyMat;
        Texture2D skyTex, dotTex;
        Volume volume; Bloom bloom;
        readonly List<Material> windowMats = new List<Material>();
        Material emberMat;
        System.Random srnd = new System.Random(7);
        float R(float a, float b) => a + (float)srnd.NextDouble() * (b - a);

        // ---------------------------------------------------------------- kurulum
        public void Build(bool lowQuality)
        {
            I = this; low = lowQuality;
            matLit = Resources.Load<Material>("Mats/Lit");
            matGrass = Resources.Load<Material>("Mats/Grass");
            matFlame = Resources.Load<Material>("Mats/Flame");
            matGlow = Resources.Load<Material>("Mats/Glow");
            dotTex = MakeDot();

            // kamera
            var cgo = new GameObject("Kamera");
            cam = cgo.AddComponent<Camera>();
            cam.fieldOfView = 62; cam.nearClipPlane = 0.05f; cam.farClipPlane = 600;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cgo.AddComponent<AudioListener>();
            var cd = cgo.AddComponent<UniversalAdditionalCameraData>();
            cd.renderPostProcessing = !low; cd.antialiasing = AntialiasingMode.FastApproximateAntialiasing;

            // ışıklar
            var sgo = new GameObject("Güneş"); sun = sgo.AddComponent<Light>(); sun.type = LightType.Directional;
            sun.shadows = LightShadows.Soft; sun.shadowStrength = 0.8f; sun.transform.rotation = Quaternion.Euler(42, -35, 0);
            var fgo = new GameObject("Ateş Işığı"); fireLight = fgo.AddComponent<Light>(); fireLight.type = LightType.Point;
            fireLight.color = new Color(1f, 0.6f, 0.27f); fireLight.range = 18; fgo.transform.position = new Vector3(0, 1.1f, 0);
            fireLight.shadows = LightShadows.None;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Linear; RenderSettings.fogStartDistance = 26; RenderSettings.fogEndDistance = 120;

            // parlama
            if (!low)
            {
                var vgo = new GameObject("Efekt"); volume = vgo.AddComponent<Volume>(); volume.isGlobal = true;
                var prof = ScriptableObject.CreateInstance<VolumeProfile>();
                bloom = prof.Add<Bloom>(true); bloom.intensity.Override(0.6f); bloom.threshold.Override(0.9f); bloom.scatter.Override(0.65f);
                var tm = prof.Add<Tonemapping>(true); tm.mode.Override(TonemappingMode.ACES);
                var ca = prof.Add<ColorAdjustments>(true); grade = ca; ca.postExposure.Override(0.25f); ca.contrast.Override(12f); ca.saturation.Override(12f); ca.colorFilter.Override(Color.white);
                var vg = prof.Add<Vignette>(true); vign = vg; vg.intensity.Override(0.28f); vg.smoothness.Override(0.45f);
                volume.profile = prof;
            }

            BuildSky();
            env = new Env(this); env.Build(transform, cam);
            if (RenderSettings.skybox != null && skySphere != null) skySphere.SetActive(false);
            BuildGround();
            BuildStones();
            BuildVillage();
            BuildCampfire();
            BuildGrass();
            BuildAtmos();
            // hareketsiz sahne parçalarını aynı malzemeye göre birleştir (çizim çağrısı azalır)
            foreach (var n in new[] { "Köy", "Taşlar" }) { var g = transform.Find(n); if (g != null) StaticBatchingUtility.Combine(g.gameObject); }
            SetLevel(low ? 2 : 0);
            ApplyTOD(0);
        }

        Material TexMat(Tex.Pair p, Vector2 tiling, Color tint, float smooth = 0.08f)
        {
            var m = new Material(Resources.Load<Material>("Mats/LitNormal"));
            m.SetTexture("_BaseMap", p.col); m.SetTexture("_BumpMap", p.nrm); m.SetFloat("_BumpScale", 1f);
            m.SetTextureScale("_BaseMap", tiling); m.SetColor("_BaseColor", tint); m.SetFloat("_Smoothness", smooth);
            m.SetColor("_EmissionColor", Color.black); m.SetTexture("_OcclusionMap", null);
            return m;
        }
        Material Lit(Color c, float smooth = 0.1f)
        {
            var m = new Material(matLit); m.SetColor("_BaseColor", c); m.SetFloat("_Smoothness", smooth); return m;
        }
        GameObject Prim(PrimitiveType t, Material m, Vector3 pos, Vector3 scale, Transform parent, Vector3 rot = default)
        {
            var g = GameObject.CreatePrimitive(t);
            Destroy(g.GetComponent<Collider>());
            g.transform.SetParent(parent, false); g.transform.localPosition = pos; g.transform.localScale = scale; g.transform.localEulerAngles = rot;
            g.GetComponent<MeshRenderer>().sharedMaterial = m;
            return g;
        }
        GameObject MeshGO(string name, Mesh mesh, Material m, Transform parent, bool shadows = true)
        {
            var g = new GameObject(name); g.transform.SetParent(parent, false);
            g.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = g.AddComponent<MeshRenderer>(); r.sharedMaterial = m; r.shadowCastingMode = shadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
            return g;
        }

        static Texture2D MakeDot()
        {
            var t = new Texture2D(32, 32, TextureFormat.RGBA32, false); t.wrapMode = TextureWrapMode.Clamp;
            for (int y = 0; y < 32; y++) for (int x = 0; x < 32; x++)
            {
                float d = Mathf.Sqrt((x - 15.5f) * (x - 15.5f) + (y - 15.5f) * (y - 15.5f)) / 15.5f;
                t.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(1 - d) * Mathf.Clamp01(1 - d)));
            }
            t.Apply(); return t;
        }

        // ---------------------------------------------------------------- gökyüzü
        GameObject moon; Matrix4x4[] starM; Vector4[] starC; MaterialPropertyBlock starMpb;
        Env env; GameObject skySphere;
        void BuildSky()
        {
            skyTex = new Texture2D(1, 64, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            skyMat = new Material(Resources.Load<Material>("Mats/Unlit"));
            skyMat.SetTexture("_BaseMap", skyTex); skyMat.SetFloat("_Cull", 1f); // içten görünsün
            var sky = Prim(PrimitiveType.Sphere, skyMat, Vector3.zero, Vector3.one * 900, transform); skySphere = sky;
            sky.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            moon = Prim(PrimitiveType.Sphere, Resources.Load<Material>("Mats/Unlit"), new Vector3(-120, 150, 180), Vector3.one * 14, transform);
            moon.GetComponent<MeshRenderer>().material.SetColor("_BaseColor", new Color(1.6f, 1.55f, 1.3f));
            int n = 260; starM = new Matrix4x4[n]; starC = new Vector4[1023]; // renk dizisi hep 1023: WebGL örnekli çizimde kısa dizi sorun çıkarıyor starMpb = new MaterialPropertyBlock();
            for (int i = 0; i < n; i++)
            {
                var d = Random.onUnitSphere; d.y = Mathf.Abs(d.y) * 0.9f + 0.1f;
                starM[i] = Matrix4x4.TRS(d.normalized * 380, Quaternion.identity, Vector3.one * R(0.9f, 2.2f));
                starC[i] = new Vector4(1, 1, 1, R(0.4f, 1));
            }
        }

        void PaintSky(float k)
        {
            Color top = Color.Lerp(new Color(0.42f, 0.62f, 0.86f), new Color(0.02f, 0.03f, 0.09f), k);
            Color hor = Color.Lerp(new Color(0.93f, 0.86f, 0.74f), new Color(0.09f, 0.08f, 0.2f), k);
            for (int y = 0; y < 64; y++) { float v = y / 63f; float u = Mathf.Clamp01((v - 0.45f) / 0.55f); skyTex.SetPixel(0, y, Color.Lerp(hor, top, Mathf.Pow(u, 0.6f))); }
            skyTex.Apply();
            cam.backgroundColor = hor;
        }

        // ---------------------------------------------------------------- zemin
        void BuildGround()
        {
            // çim: gerçek çim fotoğrafı, uzakta tepeler
            var turf = Resources.Load<Texture2D>("Grass/turf");
            if (turf != null) { turf.filterMode = FilterMode.Trilinear; turf.anisoLevel = 8; }
            var gm = new Material(matLit); gm.SetTexture("_BaseMap", turf); gm.SetColor("_BaseColor", new Color(0.62f, 0.78f, 0.5f)); gm.SetFloat("_Smoothness", 0.05f);
            int n = low ? 90 : 140; float size = 200;
            var verts = new List<Vector3>(); var uvs = new List<Vector2>(); var tris = new List<int>();
            for (int j = 0; j <= n; j++) for (int i = 0; i <= n; i++)
            {
                float x = -size / 2 + size * i / n, z = -size / 2 + size * j / n, d = Mathf.Sqrt(x * x + z * z);
                float y = R(-0.03f, 0.03f);
                if (d > 34) y += Mathf.Min(Mathf.Pow((d - 34) / 30f, 2) * 3.5f, 7f) * (0.6f + 0.4f * Mathf.Sin(x * 0.07f + z * 0.05f));
                verts.Add(new Vector3(x, y, z)); uvs.Add(new Vector2(x * 0.45f, z * 0.9f));
            }
            for (int j = 0; j < n; j++) for (int i = 0; i < n; i++)
            {
                int a = j * (n + 1) + i, b = a + 1, c = a + n + 1, e = c + 1;
                Vector3 mid = (verts[a] + verts[e]) * 0.5f; if (new Vector2(mid.x, mid.z).magnitude < 8.4f) continue;
                tris.Add(a); tris.Add(c); tris.Add(b); tris.Add(b); tris.Add(c); tris.Add(e);
            }
            var m = new Mesh { indexFormat = IndexFormat.UInt32 }; m.SetVertices(verts); m.SetUVs(0, uvs); m.SetTriangles(tris, 0); m.RecalculateNormals(); m.RecalculateBounds();
            var g = MeshGO("Çimen zemin", m, gm, transform, false); g.GetComponent<MeshRenderer>().receiveShadows = true;
            groundMesh = m;
            // kum meydan
            var sand = TexMat(Tex.Sand(), Vector2.one, Color.white, 0.02f);
            var disc = new Mesh(); var dv = new List<Vector3> { new Vector3(0, 0.02f, 0) }; var dt = new List<int>();
            int seg = 96; for (int i = 0; i <= seg; i++) { float a = i * Mathf.PI * 2 / seg; float r = 9.55f + Mathf.Sin(a * 7) * 0.12f; dv.Add(new Vector3(Mathf.Cos(a) * r, 0.02f, Mathf.Sin(a) * r)); }
            for (int i = 1; i <= seg; i++) { dt.Add(0); dt.Add(i + 1); dt.Add(i); }
            disc.SetVertices(dv); disc.SetUVs(0, dv.ConvertAll(p => new Vector2(p.x * 0.12f, p.z * 0.12f))); disc.SetTriangles(dt, 0); disc.RecalculateNormals(); disc.RecalculateTangents();
            MeshGO("Meydan", disc, sand, transform, false);
        }
        Mesh groundMesh;

        // düz gölgeli ikosahedron (taş, yaprak öbeği)
        public static Mesh Ico(float r = 1)
        {
            float t = (1 + Mathf.Sqrt(5)) / 2;
            var p = new[] { new Vector3(-1, t, 0), new Vector3(1, t, 0), new Vector3(-1, -t, 0), new Vector3(1, -t, 0), new Vector3(0, -1, t), new Vector3(0, 1, t), new Vector3(0, -1, -t), new Vector3(0, 1, -t), new Vector3(t, 0, -1), new Vector3(t, 0, 1), new Vector3(-t, 0, -1), new Vector3(-t, 0, 1) };
            int[] f = { 0, 11, 5, 0, 5, 1, 0, 1, 7, 0, 7, 10, 0, 10, 11, 1, 5, 9, 5, 11, 4, 11, 10, 2, 10, 7, 6, 7, 1, 8, 3, 9, 4, 3, 4, 2, 3, 2, 6, 3, 6, 8, 3, 8, 9, 4, 9, 5, 2, 4, 11, 6, 2, 10, 8, 6, 7, 9, 8, 1 };
            var v = new Vector3[f.Length]; var tr = new int[f.Length];
            for (int i = 0; i < f.Length; i += 3) { v[i] = p[f[i]].normalized * r; v[i + 1] = p[f[i + 1]].normalized * r; v[i + 2] = p[f[i + 2]].normalized * r; tr[i] = i; tr[i + 1] = i + 1; tr[i + 2] = i + 2; }
            var m = new Mesh(); m.vertices = v; m.triangles = tr; m.RecalculateNormals(); m.RecalculateBounds(); return m;
        }

        void BuildStones()
        {
            var ico = Ico(1); var parent = new GameObject("Taşlar").transform; parent.SetParent(transform, false);
            // 4 ortak malzeme: taşlar tek tek değil, birleşik çizilsin
            var sm = new Material[4]; for (int k = 0; k < 4; k++) sm[k] = Lit(Color.HSVToRGB(0.07f, 0.24f, 0.62f + k * 0.04f));
            for (int i = 0; i < 140; i++)
            {
                float a = R(0, 6.283f), d = Mathf.Sqrt(R(0, 1)) * 8.6f; if (d < 1.4f) { i--; continue; }
                float s = R(0.08f, 0.26f);
                var g = MeshGO("taş", ico, sm[i % 4], parent, true);
                g.transform.localPosition = new Vector3(Mathf.Cos(a) * d, 0.02f, Mathf.Sin(a) * d);
                g.transform.localScale = new Vector3(s * R(1, 1.6f), s * 0.28f, s * R(0.8f, 1.3f)); g.transform.localEulerAngles = new Vector3(0, R(0, 360), 0);
            }
        }

        // ---------------------------------------------------------------- köy
        void BuildVillage()
        {
            var root = new GameObject("Köy").transform; root.SetParent(transform, false);
            // kullanıcının köy paketi varsa onu kullan; yoksa eski kodla üretilen köy
            if (BuildAssetVillage(root)) return;
            var tP = Tex.Plaster(); var tW = Tex.Wood(); var tS = Tex.Stone();
            var plaster = TexMat(tP, new Vector2(2, 1.5f), Color.white); var timber = TexMat(tW, new Vector2(1, 1), Color.white, 0.15f);
            var roofs = new[] { TexMat(Tex.RoofTiles(new Color(0.62f, 0.22f, 0.17f)), new Vector2(1, 1), Color.white, 0.2f), TexMat(Tex.RoofTiles(new Color(0.48f, 0.3f, 0.2f)), new Vector2(1, 1), Color.white, 0.2f), TexMat(Tex.RoofTiles(new Color(0.36f, 0.33f, 0.36f)), new Vector2(1, 1), Color.white, 0.25f) };
            var stone = TexMat(tS, new Vector2(3, 0.6f), Color.white);
            var shutterMats = new[] { Lit(new Color(0.2f, 0.36f, 0.3f)), Lit(new Color(0.22f, 0.3f, 0.45f)), Lit(new Color(0.45f, 0.22f, 0.16f)) };
            var flowerMats = new[] { Lit(new Color(0.9f, 0.2f, 0.25f)), Lit(new Color(0.95f, 0.8f, 0.2f)), Lit(new Color(0.85f, 0.4f, 0.8f)), Lit(new Color(0.3f, 0.6f, 0.25f)) };
            var flowerIco = Ico(1);
            int houses = 8;
            for (int i = 0; i < houses; i++)
            {
                float a = (i + 0.5f) / houses * Mathf.PI * 2 + 0.2f, d = 17 + (i % 3) * 1.6f;
                var h = new GameObject("Ev").transform; h.SetParent(root, false);
                h.localPosition = new Vector3(Mathf.Cos(a) * d, 0, Mathf.Sin(a) * d);
                h.LookAt(new Vector3(0, 0, 0));
                float w = R(4.2f, 5.4f), dep = R(3.6f, 4.4f), ht = R(2.8f, 3.4f);
                Prim(PrimitiveType.Cube, stone, new Vector3(0, 0.25f, 0), new Vector3(w + 0.15f, 0.5f, dep + 0.15f), h);
                Prim(PrimitiveType.Cube, plaster, new Vector3(0, ht / 2 + 0.25f, 0), new Vector3(w, ht, dep), h);
                // ahşap çatkı
                for (int k = 0; k < 4; k++) { float x = -w / 2 + w * k / 3f; Prim(PrimitiveType.Cube, timber, new Vector3(x, ht / 2 + 0.25f, dep / 2 + 0.02f), new Vector3(0.16f, ht, 0.06f), h); }
                Prim(PrimitiveType.Cube, timber, new Vector3(0, ht + 0.2f, dep / 2 + 0.02f), new Vector3(w, 0.16f, 0.06f), h);
                Prim(PrimitiveType.Cube, timber, new Vector3(0, ht * 0.55f, dep / 2 + 0.02f), new Vector3(w, 0.14f, 0.06f), h);
                Prim(PrimitiveType.Cube, timber, new Vector3(-w / 3, ht * 0.75f, dep / 2 + 0.03f), new Vector3(0.12f, ht * 0.55f, 0.05f), h, new Vector3(0, 0, 38));
                Prim(PrimitiveType.Cube, timber, new Vector3(w / 3, ht * 0.75f, dep / 2 + 0.03f), new Vector3(0.12f, ht * 0.55f, 0.05f), h, new Vector3(0, 0, -38));
                Prim(PrimitiveType.Cube, timber, new Vector3(0, ht * 0.75f, dep / 2 + 0.03f), new Vector3(0.12f, ht * 0.5f, 0.05f), h, new Vector3(0, 0, 90));
                // kapı ve pencereler (gece ışıklı)
                Prim(PrimitiveType.Cube, timber, new Vector3(0, 1.0f, dep / 2 + 0.04f), new Vector3(0.95f, 1.6f, 0.06f), h);
                var win = new Material(Resources.Load<Material>("Mats/LitEmissive")); win.SetColor("_BaseColor", new Color(0.18f, 0.16f, 0.14f)); windowMats.Add(win);
                foreach (float x in new[] { -w / 3, w / 3 }) foreach (float y in new[] { 1.4f, ht * 0.8f + 0.25f })
                    {
                        Prim(PrimitiveType.Cube, win, new Vector3(x, y, dep / 2 + 0.04f), new Vector3(0.6f, 0.62f, 0.05f), h);
                        Prim(PrimitiveType.Cube, timber, new Vector3(x, y, dep / 2 + 0.07f), new Vector3(0.07f, 0.66f, 0.03f), h);
                        Prim(PrimitiveType.Cube, timber, new Vector3(x, y, dep / 2 + 0.07f), new Vector3(0.66f, 0.07f, 0.03f), h);
                        var sm = shutterMats[i % 3];
                        foreach (float sx in new[] { -0.47f, 0.47f }) Prim(PrimitiveType.Cube, sm, new Vector3(x + sx, y, dep / 2 + 0.06f), new Vector3(0.3f, 0.66f, 0.04f), h, new Vector3(0, sx < 0 ? 12 : -12, 0));
                        if (y < 2) // çiçek kutusu
                        {
                            Prim(PrimitiveType.Cube, timber, new Vector3(x, y - 0.42f, dep / 2 + 0.16f), new Vector3(0.7f, 0.14f, 0.2f), h);
                            for (int f = 0; f < 5; f++) { var fl = MeshGO("çiçek", flowerIco, flowerMats[(f + i) % 4], h, false); fl.transform.localPosition = new Vector3(x - 0.28f + f * 0.14f, y - 0.3f, dep / 2 + 0.16f); fl.transform.localScale = Vector3.one * R(0.05f, 0.08f); }
                        }
                    }
                // çatı
                var roof = MeshGO("çatı", Prism(w + 0.7f, ht * 0.7f, dep + 0.7f), roofs[i % 3], h);
                roof.transform.localPosition = new Vector3(0, ht + 0.25f, 0);
                Prim(PrimitiveType.Cube, stone, new Vector3(w * 0.28f, ht + ht * 0.55f, -dep * 0.15f), new Vector3(0.5f, 1.4f, 0.5f), h);
                chimneys.Add(h.TransformPoint(new Vector3(w * 0.28f, ht + ht * 0.55f + 0.75f, -dep * 0.15f)));
                // fıçı, sandık
                if (i % 2 == 0) Prim(PrimitiveType.Cylinder, Lit(new Color(0.42f, 0.27f, 0.16f)), new Vector3(w / 2 + 0.6f, 0.45f, dep / 2 - 0.3f), new Vector3(0.7f, 0.45f, 0.7f), h);
                else Prim(PrimitiveType.Cube, Lit(new Color(0.5f, 0.34f, 0.2f)), new Vector3(-w / 2 - 0.6f, 0.35f, dep / 2 - 0.4f), new Vector3(0.7f, 0.7f, 0.7f), h, new Vector3(0, 20, 0));
            }
            // fenerler
            var post = Lit(new Color(0.2f, 0.17f, 0.15f));
            for (int i = 0; i < 6; i++)
            {
                float a = i / 6f * Mathf.PI * 2 + 0.5f, d = 11.5f;
                var p = new Vector3(Mathf.Cos(a) * d, 0, Mathf.Sin(a) * d);
                Prim(PrimitiveType.Cylinder, post, p + new Vector3(0, 1.3f, 0), new Vector3(0.12f, 1.3f, 0.12f), root);
                var lm = new Material(Resources.Load<Material>("Mats/LitEmissive")); lm.SetColor("_BaseColor", new Color(0.9f, 0.8f, 0.5f)); windowMats.Add(lm);
                Prim(PrimitiveType.Cube, lm, p + new Vector3(0, 2.75f, 0), new Vector3(0.3f, 0.36f, 0.3f), root);
                Prim(PrimitiveType.Cube, post, p + new Vector3(0, 2.98f, 0), new Vector3(0.42f, 0.08f, 0.42f), root);
                Prim(PrimitiveType.Cube, post, p + new Vector3(0, 2.52f, 0), new Vector3(0.36f, 0.06f, 0.36f), root);
                lanternPos.Add(p + new Vector3(0, 2.75f, 0));
            }
            BuildProps(root, stone, timber, roofs[1]);
            // ağaçlar: çam ve meşe
            var trunk = Lit(new Color(0.35f, 0.24f, 0.17f));
            var pines = new[] { Lit(new Color(0.18f, 0.36f, 0.22f)), Lit(new Color(0.22f, 0.42f, 0.25f)) };
            var oaks = new[] { Lit(new Color(0.3f, 0.54f, 0.2f)), Lit(new Color(0.36f, 0.6f, 0.23f)), Lit(new Color(0.27f, 0.49f, 0.18f)) };
            var ico = Ico(1);
            for (int i = 0; i < (low ? 60 : 130); i++)
            {
                float a = R(0, 6.283f), d = i < 40 ? R(22, 34) : R(34, 75);
                var t = new GameObject("Ağaç").transform; t.SetParent(root, false); t.localPosition = new Vector3(Mathf.Cos(a) * d, GroundY(Mathf.Cos(a) * d, Mathf.Sin(a) * d) - 0.1f, Mathf.Sin(a) * d);
                float s = R(0.8f, 1.5f); t.localScale = Vector3.one * s;
                if (i % 3 != 0)
                {
                    Prim(PrimitiveType.Cylinder, trunk, new Vector3(0, 1, 0), new Vector3(0.45f, 1, 0.45f), t);
                    for (int k = 0; k < 4; k++) { var c = MeshGO("çam", Cone(2.4f - k * 0.45f, 2.6f, 8), pines[k % 2], t); c.transform.localPosition = new Vector3(0, 1.6f + k * 1.3f, 0); }
                }
                else
                {
                    Prim(PrimitiveType.Cylinder, trunk, new Vector3(0, 1.7f, 0), new Vector3(0.7f, 1.7f, 0.7f), t);
                    for (int k = 0; k < 6; k++)
                    {
                        float aa = k / 6f * 6.283f; var l = MeshGO("yaprak", ico, oaks[k % 3], t);
                        l.transform.localPosition = new Vector3(Mathf.Cos(aa) * R(0.9f, 1.6f), 4.2f + R(-0.3f, 0.9f), Mathf.Sin(aa) * R(0.9f, 1.6f));
                        float r = R(1.4f, 2.1f); l.transform.localScale = new Vector3(r, r * 0.8f, r); l.transform.localEulerAngles = new Vector3(R(0, 180), R(0, 180), 0);
                    }
                }
            }
        }

        static Mesh Prism(float w, float h, float d)
        {
            var m = new Mesh();
            Vector3 a = new Vector3(-w / 2, 0, -d / 2), b = new Vector3(w / 2, 0, -d / 2), c = new Vector3(w / 2, 0, d / 2), e = new Vector3(-w / 2, 0, d / 2), t1 = new Vector3(-w / 2, h, 0), t2 = new Vector3(w / 2, h, 0);
            var v = new List<Vector3>(); var tr = new List<int>();
            var uv = new List<Vector2>(); Vector2 U(Vector3 a) => new Vector2(a.x * 0.45f, (a.y + Mathf.Abs(a.z)) * 0.45f);
            void Tri(Vector3 p, Vector3 q, Vector3 r) { int i = v.Count; v.Add(p); v.Add(r); v.Add(q); uv.Add(U(p)); uv.Add(U(r)); uv.Add(U(q)); tr.Add(i); tr.Add(i + 1); tr.Add(i + 2); }
            Tri(e, t1, t2); Tri(e, t2, c); Tri(b, t2, t1); Tri(b, t1, a); Tri(a, t1, e); Tri(c, t2, b);
            m.SetVertices(v); m.SetUVs(0, uv); m.SetTriangles(tr, 0); m.RecalculateNormals(); m.RecalculateTangents(); m.RecalculateBounds(); return m;
        }
        static Mesh Cone(float r, float h, int seg)
        {
            var v = new List<Vector3>(); var tr = new List<int>();
            for (int i = 0; i < seg; i++)
            {
                float a0 = i * Mathf.PI * 2 / seg, a1 = (i + 1) * Mathf.PI * 2 / seg; int k = v.Count;
                v.Add(new Vector3(Mathf.Cos(a0) * r, 0, Mathf.Sin(a0) * r)); v.Add(new Vector3(0, h, 0)); v.Add(new Vector3(Mathf.Cos(a1) * r, 0, Mathf.Sin(a1) * r));
                tr.Add(k); tr.Add(k + 1); tr.Add(k + 2);
            }
            var m = new Mesh(); m.SetVertices(v); m.SetTriangles(tr, 0); m.RecalculateNormals(); m.RecalculateBounds(); return m;
        }

        // ---------------------------------------------------------------- kamp ateşi
        readonly List<Flame> flames = new List<Flame>();
        class Flame { public Transform t; public MeshRenderer r; public MaterialPropertyBlock mpb = new MaterialPropertyBlock(); public float age, life, h, w, x, z, y; }
        Mesh flameQuad;
        static readonly int[][] TILES = { new[] { 321, 16, 544, 494 }, new[] { 785, 30, 1013, 478 }, new[] { 553, 23, 772, 465 }, new[] { 321, 16, 544, 494 }, new[] { 553, 23, 772, 465 }, new[] { 133, 488, 534, 873 }, new[] { 546, 516, 775, 684 } };
        Transform glow; MaterialPropertyBlock glowMpb;

        void BuildCampfire()
        {
            var prefab = Resources.Load<GameObject>("Campfire/Campfire");
            if (prefab != null)
            {
                var cf = Instantiate(prefab, transform); cf.name = "Kamp ateşi";
                var m = new Material(Resources.Load<Material>("Mats/LitNormal"));
                m.SetTexture("_BaseMap", Resources.Load<Texture2D>("Campfire/campfire_base"));
                m.SetTexture("_BumpMap", Resources.Load<Texture2D>("Campfire/campfire_normal"));
                m.SetTexture("_OcclusionMap", Resources.Load<Texture2D>("Campfire/campfire_ao"));
                m.SetTexture("_EmissionMap", Resources.Load<Texture2D>("Campfire/campfire_emission"));
                m.SetColor("_BaseColor", new Color(0.6f, 0.6f, 0.6f)); m.SetFloat("_Smoothness", 0.05f);
                m.SetColor("_EmissionColor", Color.white * 1.6f);
                emberMat = m;
                var rs = cf.GetComponentsInChildren<Renderer>();
                foreach (var r in rs) { r.sharedMaterial = m; r.shadowCastingMode = ShadowCastingMode.Off; }
                // ölçü: 1 m çapa getir, yere oturt
                var b = BoundsOf(rs); float sc = 0.98f / Mathf.Max(b.size.x, b.size.z);
                cf.transform.localScale *= sc; b = BoundsOf(rs);
                cf.transform.position += new Vector3(-b.center.x, -b.min.y, -b.center.z);
            }
            // alev kartları
            flameQuad = new Mesh();
            flameQuad.vertices = new[] { new Vector3(-0.5f, 0, 0), new Vector3(0.5f, 0, 0), new Vector3(-0.5f, 1, 0), new Vector3(0.5f, 1, 0) };
            flameQuad.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1) };
            flameQuad.triangles = new[] { 0, 2, 1, 1, 2, 3 }; flameQuad.RecalculateBounds(); flameQuad.bounds = new Bounds(Vector3.up * 0.5f, Vector3.one * 2);
            var fm = new Material(matFlame); fm.SetTexture("_MainTex", Resources.Load<Texture2D>("Campfire/flames"));
            int n = low ? 6 : 10;
            for (int i = 0; i < n; i++)
            {
                var g = MeshGO("alev", flameQuad, fm, transform, false); g.GetComponent<MeshRenderer>().receiveShadows = false;
                var f = new Flame { t = g.transform, r = g.GetComponent<MeshRenderer>() }; Spawn(f, i / (float)n * 0.8f); flames.Add(f);
            }
            // taban parıltısı
            var gq = MeshGO("parıltı", flameQuad, new Material(matGlow) { mainTexture = dotTex }, transform, false);
            gq.GetComponent<MeshRenderer>().sharedMaterial.SetTexture("_MainTex", dotTex);
            glow = gq.transform; glowMpb = new MaterialPropertyBlock();
        }

        static Bounds BoundsOf(Renderer[] rs) { var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds); return b; }

        void Spawn(Flame f, float t0 = 0)
        {
            int k = Random.Range(0, TILES.Length); var T = TILES[k]; bool flip = Random.value < 0.5f;
            float sx = (T[2] - T[0]) / 1024f, sy = (T[3] - T[1]) / 1024f;
            f.mpb.SetVector("_UV", new Vector4(flip ? -sx : sx, sy, (flip ? T[2] : T[0]) / 1024f, 1 - T[3] / 1024f));
            bool big = k >= 5; float r = Random.value * 0.13f, a = Random.value * 6.283f;
            f.h = big ? 0.5f + Random.value * 0.25f : 0.7f + Random.value * 0.5f; f.w = f.h * (T[2] - T[0]) / (float)(T[3] - T[1]);
            f.x = Mathf.Cos(a) * r; f.z = Mathf.Sin(a) * r; f.y = 0.12f + Random.value * 0.05f; f.age = t0; f.life = 0.55f + Random.value * 0.45f;
        }

        // ---------------------------------------------------------------- parçacıklar
        class Part { public Vector3 p, v; public float age, life, s0, s1, g, a; public Color c; public bool drag; }
        readonly List<Part> parts = new List<Part>();
        Matrix4x4[] pm = new Matrix4x4[1023]; Vector4[] pc = new Vector4[1023]; MaterialPropertyBlock pmpb;
        Mesh dotQuad; Material glowMat, starMat;
        float fireAcc, smokeAcc, emberAcc, chimAcc; readonly List<Vector3> chimneys = new List<Vector3>();

        public void Emit(Vector3 pos, Vector3 vel, float life, float size, float size1, Color col, float gravity = 0, float alpha = 0.85f, bool drag = false)
        {
            if (parts.Count > 900) return;
            parts.Add(new Part { p = pos, v = vel, life = life, s0 = size, s1 = size1, c = col, g = gravity, a = alpha, drag = drag });
        }
        // patlama efekti (oyun olaylarında)
        public void Burst(Vector3 pos, Color col, int n, float speed = 0.6f, float size = 0.14f, float life = 1.2f, float gravity = 0, float up = 0)
        {
            for (int i = 0; i < n; i++)
            {
                var d = Random.onUnitSphere; d.y = Mathf.Abs(d.y) * 0.6f + up;
                Emit(pos, d * speed * (0.5f + Random.value), life * (0.7f + Random.value * 0.5f), size, size * 0.3f, col, gravity, 1, true);
            }
        }

        // ---------------------------------------------------------------- çimen
        struct Batch { public Mesh mesh; public Matrix4x4[][] m; public Vector4[][] tint; public MaterialPropertyBlock[] mpb; public int[] count; }
        readonly List<Batch> grassBatches = new List<Batch>();
        RenderParams grassRp;

        void BuildGrass()
        {
            var turf = Resources.Load<Texture2D>("Grass/turf");
            Color Sample(float x, float z)
            {
                if (turf == null || !turf.isReadable) return new Color(0.42f, 0.62f, 0.26f);
                float u = Mirror(x * 0.45f), v = Mirror(z * 0.9f);
                return turf.GetPixelBilinear(u, v);
            }
            // bir öbek: 13 ince yaprak, 2 parçalı, uca doğru incelir ve kıvrılır
            var tuft = TuftMesh(low ? 9 : 13);
            int n = low ? 5000 : (Application.isMobilePlatform ? 9000 : 18000);
            var mats = new List<Matrix4x4>(); var tints = new List<Vector4>();
            var tint = new Color(0.62f, 0.8f, 0.5f);
            for (int i = 0; i < n; i++)
            {
                float a = R(0, 6.283f); bool edge = i < n * 0.08f, far = i > n * 0.9f, mid = i > n * 0.7f;
                float d = edge ? R(8.6f, 9.5f) : far ? R(22, 36) : mid ? R(14.5f, 22) : 9.2f + Mathf.Pow(R(0, 1), 1.15f) * 5.6f;
                var pos = new Vector3(Mathf.Cos(a) * d, GroundY(Mathf.Cos(a) * d, Mathf.Sin(a) * d), Mathf.Sin(a) * d);
                float s = (edge ? R(0.07f, 0.11f) : R(0.1f, 0.17f)) * (1 + Mathf.Max(0, d - 14) * 0.03f), sw = R(2.2f, 3.2f);
                mats.Add(Matrix4x4.TRS(pos, Quaternion.Euler(R(-4, 4), R(0, 360), R(-4, 4)), new Vector3(s * sw, s, s * sw)));
                var c = Sample(pos.x, pos.z) * tint; tints.Add(new Vector4(c.r, c.g, c.b, 1));
            }
            AddBatch(tuft, mats, tints);
            // kullanıcının yabani çimen modeli
            var wild = Resources.Load<GameObject>("Grass/wild_grass");
            if (wild != null)
            {
                var meshes = new List<Mesh>();
                foreach (var mf in wild.GetComponentsInChildren<MeshFilter>()) if (mf.sharedMesh != null && mf.sharedMesh.vertexCount > 100) meshes.Add(WildMesh(mf.sharedMesh));
                foreach (var smr in wild.GetComponentsInChildren<SkinnedMeshRenderer>()) if (smr.sharedMesh != null && smr.sharedMesh.vertexCount > 100) meshes.Add(WildMesh(smr.sharedMesh));
                int cnt = (low ? 150 : (Application.isMobilePlatform ? 300 : 600)) / Mathf.Max(1, meshes.Count);
                foreach (var wm in meshes)
                {
                    mats.Clear(); tints.Clear();
                    for (int i = 0; i < cnt; i++)
                    {
                        float a = R(0, 6.283f), d = 9.6f + Mathf.Pow(R(0, 1), 1.4f) * 13;
                        var pos = new Vector3(Mathf.Cos(a) * d, -0.01f, Mathf.Sin(a) * d); pos.y += GroundY(pos.x, pos.z);
                        float h = (d < 9.8f ? R(0.16f, 0.24f) : R(0.24f, 0.42f)) * (1 + Mathf.Max(0, d - 13) * 0.02f);
                        mats.Add(Matrix4x4.TRS(pos, Quaternion.Euler(R(-3, 3), R(0, 360), R(-3, 3)), new Vector3(h * R(0.9f, 1.3f), h, h * R(0.9f, 1.3f))));
                        float r = R(0, 1); var c = r < 0.25f ? new Color(1.08f, 1.04f, 0.85f) : r < 0.5f ? new Color(0.86f, 0.94f, 0.84f) : Color.white;
                        tints.Add(new Vector4(c.r, c.g, c.b, 1));
                    }
                    AddBatch(wm, mats, tints);
                }
            }
            grassRp = new RenderParams(matGrass) { shadowCastingMode = ShadowCastingMode.Off, receiveShadows = true, worldBounds = new Bounds(Vector3.zero, Vector3.one * 200) };
        }

        static float Mirror(float u) { u = Mathf.Abs(u) % 2f; return u > 1 ? 2 - u : u; }
        float GroundY(float x, float z) { float d = Mathf.Sqrt(x * x + z * z); return d > 34 ? Mathf.Min(Mathf.Pow((d - 34) / 30f, 2) * 3.5f, 7f) * (0.6f + 0.4f * Mathf.Sin(x * 0.07f + z * 0.05f)) : 0; }

        void AddBatch(Mesh mesh, List<Matrix4x4> mats, List<Vector4> tints)
        {
            int chunks = (mats.Count + 1022) / 1023;
            var b = new Batch { mesh = mesh, m = new Matrix4x4[chunks][], tint = new Vector4[chunks][], mpb = new MaterialPropertyBlock[chunks], count = new int[chunks] };
            for (int c = 0; c < chunks; c++)
            {
                int st = c * 1023, cnt = Mathf.Min(1023, mats.Count - st);
                b.m[c] = mats.GetRange(st, cnt).ToArray(); b.tint[c] = new Vector4[1023];
                for (int i = 0; i < cnt; i++) b.tint[c][i] = tints[st + i];
                b.mpb[c] = new MaterialPropertyBlock(); b.mpb[c].SetVectorArray("_Tint", b.tint[c]); b.count[c] = cnt;
            }
            grassBatches.Add(b);
        }

        Mesh TuftMesh(int nb)
        {
            var pos = new List<Vector3>(); var col = new List<Color>(); var nor = new List<Vector3>(); var idx = new List<int>();
            Color root = new Color(0.32f, 0.36f, 0.3f), mid = new Color(0.72f, 0.76f, 0.7f), tip = new Color(1f, 1f, 0.92f);
            var rnd = new System.Random(3); float Rr(float a, float b) => a + (float)rnd.NextDouble() * (b - a);
            for (int b = 0; b < nb; b++)
            {
                float a = Rr(0, 6.283f), off = Mathf.Sqrt(Rr(0, 1)) * 0.32f, h = Rr(0.5f, 1), w = Rr(0.045f, 0.07f), lean = Rr(0.15f, 0.5f), rot = a + Rr(-0.6f, 0.6f);
                float bx = Mathf.Cos(a) * off, bz = Mathf.Sin(a) * off, dx = Mathf.Cos(rot), dz = Mathf.Sin(rot), px = -dz, pz = dx;
                int bs = pos.Count; const int SEG = 2;
                for (int k = 0; k <= SEG; k++)
                {
                    float t = k / (float)SEG, y = t * h, bend = lean * t * t * h, ww = w * (1 - t * 0.85f);
                    float cx = bx + dx * bend, cz = bz + dz * bend;
                    Color cc = t < 0.5f ? Color.Lerp(root, mid, t * 2) : Color.Lerp(mid, tip, (t - 0.5f) * 2);
                    var n = new Vector3(dx * 0.3f, 1, dz * 0.3f).normalized;
                    if (k < SEG) { pos.Add(new Vector3(cx - px * ww, y, cz - pz * ww)); pos.Add(new Vector3(cx + px * ww, y, cz + pz * ww)); col.Add(cc); col.Add(cc); nor.Add(n); nor.Add(n); }
                    else { pos.Add(new Vector3(cx, y, cz)); col.Add(cc); nor.Add(n); }
                }
                for (int k = 0; k < SEG - 1; k++) { int i0 = bs + k * 2; idx.AddRange(new[] { i0, i0 + 2, i0 + 1, i0 + 1, i0 + 2, i0 + 3 }); }
                int l = bs + (SEG - 1) * 2; idx.AddRange(new[] { l, l + 2, l + 1 });
            }
            var m = new Mesh(); m.SetVertices(pos); m.SetColors(col); m.SetNormals(nor); m.SetTriangles(idx, 0); m.RecalculateBounds(); return m;
        }

        // yabani çimen: 1 birim boya getir, yeşil/kuru yapraklara dipten uca renk ver, alt parçaları birleştir
        static Mesh WildMesh(Mesh src)
        {
            var v = src.vertices; var b = src.bounds; float h = Mathf.Max(0.001f, b.size.y);
            var outV = new Vector3[v.Length]; var col = new Color[v.Length];
            for (int i = 0; i < v.Length; i++) outV[i] = new Vector3((v[i].x - b.center.x) / h, (v[i].y - b.min.y) / h, (v[i].z - b.center.z) / h);
            Color[][] G = { new[] { new Color(0.165f, 0.29f, 0.094f), new Color(0.37f, 0.6f, 0.22f), new Color(0.66f, 0.8f, 0.38f) }, new[] { new Color(0.29f, 0.18f, 0.086f), new Color(0.6f, 0.42f, 0.2f), new Color(0.82f, 0.66f, 0.41f) } };
            var all = new List<int>();
            for (int s = 0; s < src.subMeshCount; s++)
            {
                var tr = src.GetTriangles(s); all.AddRange(tr);
                var g = G[Mathf.Min(s, 1)];
                foreach (int i in tr) { float t = Mathf.Clamp01(outV[i].y); col[i] = t < 0.4f ? Color.Lerp(g[0], g[1], t / 0.4f) : Color.Lerp(g[1], g[2], (t - 0.4f) / 0.6f); }
            }
            var m = new Mesh { indexFormat = v.Length > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
            m.vertices = outV; m.colors = col; m.normals = src.normals; m.SetTriangles(all, 0); m.RecalculateBounds(); return m;
        }

        // ---------------------------------------------------------------- gün / gece
        float todFrom, todTo, todT0, todDur = -1;
        public bool TodBusy => todDur > 0;
        public void SetTOD(float to, float seconds) { todFrom = tod; todTo = to; todT0 = Time.time; todDur = Mathf.Max(0.01f, seconds); }

        void ApplyTOD(float k)
        {
            tod = k;
            sun.color = Color.Lerp(new Color(1f, 0.9f, 0.76f), new Color(0.5f, 0.6f, 0.95f), k);
            sun.intensity = Mathf.Lerp(1.45f, 0.3f, k); // gece: soğuk ay ışığı
            sun.transform.rotation = Quaternion.Euler(Mathf.Lerp(34, 38, k), Mathf.Lerp(-40, 150, k), 0);
            RenderSettings.ambientSkyColor = Color.Lerp(new Color(0.55f, 0.66f, 0.82f), new Color(0.07f, 0.08f, 0.17f), k);
            RenderSettings.ambientEquatorColor = Color.Lerp(new Color(0.6f, 0.58f, 0.5f), new Color(0.06f, 0.06f, 0.12f), k);
            RenderSettings.ambientGroundColor = Color.Lerp(new Color(0.36f, 0.33f, 0.26f), new Color(0.03f, 0.03f, 0.05f), k);
            RenderSettings.fogColor = Color.Lerp(new Color(0.86f, 0.82f, 0.74f), new Color(0.05f, 0.05f, 0.12f), k);
            PaintSky(k);
            fireBase = Mathf.Lerp(0.6f, 5.5f, k * k); fireLight.range = Mathf.Lerp(4, 18, k);
            fireLight.shadows = !low && fireShadowAllowed && k > 0.5f ? LightShadows.Hard : LightShadows.None;
            foreach (var m in windowMats) m.SetColor("_EmissionColor", new Color(1f, 0.52f, 0.18f) * (k * 1.5f)); // sıcak turuncu, beyaza patlamasın
            moon.SetActive(false);
            env?.ApplyTOD(k, sun);
            GradeTOD(k);
        }
        float fireBase = 0.6f;

        // ---------------------------------------------------------------- her kare
        void Update()
        {
            float t = Time.time, dt = Mathf.Min(0.1f, Time.deltaTime);
            if (todDur > 0)
            {
                float u = Mathf.Clamp01((t - todT0) / todDur); u = u * u * (3 - 2 * u);
                ApplyTOD(Mathf.Lerp(todFrom, todTo, u));
                if (t - todT0 >= todDur) todDur = -1;
            }
            fireLight.intensity = fireBase * (1 + Mathf.Sin(t * 11) * 0.08f + Mathf.Sin(t * 5.3f) * 0.06f);
            if (emberMat != null) emberMat.SetColor("_EmissionColor", Color.white * (1.3f + Mathf.Sin(t * 2.3f) * 0.35f + Mathf.Sin(t * 7.1f) * 0.2f));
            // alevler
            var camPos = cam.transform.position;
            foreach (var f in flames)
            {
                f.age += dt; if (f.age >= f.life) Spawn(f);
                float q = f.age / f.life, env = Mathf.Sin(Mathf.PI * Mathf.Min(1, q));
                f.mpb.SetFloat("_Alpha", Mathf.Min(1, env * 1.4f)); f.r.SetPropertyBlock(f.mpb);
                f.t.position = new Vector3(f.x, f.y + q * 0.12f, f.z);
                var look = camPos - f.t.position; look.y = 0; if (look.sqrMagnitude > 0.001f) f.t.rotation = Quaternion.LookRotation(-look);
                f.t.localScale = new Vector3(f.w * (0.7f + q * 0.4f), f.h * (0.75f + q * 0.4f), 1);
            }
            if (glow != null)
            {
                glow.position = new Vector3(0, -0.2f, 0); var lk = camPos - glow.position; glow.rotation = Quaternion.LookRotation(-lk); glow.localScale = new Vector3(1.4f, 1.4f, 1);
                glowMpb.SetVector("_Color", new Vector4(1f, 0.55f, 0.2f, (0.04f + tod * 0.12f) * (1 + Mathf.Sin(t * 9) * 0.12f)));
                glow.GetComponent<MeshRenderer>().SetPropertyBlock(glowMpb);
            }
            // köz, kıvılcım, duman
            fireAcc += dt * (low ? 5 : 10);
            while (fireAcc > 1) { fireAcc--; float r = Random.value * 0.16f, a = Random.value * 6.283f; Emit(new Vector3(Mathf.Cos(a) * r, 0.2f, Mathf.Sin(a) * r), new Vector3((Random.value - 0.5f) * 0.15f, 0.9f + Random.value * 0.7f, (Random.value - 0.5f) * 0.15f), 0.45f + Random.value * 0.35f, 0.3f, 0.05f, Random.value < 0.5f ? new Color(1, 0.54f, 0.16f) : new Color(1, 0.76f, 0.29f), 0, 0.5f); }
            emberAcc += dt * 7;
            while (emberAcc > 1) { emberAcc--; Emit(new Vector3((Random.value - 0.5f) * 0.25f, 0.4f, (Random.value - 0.5f) * 0.25f), new Vector3((Random.value - 0.5f) * 0.3f, 0.7f + Random.value * 0.8f, (Random.value - 0.5f) * 0.3f), 2 + Random.value, 0.05f, 0.02f, new Color(1, 0.7f, 0.35f), 0, 1); }
            smokeAcc += dt * (low ? 1.5f : 3);
            while (smokeAcc > 1) { smokeAcc--; Emit(new Vector3((Random.value - 0.5f) * 0.2f, 1.1f, (Random.value - 0.5f) * 0.2f), new Vector3(0.12f + Random.value * 0.1f, 0.45f + Random.value * 0.2f, (Random.value - 0.5f) * 0.1f), 3.2f, 0.35f, 1.6f, new Color(0.35f, 0.34f, 0.33f), 0, 0.1f * (1 - tod * 0.7f)); }
            chimAcc += dt * chimneys.Count * 0.9f;
            while (chimAcc > 1 && chimneys.Count > 0) { chimAcc--; var cp = chimneys[Random.Range(0, chimneys.Count)]; Emit(cp, new Vector3(0.15f + Random.value * 0.1f, 0.5f + Random.value * 0.3f, (Random.value - 0.5f) * 0.1f), 4f, 0.4f, 2.2f, new Color(0.45f, 0.44f, 0.43f), 0, 0.12f * (1 - tod * 0.6f)); }
            DrawParticles(dt, camPos);
            UpdateAtmos(t, camPos);
            env?.Update(cam, tod);
            if (Sfx.I != null) Sfx.I.SetTod(tod);
            // yıldızlar
            if (tod > 0.2f) { for (int i = 0; i < starC.Length; i++) starC[i].w = Mathf.Clamp01((tod - 0.2f) * 1.6f) * (0.5f + 0.5f * Mathf.Sin(t * 2 + i)); starMpb.SetVectorArray("_Color", starC); if (starMat == null) starMat = new Material(GlowMat()); var rp = new RenderParams(starMat) { matProps = starMpb, worldBounds = new Bounds(Vector3.zero, Vector3.one * 1000) }; FaceAll(starM, camPos); Graphics.RenderMeshInstanced(rp, Quad(), 0, starM); }
            // çimen
            foreach (var b in grassBatches) for (int c = 0; c < b.m.Length; c++) { if (b.count[c] == 0) continue; grassRp.matProps = b.mpb[c]; Graphics.RenderMeshInstanced(grassRp, b.mesh, 0, b.m[c], b.count[c]); }
        }

        Material GlowMat() { if (glowMat == null) { glowMat = new Material(matGlow); glowMat.SetTexture("_MainTex", dotTex); glowMat.enableInstancing = true; } return glowMat; }
        Mesh Quad()
        {
            if (dotQuad != null) return dotQuad;
            dotQuad = new Mesh(); dotQuad.vertices = new[] { new Vector3(-0.5f, -0.5f, 0), new Vector3(0.5f, -0.5f, 0), new Vector3(-0.5f, 0.5f, 0), new Vector3(0.5f, 0.5f, 0) };
            dotQuad.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1) }; dotQuad.triangles = new[] { 0, 2, 1, 1, 2, 3 }; dotQuad.RecalculateBounds(); return dotQuad;
        }
        void FaceAll(Matrix4x4[] ms, Vector3 camPos)
        {
            for (int i = 0; i < ms.Length; i++) { Vector3 p = ms[i].GetColumn(3); float s = ms[i].GetColumn(0).magnitude; ms[i] = Matrix4x4.TRS(p, Quaternion.LookRotation(p - camPos), Vector3.one * s); }
        }

        void DrawParticles(float dt, Vector3 camPos)
        {
            if (pmpb == null) pmpb = new MaterialPropertyBlock();
            int n = 0; var rot = Quaternion.LookRotation(cam.transform.forward);
            for (int i = parts.Count - 1; i >= 0; i--)
            {
                var p = parts[i]; p.age += dt;
                if (p.age >= p.life) { parts.RemoveAt(i); continue; }
                float u = p.age / p.life;
                p.v.y -= p.g * dt; if (p.drag) p.v *= 1 - 1.2f * dt;
                p.p += p.v * dt;
                if (n < 1023)
                {
                    float sz = Mathf.Lerp(p.s0, p.s1, u);
                    pm[n] = Matrix4x4.TRS(p.p, rot, Vector3.one * sz);
                    float a = p.a * (u < 0.15f ? u / 0.15f : 1 - (u - 0.15f) / 0.85f);
                    pc[n] = new Vector4(p.c.r, p.c.g, p.c.b, a); n++;
                }
            }
            if (n == 0) return;
            pmpb.SetVectorArray("_Color", pc);
            var rp = new RenderParams(GlowMat()) { matProps = pmpb, worldBounds = new Bounds(Vector3.zero, Vector3.one * 100) };
            Graphics.RenderMeshInstanced(rp, Quad(), 0, pm, n);
        }

        // telefon zorlanırsa: parlama kapanır, çimenin yarısı çizilmez
        // Uyarlamalı kalite: kare hızı düşükse adım adım hafifler, yüksekse geri yükselir.
        // 0 en iyi … 4 en hafif. Çözünürlük, çimen, gölge, parlama ve kenar yumuşatma ayarlanır.
        public bool degraded => level > 0;
        public int level = -1;
        int[][] grassFull;
        // çözünürlük en son düşer ve en fazla %80'e iner (bulanıklık en göze batan kayıp)
        static readonly float[] SCALE = { 1f, 1f, 1f, 0.9f, 0.8f };
        static readonly int[] GRASS = { 100, 85, 65, 50, 40 };
        public void SetLevel(int L)
        {
            L = Mathf.Clamp(L, 0, 4); if (L == level) return; level = L;
            var urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (urp != null)
            {
                urp.renderScale = SCALE[L];
                urp.msaaSampleCount = L == 0 ? 2 : 1;
                urp.shadowDistance = L >= 3 ? 18 : L >= 2 ? 24 : 30;
            }
            if (volume != null) volume.enabled = L <= 2;
            if (bloom != null) bloom.active = L == 0;
            sun.shadows = L >= 4 ? LightShadows.Hard : L >= 2 ? LightShadows.Hard : LightShadows.Soft;
            if (grassFull == null) grassFull = grassBatches.ConvertAll(b => (int[])b.count.Clone()).ToArray();
            for (int k = 0; k < grassBatches.Count; k++) for (int c = 0; c < grassBatches[k].count.Length; c++) grassBatches[k].count[c] = grassFull[k][c] * GRASS[L] / 100;
            Debug.Log("[Zuplay] kalite seviyesi " + L);
        }
        public void Degrade() { SetLevel(level + 1); }
        public bool fireShadowAllowed => level <= 1;
    }
}
