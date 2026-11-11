// Uzak ortam: gerçek gökyüzü (atmosfer saçılımlı), katman katman sıradağlar (yakın: ormanlık, uzak: kayalık ve karlı),
// sisle derinlik, yavaş süzülen bulutlar ve köyü çevreleyen yoğun orman. Hepsi telefonu yormayacak kadar hafif.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace VK
{
    public class Env
    {
        readonly World W;
        Material sky;
        readonly List<Material> mountainMats = new List<Material>();
        Material cloudMat; Matrix4x4[] cloudM; Vector4[] cloudC; Vector3[] cloudP; float[] cloudS; MaterialPropertyBlock cloudMpb; Mesh quad;
        System.Random rnd = new System.Random(21);
        float R(float a, float b) => a + (float)rnd.NextDouble() * (b - a);

        public Env(World w) { W = w; }

        static float Noise1(float x, float seed) => Mathf.PerlinNoise(x, seed);
        static float Ridge(float x, float seed, int oct)
        {
            float v = 0, a = 0.55f, f = 1;
            for (int i = 0; i < oct; i++) { float n = 1 - Mathf.Abs(Mathf.PerlinNoise(x * f, seed + i * 9.1f) * 2 - 1); v += n * n * a; a *= 0.5f; f *= 2.1f; }
            return v;
        }

        public void Build(Transform parent, Camera cam)
        {
            // gökyüzü
            var proto = Resources.Load<Material>("Mats/Sky");
            if (proto != null)
            {
                sky = new Material(proto);
                RenderSettings.skybox = sky;
                cam.clearFlags = CameraClearFlags.Skybox;
            }
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.006f;

            // sıradağlar: 3 katman; uzaklaştıkça yükselir, maviye çalar ve sisle silikleşir
            var grassProto = Resources.Load<Material>("Mats/Grass");
            var layers = new[]
            {
                new { r = 78f, depth = 26f, h = 16f, seed = 3.1f, low = new Color(0.1f, 0.19f, 0.1f), mid = new Color(0.14f, 0.24f, 0.13f), high = new Color(0.24f, 0.29f, 0.2f), snow = 2f },
                new { r = 118f, depth = 40f, h = 34f, seed = 7.7f, low = new Color(0.12f, 0.2f, 0.13f), mid = new Color(0.27f, 0.3f, 0.31f), high = new Color(0.42f, 0.44f, 0.48f), snow = 0.8f },
                new { r = 175f, depth = 60f, h = 70f, seed = 12.3f, low = new Color(0.22f, 0.27f, 0.33f), mid = new Color(0.38f, 0.42f, 0.5f), high = new Color(0.56f, 0.6f, 0.68f), snow = 0.58f },
            };
            foreach (var L in layers)
            {
                var m = Range(L.r, L.depth, L.h, L.seed, L.low, L.mid, L.high, L.snow);
                var go = new GameObject("Sıradağ"); go.transform.SetParent(parent, false);
                go.AddComponent<MeshFilter>().sharedMesh = m;
                var mr = go.AddComponent<MeshRenderer>();
                var mat = new Material(grassProto); mat.enableInstancing = false; mat.SetFloat("_Wind", 0); mat.SetColor("_Tint", Color.white);
                mr.sharedMaterial = mat; mr.shadowCastingMode = ShadowCastingMode.Off; mr.receiveShadows = false;
                mountainMats.Add(mat);
            }

            // bulutlar
            var tex = CloudTex();
            cloudMat = new Material(Resources.Load<Material>("Mats/Cloud") ?? Resources.Load<Material>("Mats/Glow"));
            cloudMat.SetTexture("_MainTex", tex); cloudMat.enableInstancing = true;
            int n = 16; cloudM = new Matrix4x4[n]; cloudC = new Vector4[n]; cloudP = new Vector3[n]; cloudS = new float[n]; cloudMpb = new MaterialPropertyBlock();
            for (int i = 0; i < n; i++)
            {
                float a = R(0, 6.283f), d = R(170, 260);
                cloudP[i] = new Vector3(Mathf.Cos(a) * d, R(70, 115), Mathf.Sin(a) * d); cloudS[i] = R(55, 110);
            }
            quad = new Mesh();
            quad.vertices = new[] { new Vector3(-0.5f, -0.25f, 0), new Vector3(0.5f, -0.25f, 0), new Vector3(-0.5f, 0.25f, 0), new Vector3(0.5f, 0.25f, 0) };
            quad.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1) };
            quad.triangles = new[] { 0, 2, 1, 1, 2, 3 }; quad.RecalculateBounds();
            // ay: keskin kenarlı disk + hafif hale
            moonQuad = new Mesh();
            moonQuad.vertices = new[] { new Vector3(-0.5f, -0.5f, 0), new Vector3(0.5f, -0.5f, 0), new Vector3(-0.5f, 0.5f, 0), new Vector3(0.5f, 0.5f, 0) };
            moonQuad.uv = quad.uv; moonQuad.triangles = quad.triangles; moonQuad.RecalculateBounds();
            moonMat = new Material(cloudMat); moonMat.SetTexture("_MainTex", MoonTex()); moonMpb = new MaterialPropertyBlock();
        }
        Mesh moonQuad; Material moonMat; MaterialPropertyBlock moonMpb;
        static Texture2D MoonTex()
        {
            int s = 128; var t = new Texture2D(s, s, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < s; y++) for (int x = 0; x < s; x++)
                {
                    float dx = (x - 63.5f) / 63.5f, dy = (y - 63.5f) / 63.5f, d = Mathf.Sqrt(dx * dx + dy * dy);
                    float disc = Mathf.Clamp01((0.42f - d) * 60);
                    float halo = Mathf.Clamp01(1 - d) * Mathf.Clamp01(1 - d) * 0.35f;
                    float crater = 0.85f + 0.15f * Mathf.PerlinNoise(x * 0.08f + 5, y * 0.08f + 2);
                    float a = Mathf.Max(disc, halo);
                    var c = Color.Lerp(new Color(0.7f, 0.78f, 1f), new Color(1f, 0.98f, 0.9f) * crater, disc);
                    c.a = a; t.SetPixel(x, y, c);
                }
            t.Apply(true); return t;
        }

        // bir sıradağ halkası: önden bakan, sırtları kırık, yüksekliğe göre renklenen şerit
        Mesh Range(float r0, float depth, float hMax, float seed, Color low, Color mid, Color high, float snowAt)
        {
            int seg = 360, rows = 10;
            var v = new List<Vector3>(); var c = new List<Color>(); var tri = new List<int>();
            for (int i = 0; i <= seg; i++)
            {
                float u = i / (float)seg, a = u * Mathf.PI * 2;
                float ridge = Ridge(u * 9 + seed, seed, 5);
                float peak = hMax * (0.25f + ridge * 1.05f) * (0.7f + 0.3f * Mathf.Sin(a * 3 + seed));
                for (int j = 0; j <= rows; j++)
                {
                    float t = j / (float)rows;
                    float prof = Mathf.Sin(t * Mathf.PI * 0.5f);             // önden yükselen yamaç
                    float rough = ((Mathf.PerlinNoise(u * 60 + seed, t * 6) - 0.5f) * 0.1f + (Mathf.PerlinNoise(u * 180 + seed, t * 14) - 0.5f) * 0.05f) * hMax * t;
                    float y = peak * prof + rough - 2f;
                    float rr = r0 + depth * t + (Mathf.PerlinNoise(u * 20, seed) - 0.5f) * 6;
                    v.Add(new Vector3(Mathf.Cos(a) * rr, y, Mathf.Sin(a) * rr));
                    float hn = Mathf.Clamp01((y + 2) / (hMax * 1.2f));
                    Color col = hn < 0.45f ? Color.Lerp(low, mid, hn / 0.45f) : Color.Lerp(mid, high, (hn - 0.45f) / 0.55f);
                    if (hn > snowAt) col = Color.Lerp(col, new Color(0.92f, 0.94f, 0.97f), Mathf.Clamp01((hn - snowAt) * 8));
                    float n = Mathf.PerlinNoise(u * 140 + seed, t * 9);
                    col *= 0.86f + n * 0.26f;
                    col.a = 1; c.Add(col);
                }
            }
            for (int i = 0; i < seg; i++) for (int j = 0; j < rows; j++)
                {
                    int a = i * (rows + 1) + j, b = a + rows + 1;
                    tri.Add(a); tri.Add(a + 1); tri.Add(b); tri.Add(b); tri.Add(a + 1); tri.Add(b + 1);
                }
            var m = new Mesh { indexFormat = IndexFormat.UInt32 };
            m.SetVertices(v); m.SetColors(c); m.SetTriangles(tri, 0); m.RecalculateNormals(); m.RecalculateBounds();
            return m;
        }

        static Texture2D CloudTex()
        {
            int w = 128, h = 64; var t = new Texture2D(w, h, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < h; y++) for (int x = 0; x < w; x++)
                {
                    float u = x / (float)w, v = y / (float)h;
                    float n = 0, a = 0.5f, f = 3;
                    for (int o = 0; o < 5; o++) { n += Mathf.PerlinNoise(u * f + 3.3f, v * f * 0.5f + 1.1f) * a; a *= 0.5f; f *= 2; }
                    float dx = (u - 0.5f) * 2, dy = (v - 0.45f) * 2.2f;
                    float mask = Mathf.Clamp01(1 - (dx * dx + dy * dy));
                    float al = Mathf.Clamp01((n * mask - 0.22f) * 2.4f);
                    t.SetPixel(x, y, new Color(1, 1, 1, al));
                }
            t.Apply(true); return t;
        }

        public void ApplyTOD(float k, Light sun)
        {
            // dağlar güneşin sıcak rengini almasın: hafif soğuk ton (uzak hava perspektifi)
            var cool = Color.Lerp(new Color(0.86f, 0.93f, 1.05f), new Color(0.35f, 0.4f, 0.6f), k);
            if (sky != null)
            {
                sky.SetFloat("_Exposure", Mathf.Lerp(0.85f, 0.05f, k));
                sky.SetFloat("_AtmosphereThickness", Mathf.Lerp(1.3f, 0.6f, k));
                sky.SetColor("_SkyTint", Color.Lerp(new Color(0.46f, 0.48f, 0.5f), new Color(0.16f, 0.2f, 0.4f), k));
                sky.SetColor("_GroundColor", Color.Lerp(new Color(0.42f, 0.4f, 0.37f), new Color(0.05f, 0.05f, 0.09f), k));
                sky.SetFloat("_SunSize", Mathf.Lerp(0.035f, 0.0f, k));
                RenderSettings.sun = sun;
            }
            // uzak sis gökyüzünün ufkuyla aynı renkte: dağlar ufukta erir
            // gündüz gri sis, gece koyu lacivert ve yoğun: köyün ötesi karanlığa gömülür
            RenderSettings.fogColor = Color.Lerp(new Color(0.62f, 0.66f, 0.7f), new Color(0.03f, 0.04f, 0.09f), k);
            RenderSettings.fogDensity = Mathf.Lerp(0.012f, 0.024f, k);
            foreach (var m in mountainMats) m.SetColor("_Tint", cool);
        }

        public void Update(Camera cam, float tod)
        {
            if (cloudMat == null) return;
            float t = Time.time; var camPos = cam.transform.position;
            for (int i = 0; i < cloudP.Length; i++)
            {
                var p = cloudP[i] + new Vector3(t * 1.2f, 0, t * 0.4f);
                var rot = Quaternion.LookRotation(p - camPos);
                cloudM[i] = Matrix4x4.TRS(p, rot, Vector3.one * cloudS[i]);
                float a = Mathf.Lerp(0.95f, 0.18f, tod);
                cloudC[i] = new Vector4(Mathf.Lerp(0.78f, 0.32f, tod), Mathf.Lerp(0.8f, 0.36f, tod), Mathf.Lerp(0.84f, 0.5f, tod), a); // gündüz kurşuni bulut
            }
            cloudMpb.SetVectorArray("_Color", cloudC);
            var rp = new RenderParams(cloudMat) { matProps = cloudMpb, worldBounds = new Bounds(Vector3.zero, Vector3.one * 800), shadowCastingMode = ShadowCastingMode.Off };
            Graphics.RenderMeshInstanced(rp, quad, 0, cloudM);
            if (tod > 0.25f)
            {
                var mp = camPos + new Vector3(-0.42f, 0.55f, 0.72f).normalized * 220f;
                moonMpb.SetVector("_Color", new Vector4(1, 1, 1, Mathf.Clamp01((tod - 0.25f) * 2.5f)));
                var mrp = new RenderParams(moonMat) { matProps = moonMpb, worldBounds = new Bounds(mp, Vector3.one * 60), shadowCastingMode = ShadowCastingMode.Off };
                Graphics.RenderMesh(mrp, moonQuad, 0, Matrix4x4.TRS(mp, Quaternion.LookRotation(mp - camPos), Vector3.one * 34f));
            }
        }
    }
}
