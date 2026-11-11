// Oyun akışı (Agrou tarzı): rol kartı, muhtar seçimi, gece sırası, sabah, oylama, avcı, âşıklar, bitiş.
// Bu sürüm tek oyuncu + botlarla çalışır; çok oyunculu sürümde aynı olaylar sunucudan gelecek.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;

namespace VK
{
    public class Game : MonoBehaviour
    {
        public static float SPEED = 1;
        public static async Task Wait(float s) { await Awaitable.WaitForSecondsAsync(Mathf.Max(0.01f, s / SPEED)); }

        const float T_INTRO = 7, T_MAYOR = 30, T_DAY = 75, T_STEP = 20, T_WOLF = 25, T_BOTSTEP = 2.6f, T_PROMPT = 12;

        class Role { public string n, pl, ic, team, d; public Color c; }
        static Color Hex(string h) { ColorUtility.TryParseHtmlString(h, out var c); return c; }
        static readonly Dictionary<string, Role> ROLES = new Dictionary<string, Role>
        {
            { "vampir", new Role { n = "Vampir", pl = "Vampirler", ic = "vampir", team = "v", c = Hex("#FF4D6D"), d = "Geceleri diğer vampirlerle birlikte bir kurban seçersin. Gündüz köylü gibi davran, köyü ele geçir!" } },
            { "koylu", new Role { n = "Köylü", ic = "koylu", team = "k", c = Hex("#F5B942"), d = "Özel gücün yok ama oy hakkın var. Konuş, dinle ve vampirleri bul!" } },
            { "kahin", new Role { n = "Kâhin", ic = "kahin", team = "k", c = Hex("#B9A8FF"), d = "Her gece bir kişinin gerçek rolünü görürsün. Bildiklerini ne zaman söyleyeceğini iyi seç." } },
            { "koruma", new Role { n = "Koruma", ic = "koruma", team = "k", c = Hex("#4A8CFF"), d = "Her gece bir kişiyi (kendini de) vampirlerden korursun. Aynı kişiyi üst üste iki gece koruyamazsın." } },
            { "cadi", new Role { n = "Cadı", ic = "cadi", team = "k", c = Hex("#2BB673"), d = "Bir iyileştirme ve bir zehir iksirin var. Vampirlerin kurbanını görür, kurtarabilir ya da birini zehirleyebilirsin." } },
            { "avci", new Role { n = "Avcı", ic = "avci", team = "k", c = Hex("#E8955A"), d = "Öldüğünde son bir okla istediğin kişiyi yanında götürürsün." } },
            { "melek", new Role { n = "Aşk Meleği", ic = "melek", team = "k", c = Hex("#FF7AC8"), d = "İlk gece iki kişiyi âşık edersin. Biri ölürse diğeri de üzüntüden ölür. Âşıklar son ikiye kalırsa kazanır." } },
        };
        static readonly string[] DECK = { "vampir", "vampir", "kahin", "koruma", "cadi", "avci", "melek", "koylu", "koylu" };
        static readonly string[] NIGHT_ORDER = { "melek", "koruma", "kahin", "vampir", "cadi" };
        static readonly string[] BOT_NAMES = { "Mira", "Deniz", "Ece", "Kaan", "Selin", "Arda", "Zeynep", "Emre" };
        static readonly Color[] SKT = { Color.white, Hex("#E6C2A8"), Color.white, Hex("#B88A6A"), Hex("#FFF0E6"), Hex("#D2A888"), Color.white, Hex("#A07558"), Hex("#F0D8C4") };

        class Expr { public string type; public float until; }
        class GhostInfo { public Rig ch; public float born, a, r, sp, ph; }
        class Pl
        {
            public int i; public string n; public bool me; public string role; public bool alive = true; public Rig ch; public Vector3 seat; public float ang;
            public float lookYaw, headY, raise, sleep, slump, sleepTo, slumpTo, wave, talk, crownDrop; public int? pointAt;
            public GhostInfo ghost; public bool lover, claimed, reveal; public Dictionary<int, string> known = new Dictionary<int, string>();
            public float[] sus, trust; public Expr expr; public Hud.Tag tag; public int oi; public Color tint; public float size;
        }
        class Claim { public int by, target, day; public bool vamp; }

        // ---- durum
        const int N = 9; const float R = 4.3f;
        readonly List<Pl> P = new List<Pl>();
        Pl ME;
        string phase = "intro"; int day; int? mayor, wolfTarget, guardLast, guardT, poisonT; bool healTonight, healUsed, poisonUsed, locked, over, ghostCam, vampChatHint;
        float voteChangedAt;
        int[] lovers = new int[0]; readonly List<Claim> claims = new List<Claim>(); string countMode;
        Func<Pl, bool> selectable; Action<int> onSelect; readonly List<KeyValuePair<int, Color>> rings = new List<KeyValuePair<int, Color>>();
        int? lastSpeaker; float lastSpeakAt; int token;
        System.Random rng; bool AUTO; string MY_NAME = "Oyuncu";
        World W; Hud ui; Camera cam; Rig fpArm;
        float yaw, pitch = 0.17f, yawT, pitchT = 0.17f; Vector2? drag, downAt;

        float Rnd() => (float)rng.NextDouble();
        T Pick<T>(IList<T> a) => a[(int)(Rnd() * a.Count) % Math.Max(1, a.Count)];
        List<T> Shuffle<T>(List<T> a) { for (int i = a.Count - 1; i > 0; i--) { int j = (int)(Rnd() * (i + 1)); var t = a[i]; a[i] = a[j]; a[j] = t; } return a; }
        static float Now => Time.time;
        string Who(Pl p) => p.me ? "Sen" : p.n;
        List<Pl> Alive() => P.Where(p => p.alive).ToList();
        List<Pl> Others(Pl p) => Alive().Where(q => q != p).ToList();
        static bool IsV(Pl p) => p.role == "vampir";
        async void Later(float sec, Action fn) { int tk = token; await Wait(sec); if (tk == token && !over && this != null) fn(); }

        // ---------------------------------------------------------------- başlangıç
        public void Begin(World w, Hud h, string myName, bool auto, int seed)
        {
            W = w; ui = h; cam = w.cam; AUTO = auto; if (!string.IsNullOrEmpty(myName)) MY_NAME = myName.Length > 14 ? myName.Substring(0, 14) : myName;
            rng = new System.Random(seed);
            for (int i = 0; i < N; i++)
            {
                float a = Mathf.PI / 2 + i / (float)N * Mathf.PI * 2;
                var seat = new Vector3(Mathf.Cos(a) * R, 0, Mathf.Sin(a) * R);
                var p = new Pl { i = i, n = i == 0 ? MY_NAME : BOT_NAMES[i - 1], me = i == 0, seat = seat, ang = a, oi = i, tint = SKT[i % SKT.Length], size = 0.96f + ((i * 37) % 9) / 100f };
                p.ch = Rig.Make(p.oi, p.tint, p.size);
                p.ch.transform.position = seat; p.ch.transform.LookAt(new Vector3(0, 0, 0));
                if (p.me) p.ch.gameObject.SetActive(false); else p.tag = ui.MakeTag(p.n);
                P.Add(p);
            }
            ME = P[0];
            cam.transform.position = new Vector3(ME.seat.x * 1.04f, 1.45f, ME.seat.z * 1.04f);
            fpArm = Rig.Make(0, P[0].tint, 1, false, true); fpArm.transform.localScale = Vector3.one * 0.8f; fpArm.gameObject.SetActive(false);
            ui.onSend = SendChat; ui.onEmote = k => Emote(ME, k); ui.onBack = Exit; ui.onAgain = () => UnityEngine.SceneManagement.SceneManager.LoadScene(0);
            ui.onRoleChip = () => { if (ME.role != null) { var r = ROLES[ME.role]; ui.ShowCard(r.ic, r.n, r.team == "v" ? "VAMPİRLER" : "KÖY", r.c, r.d, "Kapat", null); } };
            Run();
        }

        void Exit() { Bridge.Send("{\"type\":\"exit\"}"); }

