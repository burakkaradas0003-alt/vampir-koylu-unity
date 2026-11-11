// Aşama 4 — atmosfer: gece/gündüz renk derecelendirmesi, fener haleleri ve yer ışık havuzları,
// ateş böcekleri, yer sisi ve köy dekorları (kuyu, araba, odun yığını, çitler, basamak taşları).
// Hepsi örneklenmiş (instanced) tek çizimlerle; ek gerçek ışık yok (mobilde pahalı).
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace VK
{
    public partial class World
    {
        ColorAdjustments grade; Vignette vign;
        readonly List<Vector3> lanternPos = new List<Vector3>();

        // ------------------------------------------------------------ renk derecelendirme
        void GradeTOD(float k)
        {
            if (grade != null)
            {
                grade.colorFilter.Override(Color.Lerp(new Color(1f, 0.97f, 0.92f), new Color(0.8f, 0.88f, 1.08f), k));
                grade.saturation.Override(Mathf.Lerp(12f, -6f, k));
                grade.contrast.Override(Mathf.Lerp(12f, 18f, k));
                grade.postExposure.Override(Mathf.Lerp(0.25f, 0.15f, k));
            }
            if (vign != null) vign.intensity.Override(Mathf.Lerp(0.26f, 0.4f, k));
        }

        // ------------------------------------------------------------ dekorlar
        void BuildProps(Transform root, Material stone, Material timber, Material roof)
        {
            var darkWood = Lit(new Color(0.26f, 0.18f, 0.12f));
            var water = Lit(new Color(0.06f, 0.1f, 0.16f), 0.92f);
            var sack = Lit(new Color(0.72f, 0.62f, 0.45f));
            var iron = Lit(new Color(0.18f, 0.18f, 0.2f), 0.4f);

            // kuyu
            var w = Place(root, 1.05f, 12.6f);
            Prim(PrimitiveType.Cylinder, stone, new Vector3(0, 0.45f, 0), new Vector3(1.6f, 0.45f, 1.6f), w);
            Prim(PrimitiveType.Cylinder, water, new Vector3(0, 0.88f, 0), new Vector3(1.25f, 0.02f, 1.25f), w);
            foreach (float x in new[] { -0.72f, 0.72f }) Prim(PrimitiveType.Cube, timber, new Vector3(x, 1.4f, 0), new Vector3(0.13f, 1.9f, 0.13f), w);
            Prim(PrimitiveType.Cylinder, darkWood, new Vector3(0, 1.75f, 0), new Vector3(0.09f, 0.78f, 0.09f), w, new Vector3(0, 0, 90));
            var wr = MeshGO("kuyu çatısı", Prism(2.1f, 0.65f, 1.5f), roof, w); wr.transform.localPosition = new Vector3(0, 2.3f, 0);
            Prim(PrimitiveType.Cylinder, darkWood, new Vector3(0.12f, 1.25f, 0), new Vector3(0.26f, 0.15f, 0.26f), w);
            Prim(PrimitiveType.Cylinder, iron, new Vector3(0.12f, 1.5f, 0), new Vector3(0.015f, 0.25f, 0.015f), w);

            // at arabası
            var c = Place(root, 3.1f, 13.2f, 70);
            Prim(PrimitiveType.Cube, timber, new Vector3(0, 0.78f, 0), new Vector3(1.9f, 0.12f, 1.15f), c);
            foreach (float z in new[] { -0.55f, 0.55f }) Prim(PrimitiveType.Cube, timber, new Vector3(0, 0.98f, z), new Vector3(1.9f, 0.3f, 0.06f), c);
            Prim(PrimitiveType.Cube, timber, new Vector3(-0.93f, 0.98f, 0), new Vector3(0.06f, 0.3f, 1.15f), c);
            foreach (float z in new[] { -0.66f, 0.66f })
            {
                Prim(PrimitiveType.Cylinder, darkWood, new Vector3(0.1f, 0.48f, z), new Vector3(0.95f, 0.05f, 0.95f), c, new Vector3(90, 0, 0));
                Prim(PrimitiveType.Cylinder, iron, new Vector3(0.1f, 0.48f, z * 1.04f), new Vector3(0.2f, 0.06f, 0.2f), c, new Vector3(90, 0, 0));
            }
            foreach (float z in new[] { -0.4f, 0.4f }) Prim(PrimitiveType.Cube, darkWood, new Vector3(1.6f, 0.42f, z), new Vector3(1.6f, 0.07f, 0.07f), c, new Vector3(0, 0, -22));
            var ico = Ico(1);
            for (int i = 0; i < 3; i++) { var s = MeshGO("çuval", ico, sack, c); s.transform.localPosition = new Vector3(-0.5f + i * 0.45f, 1.1f, (i % 2 - 0.5f) * 0.3f); s.transform.localScale = new Vector3(0.36f, 0.28f, 0.3f); }

            // odun yığını
            var p = Place(root, 5.2f, 12.4f, 90);
            for (int row = 0; row < 3; row++) for (int k = 0; k < 4 - row; k++)
                    Prim(PrimitiveType.Cylinder, darkWood, new Vector3(-0.42f + k * 0.28f + row * 0.14f, 0.13f + row * 0.24f, 0), new Vector3(0.24f, 0.7f, 0.24f), p, new Vector3(90, 0, 0));
            Prim(PrimitiveType.Cylinder, timber, new Vector3(0.9f, 0.25f, 0.2f), new Vector3(0.45f, 0.25f, 0.45f), p); // kütük

            // çitler
            foreach (float a0 in new[] { 0.0f, 2.1f, 4.15f })
            {
                const float d = 14.6f; int posts = 6;
                Vector3 prev = default;
                for (int k = 0; k < posts; k++)
                {
                    float a = a0 + (k - (posts - 1) / 2f) * 0.055f;
                    var q = new Vector3(Mathf.Cos(a) * d, 0, Mathf.Sin(a) * d);
                    Prim(PrimitiveType.Cube, timber, q + new Vector3(0, 0.5f, 0), new Vector3(0.1f, 1f, 0.1f), root, new Vector3(0, -a * Mathf.Rad2Deg, R(-4, 4)));
                    if (k > 0)
                    {
                        var mid = (prev + q) / 2; float len = (q - prev).magnitude; float yaw = Mathf.Atan2(q.x - prev.x, q.z - prev.z) * Mathf.Rad2Deg;
                        foreach (float y in new[] { 0.42f, 0.78f }) Prim(PrimitiveType.Cube, timber, mid + new Vector3(0, y, 0), new Vector3(0.05f, 0.08f, len + 0.08f), root, new Vector3(0, yaw, 0));
                    }
                    prev = q;
                }
            }

            // evlere giden basamak taşları: tek birleşik mesh (tek çizim)
            var parts = new List<CombineInstance>(); var stoneMesh = Ico(1);
            for (int i = 0; i < 8; i++)
            {
                float a = (i + 0.5f) / 8 * Mathf.PI * 2 + 0.2f, end = 17 + (i % 3) * 1.6f - 2.3f;
                for (float r = 6.2f; r < end; r += 0.85f)
                {
                    float j = Mathf.Sin(r * 3.1f + i) * 0.22f;
                    var pos = new Vector3(Mathf.Cos(a) * r - Mathf.Sin(a) * j, 0.01f, Mathf.Sin(a) * r + Mathf.Cos(a) * j);
                    parts.Add(new CombineInstance { mesh = stoneMesh, transform = Matrix4x4.TRS(pos, Quaternion.Euler(0, R(0, 360), 0), new Vector3(R(0.32f, 0.42f), 0.05f, R(0.26f, 0.36f))) });
                }
            }
            var path = new Mesh { indexFormat = IndexFormat.UInt32 }; path.CombineMeshes(parts.ToArray(), true, true); path.RecalculateBounds();
            var pg = MeshGO("basamak taşları", path, Lit(new Color(0.55f, 0.53f, 0.5f), 0.05f), root, false);
            pg.GetComponent<MeshRenderer>().receiveShadows = true;
        }

        Transform Place(Transform root, float angle, float dist, float yawExtra = 0)
        {
            var t = new GameObject("dekor").transform; t.SetParent(root, false);
            t.localPosition = new Vector3(Mathf.Cos(angle) * dist, 0, Mathf.Sin(angle) * dist);
            t.localEulerAngles = new Vector3(0, -angle * Mathf.Rad2Deg + yawExtra, 0);
            return t;
        }

        // ------------------------------------------------------------ fener haleleri, ateş böcekleri, yer sisi
        Matrix4x4[] haloM, poolM, flyM, mistM; Vector4[] haloC, poolC, flyC, mistC;
        Vector3[] flyP; float[] flyPh; Vector3[] mistP; Vector2[] mistS;
        MaterialPropertyBlock haloMpb, poolMpb, flyMpb, mistMpb; Material mistMat, haloMat, poolMat, flyMat;

        void BuildAtmos()
        {
            int nl = lanternPos.Count;
            haloM = new Matrix4x4[nl]; haloC = new Vector4[1023]; poolM = new Matrix4x4[nl]; poolC = new Vector4[1023];
            haloMpb = new MaterialPropertyBlock(); poolMpb = new MaterialPropertyBlock();
            for (int i = 0; i < nl; i++) poolM[i] = Matrix4x4.TRS(new Vector3(lanternPos[i].x, 0.04f, lanternPos[i].z), Quaternion.Euler(90, 0, 0), Vector3.one * 5.5f);

            int nf = low ? 24 : 48; flyP = new Vector3[nf]; flyPh = new float[nf]; flyM = new Matrix4x4[nf]; flyC = new Vector4[1023]; flyMpb = new MaterialPropertyBlock();
            for (int i = 0; i < nf; i++) { float a = R(0, 6.283f), d = R(7, 26); flyP[i] = new Vector3(Mathf.Cos(a) * d, R(0.4f, 2.2f), Mathf.Sin(a) * d); flyPh[i] = R(0, 100); }

            int nm = low ? 10 : 18; mistP = new Vector3[nm]; mistS = new Vector2[nm]; mistM = new Matrix4x4[nm]; mistC = new Vector4[nm]; mistMpb = new MaterialPropertyBlock();
            for (int i = 0; i < nm; i++) { float a = i / (float)nm * 6.283f + R(-0.15f, 0.15f), d = R(11, 32); var s = new Vector2(R(9, 16), R(1.8f, 3.2f)); mistS[i] = s; mistP[i] = new Vector3(Mathf.Cos(a) * d, s.y * 0.5f, Mathf.Sin(a) * d); }
            mistMat = new Material(Resources.Load<Material>("Mats/Cloud") ?? matGlow); mistMat.SetTexture("_MainTex", MistTex()); mistMat.enableInstancing = true;
        }

        static Texture2D MistTex()
        {
            const int W = 64, H = 32; var t = new Texture2D(W, H, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < H; y++) for (int x = 0; x < W; x++)
                {
                    float u = (x + 0.5f) / W * 2 - 1, v = (y + 0.5f) / H * 2 - 1;
                    float n = Mathf.PerlinNoise(x * 0.09f, y * 0.16f) * 0.5f + 0.5f;
                    float a = Mathf.Clamp01(1 - (u * u + v * v)); a = a * a * n;
                    t.SetPixel(x, y, new Color(1, 1, 1, a));
                }
            t.Apply(); return t;
        }

        void UpdateAtmos(float t, Vector3 camPos)
        {
            if (haloM == null) return;
            float night = Mathf.Clamp01((tod - 0.35f) / 0.65f);
            // her grup kendi malzeme kopyasıyla çizilir: aynı karede aynı malzemeyle birden çok örnekli çizim WebGL'de birbirini eziyor
            if (haloMat == null) { haloMat = new Material(GlowMat()); poolMat = new Material(GlowMat()); flyMat = new Material(GlowMat()); }
            zstep = 71;
            var q = Quad();
            if (night > 0.01f && haloM.Length > 0)
            {
                for (int i = 0; i < haloM.Length; i++)
                {
                    var p = lanternPos[i]; float fl = 1 + Mathf.Sin(t * 6 + i * 1.7f) * 0.05f;
                    haloM[i] = Matrix4x4.TRS(p, Quaternion.LookRotation(p - camPos), Vector3.one * 1.9f * fl);
                    haloC[i] = new Vector4(1f, 0.72f, 0.38f, 0.42f * night * fl);
                    poolC[i] = new Vector4(1f, 0.62f, 0.3f, 0.16f * night * fl);
                }
                haloMpb.SetVectorArray("_Color", haloC); poolMpb.SetVectorArray("_Color", poolC);
                var b = new Bounds(Vector3.zero, Vector3.one * 60);
                Graphics.RenderMeshInstanced(new RenderParams(poolMat) { matProps = poolMpb, worldBounds = b, shadowCastingMode = ShadowCastingMode.Off }, q, 0, poolM);
                Graphics.RenderMeshInstanced(new RenderParams(haloMat) { matProps = haloMpb, worldBounds = b, shadowCastingMode = ShadowCastingMode.Off }, q, 0, haloM);
            }
            zstep = 72;
            // ateş böcekleri: yalnız gece, yumuşak yanıp sönme
            if (night > 0.2f)
            {
                for (int i = 0; i < flyP.Length; i++)
                {
                    float ph = flyPh[i], s = t * 0.35f + ph;
                    var p = flyP[i] + new Vector3(Mathf.Sin(s * 1.3f) * 1.2f, Mathf.Sin(s * 2.1f) * 0.35f, Mathf.Cos(s * 0.9f) * 1.2f);
                    float blink = Mathf.Pow(Mathf.Clamp01(Mathf.Sin(t * 1.6f + ph * 3) * 0.5f + 0.5f), 3);
                    flyM[i] = Matrix4x4.TRS(p, Quaternion.LookRotation(p - camPos), Vector3.one * 0.16f);
                    flyC[i] = new Vector4(0.8f, 1f, 0.45f, (night - 0.2f) * 1.25f * blink);
                }
                flyMpb.SetVectorArray("_Color", flyC);
                Graphics.RenderMeshInstanced(new RenderParams(flyMat) { matProps = flyMpb, worldBounds = new Bounds(Vector3.zero, Vector3.one * 80), shadowCastingMode = ShadowCastingMode.Off }, q, 0, flyM);
            }
            zstep = 73;
            // yer sisi: gece belirgin, gündüz çok hafif pus
            if (mistMat != null)
            {
                Color mc = Color.Lerp(new Color(0.92f, 0.93f, 0.95f), new Color(0.32f, 0.38f, 0.55f), tod);
                float ma = Mathf.Lerp(0.07f, 0.22f, tod);
                for (int i = 0; i < mistP.Length; i++)
                {
                    var p = mistP[i] + new Vector3(Mathf.Sin(t * 0.05f + i) * 1.5f, 0, Mathf.Cos(t * 0.04f + i * 2) * 1.5f);
                    var look = p - camPos; look.y = 0; if (look.sqrMagnitude < 0.01f) look = Vector3.forward;
                    float near = Mathf.Clamp01((look.magnitude - 4f) / 5f); // kameraya çok yakınsa söner (patlama olmasın)
                    mistM[i] = Matrix4x4.TRS(p, Quaternion.LookRotation(look), new Vector3(mistS[i].x, mistS[i].y, 1));
                    mistC[i] = new Vector4(mc.r, mc.g, mc.b, ma * near);
                }
                mistMpb.SetVectorArray("_Color", mistC);
                Graphics.RenderMeshInstanced(new RenderParams(mistMat) { matProps = mistMpb, worldBounds = new Bounds(Vector3.zero, Vector3.one * 100), shadowCastingMode = ShadowCastingMode.Off }, q, 0, mistM);
            }
        }
    }
}
