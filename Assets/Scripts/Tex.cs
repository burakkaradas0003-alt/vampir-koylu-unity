// Çalışma anında üretilen dokular: sıva, ahşap, kiremit, taş, kum (+ kabartma haritaları).
// Dışarıdan dosya gerektirmez; her açılışta aynı görünür (sabit tohum).
using UnityEngine;

namespace VK
{
    public static class Tex
    {
        static float N(float x, float y, float s) => Mathf.PerlinNoise(x * s + 13.1f, y * s + 7.7f);
        static float Fbm(float x, float y, float s, int o = 4)
        {
            float v = 0, a = 0.5f, f = s;
            for (int i = 0; i < o; i++) { v += a * N(x + i * 31.7f, y + i * 17.3f, f); f *= 2; a *= 0.5f; }
            return v;
        }

        public struct Pair { public Texture2D col, nrm; }

        // yükseklikten kabartma (normal) haritası
        static Texture2D Normal(float[] h, int s, float k)
        {
            var t = new Texture2D(s, s, TextureFormat.RGBA32, true, true) { wrapMode = TextureWrapMode.Repeat, anisoLevel = 8, filterMode = FilterMode.Trilinear };
            var px = new Color32[s * s];
            for (int y = 0; y < s; y++) for (int x = 0; x < s; x++)
            {
                float l = h[y * s + (x - 1 + s) % s], r = h[y * s + (x + 1) % s], d = h[((y - 1 + s) % s) * s + x], u = h[((y + 1) % s) * s + x];
                var n = new Vector3((l - r) * k, (d - u) * k, 1).normalized;
                px[y * s + x] = new Color32((byte)((n.x * 0.5f + 0.5f) * 255), (byte)((n.y * 0.5f + 0.5f) * 255), (byte)((n.z * 0.5f + 0.5f) * 255), 255);
            }
            t.SetPixels32(px); t.Apply(true); return t;
        }
        static Texture2D MakeTex(UnityEngine.Color[] c, int s)
        {
            var t = new Texture2D(s, s, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Repeat, anisoLevel = 8, filterMode = FilterMode.Trilinear };
            t.SetPixels(c); t.Apply(true); return t;
        }

        public static Pair Plaster(int s = 256)
        {
            var c = new UnityEngine.Color[s * s]; var h = new float[s * s];
            for (int y = 0; y < s; y++) for (int x = 0; x < s; x++)
            {
                float u = x / (float)s, v = y / (float)s;
                float n = Fbm(u, v, 6), st = Mathf.Clamp01((Fbm(u, v, 2.2f, 3) - 0.55f) * 3) * (1 - v * 0.6f);
                float g = 0.9f + (n - 0.5f) * 0.12f - st * 0.12f;
                c[y * s + x] = new UnityEngine.Color(g, g * 0.975f, g * 0.93f);
                h[y * s + x] = n;
            }
            return new Pair { col = MakeTex(c, s), nrm = Normal(h, s, 2.5f) };
        }

        public static Pair Wood(int s = 256)
        {
            var c = new UnityEngine.Color[s * s]; var h = new float[s * s];
            for (int y = 0; y < s; y++) for (int x = 0; x < s; x++)
            {
                float u = x / (float)s, v = y / (float)s;
                float grain = Mathf.Sin((u * 40 + Fbm(u, v, 3) * 6) * Mathf.PI) * 0.5f + 0.5f;
                float n = Fbm(u * 0.3f, v * 4, 8);
                float g = 0.2f + grain * 0.06f + (n - 0.5f) * 0.1f;
                c[y * s + x] = new UnityEngine.Color(g * 1.25f, g * 0.85f, g * 0.6f);
                h[y * s + x] = grain * 0.6f + n * 0.4f;
            }
            return new Pair { col = MakeTex(c, s), nrm = Normal(h, s, 3f) };
        }

        public static Pair RoofTiles(UnityEngine.Color baseCol, int s = 256)
        {
            var c = new UnityEngine.Color[s * s]; var h = new float[s * s]; int rows = 8, cols = 6;
            for (int y = 0; y < s; y++) for (int x = 0; x < s; x++)
            {
                float u = x / (float)s, v = y / (float)s;
                int row = (int)(v * rows); float off = (row % 2) * 0.5f / cols;
                float fu = (u + off) * cols % 1f, fv = v * rows % 1f;
                float edge = Mathf.Clamp01(fu * 8) * Mathf.Clamp01((1 - fu) * 8);
                float lip = Mathf.Lerp(0.55f, 1f, fv);
                float n = Fbm(u, v, 10, 3);
                int id = row * 7 + (int)((u + off) * cols);
                float tone = 0.85f + (Mathf.Abs(Mathf.Sin(id * 12.9898f)) % 1f) * 0.25f;
                float g = lip * edge * tone * (0.85f + n * 0.3f);
                c[y * s + x] = baseCol * g;
                h[y * s + x] = lip * edge;
            }
            return new Pair { col = MakeTex(c, s), nrm = Normal(h, s, 4f) };
        }

        public static Pair Stone(int s = 256)
        {
            var c = new UnityEngine.Color[s * s]; var h = new float[s * s];
            // basit hücre (Voronoi) deseni: taş blokları
            var pts = new Vector2[24]; var rnd = new System.Random(11);
            for (int i = 0; i < pts.Length; i++) pts[i] = new Vector2((float)rnd.NextDouble(), (float)rnd.NextDouble());
            for (int y = 0; y < s; y++) for (int x = 0; x < s; x++)
            {
                var p = new Vector2(x / (float)s, y / (float)s); float d1 = 9, d2 = 9; int id = 0;
                for (int i = 0; i < pts.Length; i++) for (int ox = -1; ox <= 1; ox++) for (int oy = -1; oy <= 1; oy++)
                        {
                            float d = (p - pts[i] - new Vector2(ox, oy)).magnitude;
                            if (d < d1) { d2 = d1; d1 = d; id = i; } else if (d < d2) d2 = d;
                        }
                float mortar = Mathf.Clamp01((d2 - d1) * 18);
                float n = Fbm(p.x, p.y, 9, 3), tone = 0.8f + (id * 0.137f % 0.3f);
                float g = Mathf.Lerp(0.3f, 0.62f * tone, mortar) * (0.85f + n * 0.3f);
                c[y * s + x] = new UnityEngine.Color(g, g * 0.97f, g * 0.92f);
                h[y * s + x] = mortar * 0.8f + n * 0.2f;
            }
            return new Pair { col = MakeTex(c, s), nrm = Normal(h, s, 5f) };
        }

        public static Pair Sand(int s = 256)
        {
            var c = new UnityEngine.Color[s * s]; var h = new float[s * s];
            for (int y = 0; y < s; y++) for (int x = 0; x < s; x++)
            {
                float u = x / (float)s, v = y / (float)s;
                float n = Fbm(u, v, 4, 3), fine = N(u, v, 12);
                float g = 0.83f + (n - 0.5f) * 0.14f + (fine - 0.5f) * 0.03f;
                c[y * s + x] = new UnityEngine.Color(g * 1.02f, g * 0.89f, g * 0.66f);
                h[y * s + x] = n * 0.6f + fine * 0.4f;
            }
            return new Pair { col = MakeTex(c, s), nrm = Normal(h, s, 0.8f) };
        }
    }
}