        // ---------------------------------------------------------------- konuşma, duygu, emoji
        void Say(Pl p, string text, string cls = "")
        {
            if (!p.alive && !p.me) return;
            ui.Log(p.n, text, cls);
            if (p.me) return;
            p.talk = 2.2f; lastSpeaker = p.i; lastSpeakAt = Now;
            ui.Bubble(p.tag, text);
        }
        static readonly Dictionary<string, string> EXPR_OF = new Dictionary<string, string> { { "wave", "happy" }, { "up", "happy" }, { "heart", "happy" }, { "laugh", "laugh" }, { "angry", "angry" }, { "down", "angry" }, { "cry", "sad" }, { "scream", "surprise" }, { "think", "think" }, { "kahin", "think" } };
        void Feel(Pl p, string type, float sec = 2.8f) { if (!p.alive && type != "surprise") return; p.expr = new Expr { type = type, until = Now + sec }; }
        void Emote(Pl p, string e)
        {
            if (!p.alive) return;
            if (e == "wave") p.wave = 1.4f;
            if (EXPR_OF.TryGetValue(e, out var ex)) Feel(p, ex);
            if (p.me) { ui.Toast("Sen bir tepki gönderdin"); return; }
            ui.Emote(p.tag, e);
        }

        // ---------------------------------------------------------------- oy sayımı
        Dictionary<int, int> Tally(Func<Pl, bool> filter, bool weighted)
        {
            var c = new Dictionary<int, int>();
            foreach (var p in Alive())
            {
                if (p.pointAt == null || (filter != null && !filter(p))) continue;
                int t = p.pointAt.Value; if (!P[t].alive) continue;
                c.TryGetValue(t, out int v); c[t] = v + (weighted && p.i == mayor ? 2 : 1);
            }
            return c;
        }
        static (int best, List<int> list) TopOf(Dictionary<int, int> c)
        {
            int best = -1; var list = new List<int>();
            foreach (var kv in c) { if (kv.Value > best) { best = kv.Value; list = new List<int> { kv.Key }; } else if (kv.Value == best) list.Add(kv.Key); }
            return (best, list);
        }

        // ---------------------------------------------------------------- botların aklı
        void InitMinds()
        {
            foreach (var p in P)
            {
                p.sus = P.Select(q => q == p ? 0 : 0.2f + Rnd() * 0.4f).ToArray();
                p.trust = P.Select(q => 0.4f + Rnd() * 0.5f).ToArray();
                if (IsV(p)) foreach (var q in P) if (IsV(q)) p.sus[q.i] = -1;
            }
        }
        void Bump(int target, float amt, int exceptBy) { foreach (var p in P) if (p != P[target] && p.i != exceptBy && !IsV(p)) p.sus[target] = Mathf.Min(1.5f, p.sus[target] + amt); }
        Pl MaxSus(Pl p, List<Pl> pool)
        {
            Pl best = null; float bv = -9;
            foreach (var q in pool) { if (q == p) continue; if (p.lover && q.lover) continue; float v = p.sus[q.i] + Rnd() * 0.15f; if (v > bv) { bv = v; best = q; } }
            return best;
        }
        int BotMayorVote(Pl p)
        {
            if (IsV(p) && Rnd() < 0.5f) { var mate = Alive().FirstOrDefault(q => IsV(q) && q != p); if (mate != null) return mate.i; }
            Pl best = null; float bv = -9; foreach (var q in Others(p)) { float v = 1 - p.sus[q.i] + Rnd() * 0.6f; if (v > bv) { bv = v; best = q; } }
            return best.i;
        }
        int BotDayVote(Pl p)
        {
            var pool = Others(p);
            var top = TopOf(Tally(null, true));
            if (IsV(p))
            {
                var vill = pool.Where(q => !IsV(q)).ToList();
                var claimer = claims.FirstOrDefault(c => P[c.by].alive && P[c.target].alive && IsV(P[c.target]) && c.vamp);
                if (claimer != null && Rnd() < 0.6f && vill.Contains(P[claimer.by])) return claimer.by;
                if (top.list.Count > 0 && Rnd() < 0.55f) { var t = top.list.Where(i => !IsV(P[i]) && i != p.i).Cast<int?>().FirstOrDefault(); if (t != null) return t.Value; }
                return (MaxSus(p, vill) ?? Pick(vill)).i;
            }
            var claim = claims.Where(c => P[c.target].alive && c.vamp && c.by != p.i && c.target != p.i).OrderByDescending(c => p.trust[c.by]).FirstOrDefault();
            if (claim != null && Rnd() < 0.35f + p.trust[claim.by] * 0.5f) return claim.target;
            if (top.list.Count > 0 && top.best >= 2 && Rnd() < 0.35f) { var t = top.list.Where(i => i != p.i && !(p.lover && P[i].lover)).Cast<int?>().FirstOrDefault(); if (t != null) return t.Value; }
            return (MaxSus(p, pool) ?? Pick(pool)).i;
        }
        int BotWolfVote(Pl p)
        {
            var mate = Alive().FirstOrDefault(q => IsV(q) && q != p && q.pointAt != null);
            if (mate != null && Rnd() < 0.75f) return mate.pointAt.Value;
            var vill = Alive().Where(q => !IsV(q)).ToList();
            var claimer = claims.FirstOrDefault(c => P[c.by].alive && !IsV(P[c.by]));
            if (claimer != null && Rnd() < 0.65f) return claimer.by;
            if (mayor != null && P[mayor.Value].alive && !IsV(P[mayor.Value]) && Rnd() < 0.35f) return mayor.Value;
            return Pick(vill).i;
        }

        // ---------------------------------------------------------------- konuşmalar
        static readonly Dictionary<string, string[]> L = new Dictionary<string, string[]>
        {
            { "dead", new[] { "Zavallı {x}...", "{x} gitti, kim yaptı bunu?", "Bu gece {x}'i kaybettik.", "Vampirler {x}'i seçti, demek ki bir şey biliyordu." } },
            { "nodead", new[] { "Bu gece kimse ölmedi!", "Şanslıyız, herkes hayatta.", "Biri bizi korudu galiba." } },
            { "acc", new[] { "Bence {x} çok şüpheli.", "{x} dün hiç konuşmadı.", "{x}'in hâli tavrı hiç hoşuma gitmiyor.", "Ben {x}'i göstereceğim.", "{x}, savunmanı duyalım!", "{x} sürekli konuyu değiştiriyor." } },
            { "def", new[] { "Ben masumum, yemin ederim!", "Neden ben? Ben köylüyüm!", "Asıl sen şüphelisin {x}!", "Beni suçlamak vampirin işine yarar.", "Hata yapıyorsunuz!" } },
            { "seerV", new[] { "Ben Kâhin'im! {x} VAMPİR!", "Kâhin benim, gece {x}'e baktım: vampir!", "Dinleyin! {x} vampir, gördüm!" } },
            { "seerK", new[] { "Kâhin olarak {x}'e baktım, temiz.", "{x} köylü, ona dokunmayın." } },
            { "agree", new[] { "Katılıyorum.", "Mantıklı.", "Ben de öyle düşünüyorum.", "Hmm, olabilir.", "Evet!" } },
            { "misc", new[] { "Sessiz olanlara dikkat edin.", "Muhtar ne düşünüyor?", "Acele etmeyelim.", "Vampirler aramızda, dikkatli olun!", "Herkes ne düşünüyor?" } },
            { "mayor", new[] { "{x} muhtar olsun, güveniyorum.", "Ben {x}'i seçiyorum.", "{x} iyi bir muhtar olur." } },
            { "wolf", new[] { "{x}'i alalım.", "Bu gece {x}.", "{x} tehlikeli, onu seçelim." } },
        };
        static string Fill(string s, string x) => s.Replace("{x}", x);
        static string TrLower(string s) => s.Replace('İ', 'i').Replace('I', 'ı').ToLowerInvariant();
        string Line(string k, string x = "") => Fill(Pick(L[k]), x);

        void BotDayChatter(float dur)
        {
            var bots = Alive().Where(p => !p.me).ToList();
            var seer = bots.FirstOrDefault(p => p.role == "kahin");
            if (seer != null)
            {
                var known = seer.known.Where(kv => P[kv.Key].alive).ToList();
                var v = known.Where(kv => kv.Value == "vampir").Select(kv => (int?)kv.Key).FirstOrDefault();
                if (v != null && Rnd() < 0.8f) Later(dur * (0.08f + Rnd() * 0.12f), () => ClaimRole(seer, v.Value, true));
                else if (known.Count > 0 && Rnd() < 0.3f) Later(dur * (0.1f + Rnd() * 0.2f), () => ClaimRole(seer, Pick(known).Key, false));
            }
            var fake = bots.FirstOrDefault(p => IsV(p) && !p.claimed);
            if (fake != null && day >= 1 && Rnd() < 0.18f)
            {
                var pool = Alive().Where(q => !IsV(q) && q != fake).ToList();
                if (pool.Count > 0) { var t = Pick(pool); Later(dur * (0.15f + Rnd() * 0.2f), () => ClaimRole(fake, t.i, true)); }
            }
            int n = 2 + (int)(Rnd() * 3);
            for (int k = 0; k < n && bots.Count > 0; k++)
            {
                var a = Pick(bots);
                Later(dur * (0.06f + Rnd() * 0.45f), () =>
                {
                    if (!a.alive) return;
                    var pool = Others(a).Where(q => !(IsV(a) && IsV(q))).ToList();
                    var t = MaxSus(a, pool); if (t == null) return;
                    Say(a, Line("acc", t.n)); Bump(t.i, 0.08f * (0.5f + Rnd()), a.i); Feel(a, "angry", 2.5f); Feel(t, Rnd() < 0.5f ? "surprise" : "angry", 2.5f);
                    if (Rnd() < 0.5f) Emote(t, Pick(new[] { "angry", "scream", "think" }));
                    if (!t.me && t.alive && Rnd() < 0.7f) Later(1.6f + Rnd() * 1.5f, () => Say(t, Line("def", a.n)));
                    var ags = bots.Where(q => q != a && q != t).ToList();
                    if (ags.Count > 0 && Rnd() < 0.5f) { var ag = Pick(ags); Later(2.4f + Rnd() * 2, () => { Say(ag, Line("agree")); Feel(ag, "happy", 1.8f); if (Rnd() < 0.4f) Emote(ag, "up"); }); }
                });
            }
            if (Rnd() < 0.6f && bots.Count > 0) { var m = Pick(bots); Later(dur * (0.3f + Rnd() * 0.3f), () => { if (m.alive) Say(m, Line("misc")); }); }
        }

        void ClaimRole(Pl p, int target, bool vamp)
        {
            if (!p.alive || !P[target].alive) return;
            p.claimed = true;
            Say(p, Line(vamp ? "seerV" : "seerK", P[target].n));
            Emote(p, "kahin");
            var prev = claims.FirstOrDefault(c => c.by != p.i);
            if (prev != null) foreach (var q in P) { q.trust[p.i] *= 0.7f; q.trust[prev.by] *= 0.7f; }
            claims.Add(new Claim { by = p.i, target = target, vamp = vamp, day = day });
            if (vamp) { foreach (var q in P) if (!IsV(q) && q != P[target]) q.sus[target] = Mathf.Min(1.5f, q.sus[target] + 0.45f * q.trust[p.i]); }
            else foreach (var q in P) if (!IsV(q)) q.sus[target] *= 0.4f;
            if (!P[target].me && Rnd() < 0.8f) Later(1.5f + Rnd(), () => Say(P[target], vamp ? Line("def", p.n) : "Teşekkürler!"));
        }

        void SendChat(string t)
        {
            if (!ME.alive) return;
            bool night = phase.StartsWith("night");
            if (night && !IsV(ME)) return;
            Say(ME, t, night ? "wolf" : "");
            var low = TrLower(t);
            var named = P.FirstOrDefault(p => !p.me && p.alive && low.Contains(TrLower(p.n)));
            if (night)
            {
                var mate = Alive().FirstOrDefault(q => IsV(q) && !q.me);
                if (mate != null) Later(1.4f, () => Say(mate, named != null ? $"Tamam, {named.n} olsun." : Pick(new[] { "Tamam.", "Kimi seçelim?", "Sessiz ol, bizi duyacaklar." }), "wolf"));
                if (named != null && mate != null && phase == "night-vampir") Later(1.8f, () => mate.pointAt = named.i);
                return;
            }
            if (named != null)
            {
                Bump(named.i, low.Contains("vampir") ? 0.15f : 0.05f, ME.i);
                Later(1.3f + Rnd() * 1.2f, () => { Say(named, Line("def", ME.n)); if (Rnd() < 0.5f) Emote(named, "angry"); });
                if (low.Contains("vampir") && (low.Contains("kâhin") || low.Contains("kahin"))) claims.Add(new Claim { by = ME.i, target = named.i, vamp = true, day = day });
            }
            else if (Rnd() < 0.4f) { var bs = Alive().Where(p => !p.me).ToList(); if (bs.Count > 0) { var b = Pick(bs); Later(1.5f, () => Say(b, Line("agree"))); } }
        }

        // ---------------------------------------------------------------- ölüm ve hayaletler
        async Task KillAnim(Pl p, string cause)
        {
            var pos = p.seat + Vector3.up * 1.2f;
            if (cause == "night") W.Burst(pos, new Color(0.7f, 0.06f, 0.16f), 26, 0.6f, 0.12f, 1.1f, 1.5f);
            if (cause == "poison") W.Burst(pos, new Color(0.55f, 0.88f, 0.29f), 30, 0.3f, 0.16f, 1.4f, 0, 0.6f);
            if (cause == "grief") W.Burst(pos + Vector3.up * 0.5f, new Color(1f, 0.44f, 0.68f), 18, 0.5f, 0.2f, 1.6f, 1.2f);
            if (cause == "exec") for (int k = 0; k < 6; k++) W.Burst(p.seat + Vector3.up * (0.3f + k * 0.25f), new Color(1f, 0.82f, 0.48f), 10, 0.4f, 0.14f, 1.3f);
            await Wait(cause == "exec" ? 1.1f : 0.4f);
            p.slumpTo = 1;
            await Wait(1.3f);
            p.ch.body.SetActive(false);
            W.Burst(p.seat + Vector3.up * 0.9f, new Color(0.75f, 0.83f, 1f), 18, 0.5f, 0.2f, 1.2f);
            var gh = Rig.Make(p.oi, p.tint, p.size, true);
            gh.transform.position = p.seat + Vector3.up * 0.1f; gh.transform.LookAt(new Vector3(0, 0.1f, 0));
            p.ghost = new GhostInfo { ch = gh, born = Now, a = p.ang + 0.15f, r = 7.4f + Rnd() * 1.4f, sp = (Rnd() < 0.5f ? -1 : 1) * (0.05f + Rnd() * 0.04f), ph = Rnd() * 6 };
            if (p.me) { gh.gameObject.SetActive(false); BecomeGhost(); }
        }
        void BecomeGhost()
        {
            ghostCam = true;
            ui.Toast("Öldün — artık bir hayaletsin. Oyunu izleyebilirsin.", 4);
            ui.InputState(false, "Hayaletler konuşamaz");
            Bridge.Send("{\"type\":\"dead\"}");
        }

        async Task KillMany(List<(int i, string cause)> list)
        {
            var queue = new Queue<(int i, string cause)>(list);
            while (queue.Count > 0 && !over)
            {
                var (i, cause) = queue.Dequeue();
                var p = P[i]; if (!p.alive) continue;
                p.alive = false; p.pointAt = null;
                string txt = p.me
                    ? cause switch { "night" => "Gece saldırıya uğradın", "poison" => "Zehirlenerek öldün", "exec" => "Köy kararıyla öldürüldün", "grief" => "Acına dayanamadın", _ => "Avcı'nın okuyla vuruldun" }
                    : cause switch { "night" => $"{p.n} gece saldırıya uğradı", "poison" => $"{p.n} zehirlenerek öldü", "exec" => $"{p.n} köy kararıyla öldürüldü", "grief" => $"{p.n} acısına dayanamadı", _ => $"{p.n} Avcı'nın okuyla vuruldu" };
                ui.Log("", txt, "sys");
                Feel(p, cause == "grief" ? "sad" : "surprise", 2.5f);
                foreach (var q in Alive()) if (!q.me) { var qq = q; Later(0.3f + Rnd() * 0.9f, () => Feel(qq, Rnd() < 0.6f ? "sad" : "surprise", 3)); }
                Sfx.Play("death");
                var anim = KillAnim(p, cause);
                await ui.Banner(cause == "grief" ? "broken" : "skull", Who(p), txt, 2.2f);
                await anim;
                if (p.lover) { var o = P.FirstOrDefault(q => q.lover && q != p && q.alive); if (o != null) { await Wait(0.4f); queue.Enqueue((o.i, "grief")); } }
                if (p.role == "avci" && cause != "grief") { var t = await HunterShot(p); if (t != null) queue.Enqueue((t.Value, "shot")); }
                if (mayor == p.i) await PassMayor(p);
            }
        }

        async Task<int?> HunterShot(Pl h)
        {
            var pool = Alive().Where(q => q != h).ToList();
            if (pool.Count == 0) return null;
            int? t;
            if (h.me && !AUTO) { ui.Eyes(false); t = (await Choose("Son okun!", "avci", "Avcı olarak ölüyorsun. Yanında götürmek istediğin kişiye dokun.", T_PROMPT, q => q.alive && !q.me, Hex("#E8955A"), "Vur"))?.FirstOrDefault(); }
            else t = (MaxSus(h, pool.Where(q => !(IsV(h) && IsV(q))).ToList()) ?? Pick(pool)).i;
            if (t == null) return null;
            await ui.Banner("avci", h.me ? "Son okunu attın!" : h.n + " son okunu attı!", "Hedef: " + Who(P[t.Value]), 1.6f);
            Vector3 from = h.seat + Vector3.up * 1.2f, to = P[t.Value].seat + Vector3.up * 1.2f;
            for (int k = 0; k <= 12; k++) W.Burst(Vector3.Lerp(from, to, k / 12f), new Color(1, 0.85f, 0.63f), 2, 0.1f, 0.1f, 0.5f);
            W.Burst(to, new Color(1, 0.35f, 0.23f), 20, 1, 0.14f, 0.8f);
            await Wait(0.5f);
            return t;
        }

        async Task PassMayor(Pl old)
        {
            var pool = Alive();
            if (pool.Count == 0) { mayor = null; return; }
            int? n;
            if (old.me && !AUTO) n = (await Choose("Muhtarlığı devret", "crown", "Muhtarlığı kime bırakmak istiyorsun?", T_PROMPT, q => q.alive && !q.me, Hex("#F5C542"), "Devret"))?.FirstOrDefault();
            else n = pool.OrderBy(a => old.sus[a.i]).First().i;
            if (n == null) n = Pick(pool).i;
            SetMayor(n.Value);
            await ui.Banner("crown", P[n.Value].me ? "Muhtarlık sana geçti" : "Muhtarlık " + P[n.Value].n + "'e geçti", "", 2);
        }
        void SetMayor(int i)
        {
            if (mayor != null) P[mayor.Value].ch.crown.gameObject.SetActive(false);
            mayor = i; var p = P[i];
            p.ch.crown.gameObject.SetActive(true); p.crownDrop = 1; Feel(p, "happy", 3.5f); Sfx.Play("fanfare"); if (p.tag != null) ui.PunchTag(p.tag);
            W.Burst(p.seat + Vector3.up * 2.1f, new Color(0.96f, 0.77f, 0.26f), 30, 0.8f, 0.12f, 1.3f, 0.6f);
        }

        // ---------------------------------------------------------------- oyuncu seçimi
        class Extra { public string t, style; public Action<List<int>, Action<int[]>, Action> f; }
        Task<int[]> Choose(string title, string icon, string desc, float sec, Func<Pl, bool> filter, Color color, string confirm = "Onayla", int multi = 1, params Extra[] extra)
        {
            var tcs = new TaskCompletionSource<int[]>();
            var sel = new List<int>(); bool done = false;
            void Finish(int[] v) { if (done) return; done = true; selectable = null; rings.Clear(); ME.pointAt = null; ui.HideAction(); tcs.TrySetResult(v); }
            void Render()
            {
                rings.Clear(); foreach (var i in sel) rings.Add(new KeyValuePair<int, Color>(i, color));
                ME.pointAt = sel.Count > 0 ? sel[sel.Count - 1] : (int?)null;
                var names = string.Join(" & ", sel.Select(i => P[i].me ? "Sen" : P[i].n));
                var btns = extra.Select(x => new Hud.Btn_ { t = x.t, style = x.style, f = () => x.f(sel, Finish, Render) }).ToList();
                btns.Add(new Hud.Btn_ { t = confirm, style = "on", dis = sel.Count != multi, f = () => Finish(sel.ToArray()) });
                ui.ShowAction(icon, title, desc + (names.Length > 0 ? $"\n<b><color=#FFFFFF>{names}</color></b>" : ""), btns.ToArray());
            }
            selectable = filter;
            onSelect = i => { int k = sel.IndexOf(i); if (k >= 0) { sel.RemoveAt(k); Sfx.Play("unselect"); } else { if (sel.Count >= multi) sel.RemoveAt(0); sel.Add(i); Sfx.Play("select"); if (P[i].tag != null) ui.PunchTag(P[i].tag); } Render(); };
            Render();
            ui.SetTimer(sec / SPEED);
            TimedThen(sec, () => done, () => Finish(sel.Count == multi ? sel.ToArray() : null));
            return tcs.Task;
        }
        async void TimedThen(float sec, Func<bool> early, Action then) { await Timed(sec, early); then(); }

        int timerId;
        async Task Timed(float sec, Func<bool> early)
        {
            int id = ++timerId;
            float end = Now + sec / SPEED; ui.SetTimerEnd(end);
            bool shortened = false;
            while (Now < end && !over)
            {
                if (!shortened && early != null && early()) { shortened = true; end = Mathf.Min(end, Now + 2.5f / SPEED); if (id == timerId) ui.SetTimerEnd(end); }
                await Awaitable.WaitForSecondsAsync(0.1f);
            }
            if (id == timerId) ui.SetTimer(0);
        }

        // ---------------------------------------------------------------- oylama
        async Task<(Dictionary<int, int> counts, int best, List<int> list)> VotePhase(string kind, float sec, string title, string sub)
        {
            token++;
            countMode = kind; locked = false;
            foreach (var p in P) p.pointAt = null;
            ui.Phase(title, sub, sec / SPEED);
            var voters = Alive();
            foreach (var p in voters)
            {
                if (p.me && !AUTO) continue;
                Func<Pl, int> chooser = kind == "mayor" ? (Func<Pl, int>)BotMayorVote : BotDayVote;
                var pp = p;
                Later(sec * (kind == "mayor" ? 0.15f + Rnd() * 0.45f : 0.35f + Rnd() * 0.4f), () =>
                {
                    if (!pp.alive) return;
                    pp.pointAt = chooser(pp);
                    if (kind == "mayor" && Rnd() < 0.35f) Say(pp, Line("mayor", P[pp.pointAt.Value].n));
                });
                if (Rnd() < 0.3f) Later(sec * (0.8f + Rnd() * 0.1f), () => { if (pp.alive) pp.pointAt = chooser(pp); });
            }
            if (ME.alive && !AUTO)
            {
                void Render()
                {
                    ui.ShowAction(kind == "mayor" ? "crown" : "hand", kind == "mayor" ? "Muhtar seçimi" : "Oylama",
                        ME.pointAt == null ? "Oy vermek istediğin kişiye dokun." : $"Oyun: <b><color=#FFFFFF>{P[ME.pointAt.Value].n}</color></b>\nGeri almak için tekrar dokun, değiştirmek için başkasına dokun.");
                }
                selectable = q => q.alive && !q.me;
                onSelect = i => { ME.pointAt = ME.pointAt == i ? null : (int?)i; voteChangedAt = Now; Sfx.Play(ME.pointAt == null ? "unselect" : "select"); if (ME.pointAt != null) ui.PunchTag(P[i].tag); Render(); };
                Render();
            }
            else if (!ME.alive) ui.ShowAction("ghost", "Hayaletsin", "Yaşayanların tartışmasını izliyorsun.");
            // herkes oy verdiyse ve oyuncu birkaç saniyedir fikrini değiştirmediyse süre kısalır
            await Timed(sec, () => voters.All(p => !p.alive || p.pointAt != null) && (!ME.alive || AUTO || Now - voteChangedAt > 6 / SPEED));
            selectable = null;
            var c = Tally(null, kind != "mayor");
            var r = TopOf(c);
            return (c, r.best, r.list);
        }

        // ---------------------------------------------------------------- akış
        async Task Intro()
        {
            var deck = Shuffle(DECK.ToList());
            for (int i = 0; i < N; i++) P[i].role = deck[i];
            InitMinds();
            var r = ROLES[ME.role];
            ui.RoleChip(r.ic, r.n, r.c);
            var desc = r.d;
            if (IsV(ME)) desc += " Diğer vampir: " + string.Join(", ", P.Where(q => IsV(q) && !q.me).Select(q => q.n)) + ".";
            ui.Phase("VAMPİR KÖYLÜ", "Rolün dağıtılıyor...");
            await Wait(0.7f);
            bool closed = false;
            ui.ShowCard(r.ic, r.n, r.team == "v" ? "VAMPİRLER" : "KÖY", r.c, desc, "Hazırım", () => closed = true);
            float until = Now + T_INTRO / SPEED;
            while (!closed && Now < until) await Awaitable.NextFrameAsync();
            ui.HideCard();
            ui.Log("", "Rolün: " + r.n, "sys");
        }

        async Task MayorElection()
        {
            phase = "mayor";
            await ui.Banner("sun", "Köye hoş geldin", "Önce bir muhtar seçin. Muhtarın oyu iki sayılır ve eşitlikte son sözü o söyler.", 3);
            var r = await VotePhase("mayor", T_MAYOR, "MUHTAR SEÇİMİ", "Muhtar olmasını istediğin kişiyi göster");
            int m = r.list.Count > 0 ? Pick(r.list) : Pick(Alive()).i;
            foreach (var p in P) p.pointAt = null; countMode = null; ui.HideAction();
            SetMayor(m);
            await ui.Banner("crown", P[m].me ? "Muhtar sensin!" : P[m].n + " muhtar seçildi!", r.best > 0 ? r.best + " oy" : "", 2.6f);
            ui.Log("", P[m].me ? "Muhtar oldun — oyun iki sayılır" : P[m].n + " muhtar oldu", "sys");
        }

        async Task Night(int n)
        {
            token++;
            phase = "night"; day = n; wolfTarget = null; guardT = null; healTonight = false; poisonT = null;
            ui.HideAction();
            ui.Phase("GECE " + n, "Köy uyuyor...");
            W.SetTOD(1, 3.5f / SPEED); Sfx.Play("night");
            await ui.Banner("moon", "Gece " + n, "Herkes gözlerini kapatsın...", 2);
            while (W.TodBusy) await Awaitable.NextFrameAsync();
            foreach (var p in P) p.sleepTo = 1;
            if (ME.alive) ui.Eyes(true, "moon", "Köy uyuyor...", "Gözlerin kapalı");
            await Wait(1.2f);
            foreach (var role in NIGHT_ORDER)
            {
                if (over) return;
                if (role == "melek" && n != 1) continue;
                await NightStep(role, n);
            }
            ui.Eyes(false); ui.HideAction(); rings.Clear(); ME.pointAt = null;
        }

        async Task NightStep(string role, int n)
        {
            token++;
            var R0 = ROLES[role];
            var actors = Alive().Where(p => p.role == role).ToList();
            phase = "night-" + role;
            var nm = R0.pl ?? R0.n;
            ui.Sub(nm + " uyanıyor...");
            if (actors.Count == 0) { if (ME.alive) ui.Eyes(true, R0.ic, nm + " uyanıyor...", "Gözlerin kapalı"); await Wait(T_BOTSTEP); return; }
            bool human = actors.Contains(ME) && !AUTO;
            if (!human)
            {
                if (ME.alive) ui.Eyes(true, R0.ic, nm + " uyanıyor...", "Gözlerin kapalı");
                await BotNight(role, actors, n);
                await Wait(T_BOTSTEP);
                return;
            }
            ui.Eyes(false);
            if (role == "vampir") { await HumanWolves(actors, n); return; }
            if (role == "melek")
            {
                var v = await Choose("Aşk Meleği", "melek", "Âşık edeceğin iki kişiye dokun.", T_STEP, q => q.alive && !q.me, Hex("#FF7AC8"), "Ok at", 2,
                    new Extra { t = "Beni de ekle", style = "p", f = (sel, fin, render) => { if (!sel.Contains(ME.i)) { if (sel.Count >= 2) sel.RemoveAt(0); sel.Add(ME.i); render(); } } });
                LinkLovers(v ?? Shuffle(Others(ME).Select(q => q.i).ToList()).Take(2).ToArray());
            }
            else if (role == "koruma")
            {
                var ex = guardLast != ME.i ? new[] { new Extra { t = "Kendimi koru", style = "p", f = (sel, fin, render) => fin(new[] { ME.i }) } } : new Extra[0];
                var v = (await Choose("Koruma", "koruma", "Bu gece korumak istediğin kişiye dokun." + (guardLast != null ? $" (Dün: {(P[guardLast.Value].me ? "sen" : P[guardLast.Value].n)})" : ""), T_STEP, q => q.alive && q.i != guardLast, Hex("#4A8CFF"), "Koru", 1, ex))?.FirstOrDefault();
                guardT = v; guardLast = v;
                if (v != null) { W.Burst(P[v.Value].seat + Vector3.up * 1.2f, Hex("#4A8CFF"), 20, 0.5f, 0.16f); ui.Toast(P[v.Value].me ? "Kendini koruyorsun" : P[v.Value].n + "'i koruyorsun"); }
            }
            else if (role == "kahin")
            {
                var v = (await Choose("Kâhin", "kahin", "Rolünü görmek istediğin kişiye dokun.", T_STEP, q => q.alive && !q.me, Hex("#B9A8FF"), "Bak"))?.FirstOrDefault();
                if (v != null) await SeerReveal(ME, v.Value);
            }
            else if (role == "cadi") await HumanWitch();
        }

        async Task BotNight(string role, List<Pl> actors, int n)
        {
            var p = actors[0];
            if (role == "melek") { var pool = Alive(); var a = Rnd() < 0.3f ? p : Pick(pool.Where(q => q != p).ToList()); var b = Pick(pool.Where(q => q != a).ToList()); LinkLovers(new[] { a.i, b.i }); }
            if (role == "koruma")
            {
                var pool = Alive().Where(q => q.i != guardLast).ToList();
                var claimer = claims.Select(c => P[c.by]).FirstOrDefault(q => q.alive && q.i != guardLast);
                Pl t = claimer != null && Rnd() < 0.55f ? claimer : (mayor != null && P[mayor.Value].alive && mayor != guardLast && Rnd() < 0.35f ? P[mayor.Value] : (Rnd() < 0.2f && p.i != guardLast ? p : Pick(pool)));
                guardT = t.i; guardLast = t.i;
            }
            if (role == "kahin") { var pool = Others(p).Where(q => !p.known.ContainsKey(q.i)).ToList(); if (pool.Count > 0) { var q = Pick(pool); p.known[q.i] = q.role; p.sus[q.i] = IsV(q) ? 1.5f : 0; } }
            if (role == "vampir")
            {
                bool visible = IsV(ME) && ME.alive;
                foreach (var v in actors) { v.sleepTo = 0; await Wait(0.5f); v.pointAt = BotWolfVote(v); }
                if (visible) await Wait(1.2f);
                var r = TopOf(Tally(q => IsV(q), false));
                wolfTarget = r.list.Count > 0 ? Pick(r.list) : Pick(Alive().Where(q => !IsV(q)).ToList()).i;
                await Wait(0.6f);
                foreach (var v in actors) { v.pointAt = null; v.sleepTo = 1; }
            }
            if (role == "cadi")
            {
                var v = wolfTarget;
                if (v != null && !healUsed && (v == p.i || (claims.Any(c => c.by == v) && Rnd() < 0.75f) || (v == mayor && Rnd() < 0.5f) || Rnd() < 0.25f)) { healUsed = true; healTonight = true; }
                if (!poisonUsed && n >= 2) { var t = MaxSus(p, Others(p)); if (t != null && p.sus[t.i] > 0.95f && Rnd() < 0.45f) { poisonUsed = true; poisonT = t.i; } }
            }
        }

        async Task HumanWolves(List<Pl> actors, int n)
        {
            token++;
            countMode = "wolf";
            var mates = actors.Where(q => !q.me).ToList();
            ui.Phase("GECE " + n, "Vampirler kurbanını seçiyor", T_WOLF / SPEED);
            foreach (var m in mates) { var mm = m; mm.sleepTo = 0; Later(T_WOLF * (0.25f + Rnd() * 0.35f), () => { mm.pointAt = BotWolfVote(mm); if (Rnd() < 0.6f) Say(mm, Line("wolf", P[mm.pointAt.Value].n), "wolf"); }); }
            if (!vampChatHint) { vampChatHint = true; ui.Toast("Vampir sohbeti açık — sadece vampirler görür"); }
            ui.InputState(true, "Vampirlere yaz...");
            void Render()
            {
                ui.ShowAction("vampir", "Avını seç", (ME.pointAt == null ? "Isıracağın kişiye dokun. Geri almak için tekrar dokun. Diğer vampirler kimi gösterdiğini görür." : $"Hedef: <b><color=#FFFFFF>{P[ME.pointAt.Value].n}</color></b>") + (mates.Count > 0 ? "\nDiğer vampir: " + string.Join(", ", mates.Select(m => m.n)) : ""),
                    new Hud.Btn_[0]);
            }
            locked = false;
            selectable = q => q.alive && !IsV(q);
            onSelect = i => { ME.pointAt = ME.pointAt == i ? null : (int?)i; voteChangedAt = Now; Sfx.Play(ME.pointAt == null ? "unselect" : "select", 1, 0.8f); Render(); };
            Render();
            await Timed(T_WOLF, () => ME.pointAt != null && mates.All(m => m.pointAt != null) && Now - voteChangedAt > 6 / SPEED);
            selectable = null;
            var r = TopOf(Tally(q => IsV(q), false));
            wolfTarget = r.list.Count > 0 ? Pick(r.list) : Pick(Alive().Where(q => !IsV(q)).ToList()).i;
            ui.Toast("Kurban: " + P[wolfTarget.Value].n);
            await Wait(1.2f);
            foreach (var v in actors) { v.pointAt = null; v.sleepTo = 1; }
            countMode = null; ui.InputState(true, "Bir şey yaz...");
            ui.HideAction();
        }

        Task HumanWitch()
        {
            var tcs = new TaskCompletionSource<bool>();
            var v = wolfTarget; var vn = v == null ? null : P[v.Value];
            bool done = false;
            void Fin() { if (done) return; done = true; selectable = null; rings.Clear(); ME.pointAt = null; ui.HideAction(); tcs.TrySetResult(true); }
            void Main()
            {
                rings.Clear(); if (vn != null) rings.Add(new KeyValuePair<int, Color>(v.Value, Hex("#FF4D6D")));
                ui.ShowAction("cadi", "Cadı", vn != null ? $"Vampirler bu gece <b><color=#FF8FA3>{(vn.me ? "SENİ" : vn.n)}</color></b> seçti." : "Bu gece kimse saldırıya uğramadı.",
                    new Hud.Btn_ { t = "İyileştir", style = "g", dis = healUsed || vn == null, f = () => { healUsed = true; healTonight = true; Sfx.Play("heal"); ui.Toast(vn.me ? "Kendini iyileştirdin" : vn.n + "'i iyileştirdin"); W.Burst(vn.seat + Vector3.up * 1.2f, Hex("#4FD99A"), 24, 0.6f, 0.15f); Main(); } },
                    new Hud.Btn_ { t = "Zehirle", style = "r", dis = poisonUsed, f = Poison },
                    new Hud.Btn_ { t = "Bitti", style = "m", f = Fin });
            }
            void Poison()
            {
                selectable = q => q.alive && !q.me;
                onSelect = i =>
                {
                    ME.pointAt = i; rings.Clear(); if (vn != null) rings.Add(new KeyValuePair<int, Color>(v.Value, Hex("#FF4D6D"))); rings.Add(new KeyValuePair<int, Color>(i, Hex("#8BE04A")));
                    ui.ShowAction("skull", "Zehir", $"Zehirlenecek: <b><color=#FFFFFF>{P[i].n}</color></b>",
                        new Hud.Btn_ { t = "Zehirle", style = "r", f = () => { poisonUsed = true; poisonT = i; ui.Toast(P[i].n + " zehirlendi"); selectable = null; ME.pointAt = null; Main(); } },
                        new Hud.Btn_ { t = "Geri", style = "m", f = () => { selectable = null; ME.pointAt = null; Main(); } });
                };
                ui.ShowAction("skull", "Zehir", "Zehirlemek istediğin kişiye dokun.", new Hud.Btn_ { t = "Geri", style = "m", f = () => { selectable = null; Main(); } });
            }
            Main();
            ui.Sub("Cadı uyanıyor"); ui.SetTimer(T_STEP / SPEED);
            TimedThen(T_STEP, () => done, Fin);
            return tcs.Task;
        }

        async Task SeerReveal(Pl seer, int i)
        {
            var q = P[i]; seer.known[i] = q.role;
            var r = ROLES[q.role];
            ui.Seer(q.tag, q.n + ": " + r.n, 3.2f); Sfx.Play("magic");
            W.Burst(q.seat + Vector3.up * 1.9f, Hex("#B9A8FF"), 26, 0.5f, 0.14f, 1.4f);
            ui.Log("", q.n + ": " + r.n, "sys");
            await Wait(3.2f);
        }

        void LinkLovers(int[] ab)
        {
            if (ab == null || ab.Length < 2) return;
            int a = ab[0], b = ab[1];
            lovers = new[] { a, b }; P[a].lover = true; P[b].lover = true;
            bool meKnows = ME.role == "melek" || P[a].me || P[b].me;
            if (!meKnows) return;
            Sfx.Play("heart");
            foreach (var i in lovers) W.Burst(P[i].seat + Vector3.up * 1.6f, new Color(1, 0.44f, 0.68f), 12, 0.4f, 0.22f, 1.6f);
            if (P[a].me || P[b].me) { var o = P[a].me ? P[b] : P[a]; ui.Toast(o.n + " ile âşık oldun! Biri ölürse diğeri de ölür.", 4); ui.Log("", o.n + " ile âşıksınız", "sys"); }
            else ui.Log("", P[a].n + " & " + P[b].n + " artık âşık", "sys");
        }

        async Task Morning(int n)
        {
            token++;
            phase = "morning";
            var deaths = new List<(int i, string cause)>();
            int? saved = null;
            if (wolfTarget != null)
            {
                if (guardT == wolfTarget || healTonight) saved = wolfTarget;
                else deaths.Add((wolfTarget.Value, "night"));
            }
            if (poisonT != null && !deaths.Any(d => d.i == poisonT) && P[poisonT.Value].alive) deaths.Add((poisonT.Value, "poison"));
            ui.Phase("SABAH", "Güneş doğuyor...");
            foreach (var p in P) p.sleepTo = 0;
            W.SetTOD(0, 3.5f / SPEED);
            while (W.TodBusy) await Awaitable.NextFrameAsync();
            Sfx.Play("day");
            await ui.Banner("sun", "Gün " + n, "Köy uyanıyor...", 1.8f);
            if (deaths.Count == 0)
            {
                if (saved != null) W.Burst(P[saved.Value].seat + Vector3.up * 1.2f, new Color(0.5f, 0.84f, 1f), 30, 0.6f, 0.18f, 1.6f);
                await ui.Banner("sunrise", "Bu gece kimse ölmedi", saved != null ? "Biri ölümden kurtarıldı" : "", 2.6f);
                ui.Log("", "Bu gece kimse ölmedi", "sys");
                var bs = Alive().Where(p => !p.me).ToList(); if (bs.Count > 0) { var b = Pick(bs); Later(0.8f, () => Say(b, Line("nodead"))); }
            }
            else
            {
                await KillMany(deaths);
                var bs = Alive().Where(p => !p.me).ToList();
                if (bs.Count > 0) { var b = Pick(bs); var dn = P[deaths[0].i].n; Later(0.6f, () => { Say(b, Line("dead", dn)); Emote(b, "cry"); }); }
            }
        }

        async Task DayPhase(int n)
        {
            phase = "day";
            BotDayChatter(T_DAY);
            var r = await VotePhase("day", T_DAY, "GÜNDÜZ " + n, "Tartış ve şüphelendiğin kişiyi parmağınla göster");
            ui.HideAction();
            if (over) return;
            if (r.list.Count == 0)
            {
                foreach (var p in P) p.pointAt = null; countMode = null;
                await ui.Banner("shrug", "Kimse seçilmedi", "Bugün kimse cezalandırılmadı", 2.4f);
                return;
            }
            int t = r.list[0];
            if (r.list.Count > 1)
            {
                await ui.Banner("scale", "Oylar eşit!", mayor != null ? "Son kararı muhtar verecek" : "Kura çekiliyor...", 2);
                var m = mayor != null ? P[mayor.Value] : null;
                if (m != null && m.alive && m.me && !AUTO)
                {
                    var list = r.list;
                    var v = (await Choose("Muhtarın kararı", "crown", "Eşit oy alanlardan birini seç.", T_PROMPT, q => list.Contains(q.i), Hex("#F5C542"), "Karar ver"))?.FirstOrDefault();
                    t = v ?? Pick(r.list);
                }
                else if (m != null && m.alive) { t = r.list.OrderByDescending(a => m.sus[a]).First(); m.pointAt = t; await Wait(1.2f); }
                else t = Pick(r.list);
            }
            r.counts.TryGetValue(t, out int cnt);
            await ui.Banner("scale", "Köyün kararı: " + Who(P[t]), cnt + " oy", 2);
            foreach (var p in P) p.pointAt = null; countMode = null;
            await KillMany(new List<(int, string)> { (t, "exec") });
        }

        string Winner()
        {
            var al = Alive();
            if (lovers.Length == 2 && al.Count == 2 && al.All(p => p.lover)) return "lovers";
            int v = al.Count(IsV), o = al.Count - v;
            if (v == 0) return "koy";
            if (v >= o) return "vampir";
            return null;
        }

        async Task EndGame(string w)
        {
            over = true; phase = "end"; token++;
            ui.Eyes(false); ui.HideAction(); selectable = null; rings.Clear(); ME.pointAt = null;
            bool meWin = w == "lovers" ? ME.lover : (w == "vampir" ? IsV(ME) : !IsV(ME));
            string title = w == "koy" ? "Köylüler kazandı!" : w == "vampir" ? "Vampirler kazandı!" : "Âşıklar kazandı!";
            W.SetTOD(w == "vampir" ? 1 : 0, 2.5f);
            foreach (var p in P) if (IsV(p)) p.reveal = true;
            Sfx.Play(meWin ? "win" : "lose");
            await ui.Banner(w == "koy" ? "wheat" : w == "vampir" ? "vampir" : "melek", title, meWin ? "Tebrikler, kazandın!" : "Bu sefer kaybettin", 2.6f);
            var list = new List<Hud.EndEntry>();
            foreach (var p in P)
            {
                var r = ROLES[p.role];
                bool win = w == "lovers" ? p.lover : (w == "vampir" ? IsV(p) : !IsV(p));
                list.Add(new Hud.EndEntry { icon = r.ic, name = p.me ? p.n + " (sen)" : p.n, role = r.n, color = r.c, extra = (p.i == mayor ? " · muhtar" : "") + (p.lover ? " · âşık" : ""), alive = p.alive, win = win });
            }
            ui.ShowEnd(title, meWin ? "Kazandın · +120 XP · +50 Zu altın" : "Kaybettin · +30 XP", list);
            Bridge.Send("{\"type\":\"end\",\"win\":" + (meWin ? "true" : "false") + ",\"winner\":\"" + w + "\"}");
        }

        async Task<bool> Check() { var w = Winner(); if (w != null) { await EndGame(w); return true; } return false; }

        async void Run()
        {
            try
            {
                await Intro();
                await MayorElection();
                for (int n = 1; n < 30; n++)
                {
                    await Night(n); if (over) return;
                    await Morning(n); if (await Check()) return;
                    await DayPhase(n); if (await Check()) return;
                }
            }
            catch (Exception e) { Debug.LogException(e); if (ui != null) ui.Toast("Hata: " + e.Message, 8); }
        }

        // ---------------------------------------------------------------- her kare
        float nextLookAt, fpsT0, goodSecs, armSide = 1; int fpsN, lowCount;
        readonly List<Transform> ringObjs = new List<Transform>(); Material ringMat; Mesh ringMesh;

        float AngleTo(Pl p, Vector3 target) { var v = p.ch.transform.InverseTransformPoint(target); return Mathf.Atan2(v.x, v.z); }
        bool PointVisible(Pl p)
        {
            if (p.pointAt == null) return false;
            if (countMode == "mayor" || countMode == "day") return true;
            if (phase.StartsWith("day") || phase == "mayor") return true;
            return IsV(p) && IsV(ME) && phase == "night-vampir";
        }

        void Update()
        {
            if (ME == null) return;
            float t = Time.time, dt = Mathf.Min(0.1f, Time.deltaTime), k = Mathf.Min(1, dt * 7);
            // kare hızı düşükse kaliteyi otomatik düşür
            // sürekli kare hızı izleme: 2 sn ortalama < 40 ise hafiflet, 8 sn boyunca > 56 ise bir adım geri al
            if (fpsT0 == 0) fpsT0 = t; fpsN++;
            if (t - fpsT0 > 3f)
            {
                float fps = fpsN / (t - fpsT0); fpsT0 = t; fpsN = 0;
                // ilk 8 sn (yükleme takılmaları) sayılmaz; iki ölçüm üst üste düşükse hafiflet
                if (t < 8) { }
                else if (fps < 38) { lowCount++; goodSecs = 0; if (lowCount >= 2) { W.Degrade(); lowCount = 0; } }
                else if (fps > 54) { lowCount = 0; goodSecs += 3; if (goodSecs >= 8 && W.level > 0) { W.SetLevel(W.level - 1); goodSecs = 0; } }
                else { goodSecs = 0; lowCount = 0; }
            }
            HandleInput();

            if (t > nextLookAt)
            {
                nextLookAt = t + 1.2f;
                foreach (var p in Alive())
                {
                    if (p.me || Rnd() < 0.5f) continue;
                    var sp = lastSpeaker != null && Now - lastSpeakAt < 4 ? P[lastSpeaker.Value] : null;
                    var q = sp != null && sp != p && Rnd() < 0.6f ? sp : Pick(Alive());
                    p.lookYaw = q == p ? 0 : Mathf.Clamp(AngleTo(p, q.seat), -1.3f, 1.3f);
                }
            }
            var counts = countMode != null ? Tally(countMode == "wolf" ? (Func<Pl, bool>)IsV : null, countMode == "day") : new Dictionary<int, int>();
            foreach (var p in P)
            {
                var ch = p.ch;
                p.sleep += (p.sleepTo - p.sleep) * Mathf.Min(1, dt * 2);
                p.slump += (p.slumpTo - p.slump) * Mathf.Min(1, dt * 3);
                bool show = p.alive && PointVisible(p);
                p.raise += ((show ? 1 : 0) - p.raise) * k;
                if (p.me) continue;
                var tgt = show ? P[p.pointAt.Value] : null;
                float want = tgt != null ? Mathf.Clamp(AngleTo(p, tgt.seat), -1.3f, 1.3f) : p.lookYaw;
                p.headY += (want - p.headY) * Mathf.Min(1, dt * 4);
                ch.headYaw = p.headY * (1 - p.sleep);
                ch.headPitch = 0.45f * p.sleep + 0.5f * p.slump + (p.talk > 0 ? Mathf.Sin(t * 11) * 0.05f : 0);
                ch.upperPitch = 0.08f * p.sleep + 0.85f * p.slump;
                ch.upperRoll = 0.25f * p.slump * (p.i % 2 == 1 ? 1 : -1);
                if (p.talk > 0) p.talk -= dt;
                bool red = p.reveal || (IsV(p) && IsV(ME) && W.tod > 0.5f);
                // mimik hedefleri
                float smile = 0.12f, open = 0, up = 0, angry = 0, sad = 0;
                string ex = p.expr != null && Now < p.expr.until ? p.expr.type : (red ? "evil" : null);
                switch (ex)
                {
                    case "happy": smile = 1; open = 0.25f; up = 0.3f; break;
                    case "laugh": smile = 1; open = 0.45f + Mathf.Abs(Mathf.Sin(t * 12)) * 0.4f; up = 0.5f; break;
                    case "angry": smile = 0; angry = 1; break;
                    case "sad": smile = 0; sad = 1; break;
                    case "surprise": smile = 0; up = 1; open = 0.75f; break;
                    case "think": smile = 0; up = 0.5f; angry = 0.2f; break;
                    case "evil": smile = 0.8f; angry = 0.7f; break;
                }
                if (p.talk > 0) open = Mathf.Max(open, 0.15f + Mathf.Abs(Mathf.Sin(t * 13 + p.i)) * 0.55f);
                if (p.sleep > 0.5f || p.slump > 0.5f) { smile = 0; open = 0; }
                float fk = Mathf.Min(1, dt * 10); var F = ch.face;
                F.smile += (smile - F.smile) * fk; F.open += (open - F.open) * fk; F.up += (up - F.up) * fk; F.angry += (angry - F.angry) * fk; F.sad += (sad - F.sad) * fk;
                ch.redEyes = red && p.alive;
                if (tgt != null) ch.pointYaw = Mathf.Clamp(AngleTo(p, tgt.seat) * 0.9f, -1.1f, 1.1f);
                ch.pointRaise = p.raise;
                if (p.wave > 0) { p.wave -= dt; ch.wave = Mathf.Min(1, p.wave * 2); ch.waveWiggle = Mathf.Sin(t * 14) * 0.35f * ch.wave; } else { ch.wave *= 0.8f; ch.waveWiggle *= 0.8f; }
                if (p.crownDrop > 0) { p.crownDrop = Mathf.Max(0, p.crownDrop - dt * 1.4f); float u = p.crownDrop; ch.crown.localPosition = new Vector3(0, u * u * 1.2f, 0); ch.crown.localEulerAngles = new Vector3(0, u * 6 * Mathf.Rad2Deg, 0); }
                if (p.ghost != null)
                {
                    var gh = p.ghost; float age = Now - gh.born, rise = Mathf.Min(1, age / 2);
                    gh.a += gh.sp * dt * rise;
                    float r0 = Mathf.Lerp(R, gh.r, rise), x = Mathf.Cos(gh.a) * r0, z = Mathf.Sin(gh.a) * r0;
                    var pos = new Vector3(x, 0.1f + rise * 0.7f + Mathf.Sin(t * 1.4f + gh.ph) * 0.12f, z);
                    gh.ch.transform.position = pos;
                    var dir = new Vector3(-Mathf.Sin(gh.a) * gh.sp, 0, Mathf.Cos(gh.a) * gh.sp); if (dir.sqrMagnitude > 1e-6f) gh.ch.transform.rotation = Quaternion.LookRotation(dir);
                    if (gh.ch.ghostMat != null) { var c = gh.ch.ghostMat.GetColor("_BaseColor"); c.a = 0.3f + Mathf.Sin(t * 3 + gh.ph) * 0.06f; gh.ch.ghostMat.SetColor("_BaseColor", c); }
                }
            }
            // kamera ve kendi kolum
            bool myShow = ME.alive && ME.pointAt != null && !ghostCam;
            ME.raise += ((myShow ? 1 : 0) - ME.raise) * k;
            if (ghostCam) cam.transform.position = Vector3.Lerp(cam.transform.position, new Vector3(ME.seat.x * 1.3f, 2.5f, ME.seat.z * 1.3f), Mathf.Min(1, dt * 1.5f));
            // kamera parmağı yumuşakça izler (titreşim süzülür)
            float follow = 1 - Mathf.Exp(-dt * 18f);
            yaw = Mathf.Lerp(yaw, yawT, follow); pitch = Mathf.Lerp(pitch, pitchT, follow);
            float baseYaw = Mathf.Atan2(-ME.seat.x, -ME.seat.z) * Mathf.Rad2Deg;
            cam.transform.rotation = Quaternion.Euler(pitch * Mathf.Rad2Deg, baseYaw + yaw * Mathf.Rad2Deg, 0);
            bool armOn = ME.raise > 0.03f && ME.pointAt != null;
            if (fpArm.gameObject.activeSelf != armOn) fpArm.gameObject.SetActive(armOn);
            if (armOn)
            {
                var q = P[ME.pointAt.Value].seat; var target = new Vector3(q.x, 1.3f, q.z);
                // Agrou gibi: omuz ekranın alt köşesinin dışında, kol oradan hedefe uzanır (kesik uç asla görünmez)
                float side = cam.transform.InverseTransformPoint(target).x < 0 ? -1 : 1;
                armSide = Mathf.Lerp(armSide, side, Mathf.Min(1, dt * 10));
                // önce elin ekrandaki yeri (hedefin biraz altı/yanı), sonra omuz oradan geriye: ekran köşesinin dışına
                var tv = cam.WorldToViewportPoint(target);
                // el, ekran köşesiyle hedef arasındaki çizginin üstünde: parmak ekranda tam hedefi gösterir
                float cx = armSide > 0 ? 1.12f : -0.12f, cy = -0.2f;
                float hx = Mathf.Lerp(cx, Mathf.Clamp(tv.x, 0.05f, 0.95f), 0.58f), hy = Mathf.Clamp(Mathf.Lerp(cy, Mathf.Clamp(tv.y, 0.3f, 0.9f), 0.58f), 0.22f, 0.5f);
                var hand = cam.ViewportToWorldPoint(new Vector3(hx, hy, 0.5f));
                var corner = cam.ViewportToWorldPoint(new Vector3(cx, cy, 0.28f));
                var dir = (hand - corner).normalized;
                float rch = fpArm.reach * 0.8f;
                var pos = hand - dir * rch;
                fpArm.transform.position = pos;
                var aim = Quaternion.LookRotation(dir, Vector3.up);
                fpArm.transform.rotation = aim * Quaternion.Euler((1 - ME.raise) * 1.1f * Mathf.Rad2Deg, 0, 0);
                // soldaki hedef için kolu aynala (sağ kol yerine sol kol)
                fpArm.transform.localScale = new Vector3(0.8f * (armSide < 0 ? -1 : 1), 0.8f, 0.8f);
            }
            DrawRings(t);
            // etiketler
            foreach (var p in P)
            {
                if (p.me) continue;
                var pos = p.alive || p.ghost == null ? p.seat + Vector3.up * (p.alive ? 1.95f : 1.1f) : p.ghost.ch.transform.position + Vector3.up * 1.5f;
                bool selected = rings.Any(r => r.Key == p.i) || ME.pointAt == p.i;
                bool loverKnown = p.lover && (ME.lover || ME.role == "melek");
                var icons = new List<string>();
                if (!p.alive) icons.Add("ghost");
                else
                {
                    if (p.i == mayor) icons.Add("crown");
                    if (loverKnown) icons.Add("melek");
                    if (ME.known.TryGetValue(p.i, out var kr)) icons.Add(ROLES[kr].ic);
                    else if (IsV(ME) && IsV(p)) icons.Add("vampir");
                    if (p.talk > 0) icons.Add("speaker");
                }
                counts.TryGetValue(p.i, out int n);
                ui.PlaceTag(p.tag, pos, cam, !p.alive, selected, icons.ToArray(), n > 0 ? n.ToString() : null, countMode == "wolf");
            }
        }

        void DrawRings(float t)
        {
            var list = new List<KeyValuePair<int, Color>>(rings);
            if (countMode != null && ME.pointAt != null && !list.Any(r => r.Key == ME.pointAt.Value)) list.Add(new KeyValuePair<int, Color>(ME.pointAt.Value, countMode == "wolf" ? Hex("#FF4D6D") : Hex("#F5B942")));
            if (ringMesh == null)
            {
                ringMesh = new Mesh(); var v = new List<Vector3>(); var tr = new List<int>(); int seg = 48;
                for (int i = 0; i <= seg; i++) { float a = i * Mathf.PI * 2 / seg; v.Add(new Vector3(Mathf.Cos(a) * 0.55f, 0, Mathf.Sin(a) * 0.55f)); v.Add(new Vector3(Mathf.Cos(a) * 0.66f, 0, Mathf.Sin(a) * 0.66f)); }
                for (int i = 0; i < seg; i++) { int a = i * 2; tr.AddRange(new[] { a, a + 1, a + 2, a + 1, a + 3, a + 2 }); }
                ringMesh.SetVertices(v); ringMesh.SetTriangles(tr, 0); ringMesh.RecalculateNormals(); ringMesh.RecalculateBounds();
                ringMat = new Material(Resources.Load<Material>("Mats/Unlit"));
            }
            while (ringObjs.Count < list.Count)
            {
                var g = new GameObject("halka"); g.AddComponent<MeshFilter>().sharedMesh = ringMesh; var mr = g.AddComponent<MeshRenderer>(); mr.material = new Material(ringMat); mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; ringObjs.Add(g.transform);
            }
            for (int j = 0; j < ringObjs.Count; j++)
            {
                bool on = j < list.Count; ringObjs[j].gameObject.SetActive(on); if (!on) continue;
                var p = P[list[j].Key]; ringObjs[j].position = new Vector3(p.seat.x, 0.04f, p.seat.z); ringObjs[j].localScale = Vector3.one * (1 + j * 0.12f);
                var c = list[j].Value * (1.4f + Mathf.Sin(t * 4) * 0.6f); c.a = 1;
                ringObjs[j].GetComponent<MeshRenderer>().material.SetColor("_BaseColor", c);
            }
        }

        // kaydırarak bakma, dokunarak seçme
        void HandleInput()
        {
            bool down = Input.GetMouseButtonDown(0), held = Input.GetMouseButton(0), upE = Input.GetMouseButtonUp(0);
            Vector2 mp = Input.mousePosition;
            bool overUI = EventSystem.current != null && (EventSystem.current.IsPointerOverGameObject() || (Input.touchCount > 0 && EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId)));
            if (down && !overUI) { drag = mp; downAt = mp; ui.HideHint(); }
            if (held && drag != null)
            {
                var d = mp - drag.Value; float s = 844f / Screen.width;
                yawT -= d.x * s * 0.006f; pitchT = Mathf.Clamp(pitchT + d.y * s * 0.004f, -0.35f, 0.75f); drag = mp;
            }
            if (upE)
            {
                var start = downAt; drag = null; downAt = null;
                if (start == null || (mp - start.Value).magnitude > 12 * Screen.width / 844f || selectable == null) return;
                var ray = cam.ScreenPointToRay(mp);
                var hits = Physics.RaycastAll(ray, 60).OrderBy(h => h.distance);
                foreach (var h in hits)
                {
                    var rig = h.collider.GetComponentInParent<Rig>(); if (rig == null) continue;
                    var p = P.FirstOrDefault(q => q.ch == rig);
                    if (p != null && !p.me && selectable(p)) { onSelect(p.i); return; }
                }
            }
        }
    }
}
