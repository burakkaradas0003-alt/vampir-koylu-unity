// Oyuncu verisi: görünüm (Look), kozmetik kataloğu, profil, envanter, cüzdan, görevler ve başarımlar.
// Şimdilik cihazda saklanır (PlayerPrefs). Zuplay'e bağlanınca aynı yapılar Bridge üzerinden
// sunucudan gelir / sunucuya gider: Store.Load / Store.Save tek giriş noktasıdır.
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace VK
{
    // ---------------------------------------------------------------- görünüm
    [Serializable]
    public class Look
    {
        public string hair = "Hairstyle_male_012", hat = "", top = "T_Shirt_009", bottom = "Pants_010", shoes = "Shoe_Sneakers_009";
        public string glasses = "", face = "", hands = "", ears = "", costume = "", mask = "", head = "";
        public string effect = "", victory = "", frame = "", title = "";
        public string synty = ""; // deneme: Synty Fantasy Hero kombini (boşsa klasik karakter)
        public int skin = 1, topColor, bottomColor, hatColor;
        public Look Clone() => (Look)MemberwiseClone();
        public string Json() => JsonUtility.ToJson(this);
        public static Look FromJson(string j) { try { return string.IsNullOrEmpty(j) ? new Look() : JsonUtility.FromJson<Look>(j); } catch { return new Look(); } }

        // FBX'teki gösterilecek parçalar
        public IEnumerable<string> Parts()
        {
            if (!string.IsNullOrEmpty(costume))
            {
                yield return costume;
                if (!string.IsNullOrEmpty(glasses)) yield return glasses;
                if (!string.IsNullOrEmpty(face)) yield return face;
                yield break;
            }
            foreach (var p in new[] { hat != "" ? hat : hair, top, bottom, shoes, glasses, face, hands, ears })
                if (!string.IsNullOrEmpty(p)) yield return p;
            if (bottom == "Shorts_003" && shoes != "") yield return "Socks_008";
        }

        public static readonly Color[] SKINS = { new Color(1f, 0.93f, 0.86f), Color.white, new Color(0.94f, 0.85f, 0.77f), new Color(0.9f, 0.76f, 0.66f), new Color(0.78f, 0.6f, 0.46f), new Color(0.63f, 0.46f, 0.34f) };
        public static readonly Color[] TINTS = { Color.white, new Color(1f, 0.72f, 0.72f), new Color(1f, 0.86f, 0.62f), new Color(0.74f, 0.92f, 0.7f), new Color(0.68f, 0.84f, 1f), new Color(0.82f, 0.74f, 1f), new Color(1f, 0.74f, 0.92f), new Color(0.55f, 0.55f, 0.6f) };
        public static readonly string[] TINT_NAMES = { "Orijinal", "Gül", "Kum", "Nane", "Gök", "Lavanta", "Şeker", "Füme" };
    }

    // ---------------------------------------------------------------- katalog
    public enum Cat { Hair, Hat, Top, Bottom, Shoes, Glasses, Face, Hands, Ears, Costume, Mask, Head, Effect, Victory, Frame, Title }

    public class Item
    {
        public string id, name, icon; public Cat cat; public int price; public string rarity = "Sıradan"; public Color tint = Color.white; public string desc = "";
        public bool Free => price == 0;
        public Sprite Icon() => Resources.Load<Sprite>("Items/" + icon) ?? Hud.Emoji(icon);
    }

    public static class Catalog
    {
        public static readonly Dictionary<Cat, string> CAT_NAME = new Dictionary<Cat, string>
        {
            { Cat.Hair, "Saç" }, { Cat.Hat, "Şapka" }, { Cat.Top, "Üst" }, { Cat.Bottom, "Alt" }, { Cat.Shoes, "Ayakkabı" }, { Cat.Glasses, "Gözlük" },
            { Cat.Face, "Yüz" }, { Cat.Hands, "Eldiven" }, { Cat.Ears, "Kulaklık" }, { Cat.Costume, "Kostüm" }, { Cat.Mask, "Maske" }, { Cat.Head, "Aksesuar" },
            { Cat.Effect, "Efekt" }, { Cat.Victory, "Zafer pozu" }, { Cat.Frame, "Çerçeve" }, { Cat.Title, "Unvan" },
        };
        public static readonly Dictionary<Cat, string> CAT_ICON = new Dictionary<Cat, string>
        {
            { Cat.Hair, "Hairstyle_male_010" }, { Cat.Hat, "tophat" }, { Cat.Top, "shirt" }, { Cat.Bottom, "jeans" }, { Cat.Shoes, "shoe" }, { Cat.Glasses, "glasses" },
            { Cat.Face, "Moustache_002" }, { Cat.Hands, "gloves" }, { Cat.Ears, "headphone" }, { Cat.Costume, "clown" }, { Cat.Mask, "mask" }, { Cat.Head, "sparkles" },
            { Cat.Effect, "dizzy" }, { Cat.Victory, "hands" }, { Cat.Frame, "gem" }, { Cat.Title, "scroll" },
        };
        // boş bırakılabilen kategoriler (çıkar seçeneği)
        public static bool Optional(Cat c) => c != Cat.Top && c != Cat.Bottom && c != Cat.Shoes && c != Cat.Hair;

        public static readonly List<Item> ALL = new List<Item>
        {
            I("Hairstyle_male_012", "Kısa Saç", Cat.Hair, 0), I("Hairstyle_male_010", "Kıvırcık Saç", Cat.Hair, 0),
            I("Hat_010", "Balıkçı Şapkası", Cat.Hat, 150, "Nadir"), I("Hat_057", "Pilot Kaskı", Cat.Hat, 320, "Destansı"), I("Hat_049", "Tavşan Başlığı", Cat.Hat, 450, "Destansı"),
            I("T_Shirt_009", "Tişört", Cat.Top, 0), I("Outerwear_036", "Kapüşonlu", Cat.Top, 120, "Nadir"), I("Outerwear_029", "Çizgili Mont", Cat.Top, 220, "Nadir"),
            I("Pants_010", "Kargo Pantolon", Cat.Bottom, 0), I("Pants_014", "Siyah Pantolon", Cat.Bottom, 90), I("Shorts_003", "Şort", Cat.Bottom, 60),
            I("Shoe_Sneakers_009", "Spor Ayakkabı", Cat.Shoes, 0), I("Shoe_Slippers_005", "Turuncu Terlik", Cat.Shoes, 70), I("Shoe_Slippers_002", "Ponponlu Terlik", Cat.Shoes, 140, "Nadir"),
            I("Glasses_004", "Kırmızı Gözlük", Cat.Glasses, 80), I("Glasses_006", "Pilot Gözlüğü", Cat.Glasses, 160, "Nadir"),
            I("Moustache_001", "İnce Bıyık", Cat.Face, 60), I("Moustache_002", "Gür Bıyık", Cat.Face, 90), I("Clown_nose_001", "Palyaço Burnu", Cat.Face, 120, "Nadir"), I("Pacifier_001", "Emzik", Cat.Face, 200, "Nadir"),
            I("Gloves_006", "Yeşil Eldiven", Cat.Hands, 70), I("Gloves_014", "Kırmızı Eldiven", Cat.Hands, 70),
            I("Headphones_002", "Kulaklık", Cat.Ears, 110),
            I("Costume_10_001", "Palyaço Kostümü", Cat.Costume, 600, "Efsanevi"), I("Costume_6_001", "Fare Kostümü", Cat.Costume, 600, "Efsanevi"),
            I("mask_night", "Gece Maskesi", Cat.Mask, 180, "Nadir", "mask", new Color(0.12f, 0.12f, 0.16f)), I("mask_gold", "Balo Maskesi", Cat.Mask, 380, "Destansı", "mask", new Color(0.96f, 0.77f, 0.26f)), I("mask_red", "Kızıl Maske", Cat.Mask, 260, "Nadir", "mask", new Color(0.85f, 0.18f, 0.24f)),
            I("head_halo", "Melek Halesi", Cat.Head, 500, "Efsanevi", "angel", new Color(1f, 0.86f, 0.4f)), I("head_horns", "Şeytan Boynuzu", Cat.Head, 420, "Destansı", "devilface", new Color(0.8f, 0.12f, 0.16f)),
            I("head_cat", "Kedi Kulakları", Cat.Head, 240, "Nadir", "cat", new Color(0.25f, 0.22f, 0.24f)), I("head_bunny", "Pembe Kulaklar", Cat.Head, 240, "Nadir", "bunny", new Color(1f, 0.72f, 0.85f)),
            I("fx_fireflies", "Ateş Böcekleri", Cat.Effect, 350, "Destansı", "sparkles", new Color(0.85f, 1f, 0.45f)), I("fx_hearts", "Kalpler", Cat.Effect, 300, "Nadir", "heart", new Color(1f, 0.35f, 0.5f)),
            I("fx_stars", "Yıldız Tozu", Cat.Effect, 420, "Destansı", "star", new Color(1f, 0.85f, 0.35f)), I("fx_bats", "Gece Gölgesi", Cat.Effect, 550, "Efsanevi", "bat", new Color(0.6f, 0.3f, 1f)),
            I("vic_cheer", "Kollar Havada", Cat.Victory, 0, "Sıradan", "hands"), I("vic_clap", "Alkış", Cat.Victory, 150, "Nadir", "clap"), I("vic_flex", "Güç Gösterisi", Cat.Victory, 300, "Destansı", "muscle"), I("vic_wave", "Selam", Cat.Victory, 120, "Nadir", "wave"),
            I("fr_gold", "Altın Çerçeve", Cat.Frame, 400, "Destansı", "medal", new Color(0.96f, 0.77f, 0.26f)), I("fr_amethyst", "Ametist", Cat.Frame, 250, "Nadir", "gem", new Color(0.66f, 0.5f, 1f)),
            I("fr_ice", "Buz Mavisi", Cat.Frame, 250, "Nadir", "snow", new Color(0.5f, 0.85f, 1f)), I("fr_rose", "Gül", Cat.Frame, 250, "Nadir", "flower", new Color(1f, 0.5f, 0.7f)),
            I("t_hope", "Köyün Umudu", Cat.Title, 0, "Sıradan", "wheat"), I("t_hunter", "Gece Avcısı", Cat.Title, 200, "Nadir", "avci"), I("t_sly", "Kurnaz Vampir", Cat.Title, 200, "Nadir", "vampir"),
            I("t_oracle", "Kâhinlerin Kâhini", Cat.Title, 350, "Destansı", "crystal"), I("t_legend", "Efsane", Cat.Title, 900, "Efsanevi", "trophy"),
        };
        static Item I(string id, string name, Cat c, int price, string rarity = "Sıradan", string icon = null, Color? tint = null)
            => new Item { id = id, name = name, cat = c, price = price, rarity = rarity, icon = icon ?? id, tint = tint ?? Color.white };

        public static Item Get(string id) => ALL.FirstOrDefault(i => i.id == id);
        public static IEnumerable<Item> Of(Cat c) => ALL.Where(i => i.cat == c);
        public static Color RarityColor(string r) => r == "Efsanevi" ? new Color(1f, 0.66f, 0.2f) : r == "Destansı" ? new Color(0.72f, 0.48f, 1f) : r == "Nadir" ? new Color(0.36f, 0.7f, 1f) : new Color(0.7f, 0.73f, 0.8f);

        public static string Get(Look l, Cat c)
        {
            switch (c)
            {
                case Cat.Hair: return l.hair; case Cat.Hat: return l.hat; case Cat.Top: return l.top; case Cat.Bottom: return l.bottom; case Cat.Shoes: return l.shoes;
                case Cat.Glasses: return l.glasses; case Cat.Face: return l.face; case Cat.Hands: return l.hands; case Cat.Ears: return l.ears; case Cat.Costume: return l.costume;
                case Cat.Mask: return l.mask; case Cat.Head: return l.head; case Cat.Effect: return l.effect; case Cat.Victory: return l.victory; case Cat.Frame: return l.frame; default: return l.title;
            }
        }
        public static void Set(Look l, Cat c, string v)
        {
            switch (c)
            {
                case Cat.Hair: l.hair = v; break; case Cat.Hat: l.hat = v; break; case Cat.Top: l.top = v; break; case Cat.Bottom: l.bottom = v; break; case Cat.Shoes: l.shoes = v; break;
                case Cat.Glasses: l.glasses = v; break; case Cat.Face: l.face = v; break; case Cat.Hands: l.hands = v; break; case Cat.Ears: l.ears = v; break; case Cat.Costume: l.costume = v; break;
                case Cat.Mask: l.mask = v; break; case Cat.Head: l.head = v; break; case Cat.Effect: l.effect = v; break; case Cat.Victory: l.victory = v; break; case Cat.Frame: l.frame = v; break; default: l.title = v; break;
            }
        }

        // botlar için rastgele ama uyumlu görünüm (sahip olma şartı yok)
        public static Look Random(System.Random r)
        {
            T P<T>(IList<T> a) => a[r.Next(a.Count)];
            var l = new Look();
            l.hair = P(new[] { "Hairstyle_male_010", "Hairstyle_male_012" });
            l.top = P(new[] { "T_Shirt_009", "Outerwear_029", "Outerwear_036" }); l.bottom = P(new[] { "Pants_010", "Pants_014", "Shorts_003" });
            l.shoes = P(new[] { "Shoe_Sneakers_009", "Shoe_Slippers_002", "Shoe_Slippers_005" });
            if (r.NextDouble() < 0.35) l.hat = P(new[] { "Hat_010", "Hat_057", "Hat_049" });
            if (r.NextDouble() < 0.35) l.glasses = P(new[] { "Glasses_004", "Glasses_006" });
            if (r.NextDouble() < 0.3) l.face = P(new[] { "Moustache_001", "Moustache_002", "Clown_nose_001" });
            if (r.NextDouble() < 0.12) l.costume = P(new[] { "Costume_10_001", "Costume_6_001" });
            if (r.NextDouble() < 0.12) l.ears = "Headphones_002";
            if (r.NextDouble() < 0.12) l.head = P(new[] { "head_cat", "head_bunny", "head_horns" });
            l.skin = r.Next(Look.SKINS.Length); l.topColor = r.NextDouble() < 0.5 ? 0 : r.Next(Look.TINTS.Length); l.bottomColor = r.NextDouble() < 0.6 ? 0 : r.Next(Look.TINTS.Length);
            l.frame = P(new[] { "", "", "fr_amethyst", "fr_ice", "fr_rose", "fr_gold" });
            return l;
        }
    }

    // ---------------------------------------------------------------- görev ve başarımlar
    public class QuestDef { public string id, name, icon, stat; public int goal, xp, coins; public bool weekly; }
    public class AchDef { public string id, name, desc, icon, stat, reward; public int goal; }

    [Serializable] public class StatKV { public string k; public int v; }
    [Serializable] public class QuestState { public string id; public int start; public bool claimed; }

    [Serializable]
    public class Profile
    {
        public string name = "Oyuncu";
        public int level = 1, xp, coins = 600, gems;
        public Look look = new Look();
        public List<string> owned = new List<string>();
        public List<StatKV> stats = new List<StatKV>();
        public List<string> achieved = new List<string>();
        public List<QuestState> quests = new List<QuestState>();
        public string questDay = "", questWeek = "";
        public List<string> history = new List<string>(); // son maçlar: "kazandı|Kâhin|9|3"

        public int Stat(string k) { var s = stats.FirstOrDefault(x => x.k == k); return s == null ? 0 : s.v; }
        public void Add(string k, int v = 1) { var s = stats.FirstOrDefault(x => x.k == k); if (s == null) stats.Add(s = new StatKV { k = k }); s.v += v; }
        public bool Owns(string id) { var it = Catalog.Get(id); return string.IsNullOrEmpty(id) || (it != null && it.Free) || owned.Contains(id); }
        public static int XpFor(int level) => 200 + (level - 1) * 120;
    }

    public static class Store
    {
        public static Profile P;
        public static event Action Changed;
        const string KEY = "zu.vk.profile.v1";

        public static readonly List<QuestDef> DAILY = new List<QuestDef>
        {
            new QuestDef { id = "d_play", name = "3 maç oyna", icon = "gamepad", stat = "games", goal = 3, xp = 80, coins = 60 },
            new QuestDef { id = "d_win", name = "1 maç kazan", icon = "trophy", stat = "wins", goal = 1, xp = 100, coins = 80 },
            new QuestDef { id = "d_vote", name = "Gündüz 5 oy ver", icon = "vote", stat = "votes", goal = 5, xp = 60, coins = 40 },
            new QuestDef { id = "d_chat", name = "Sohbete 5 mesaj yaz", icon = "speaker", stat = "msgs", goal = 5, xp = 40, coins = 30 },
        };
        public static readonly List<QuestDef> WEEKLY = new List<QuestDef>
        {
            new QuestDef { id = "w_play", name = "15 maç oyna", icon = "gamepad", stat = "games", goal = 15, xp = 400, coins = 300, weekly = true },
            new QuestDef { id = "w_win", name = "6 maç kazan", icon = "trophy", stat = "wins", goal = 6, xp = 500, coins = 400, weekly = true },
            new QuestDef { id = "w_vamp", name = "Vampir olarak 2 kez kazan", icon = "vampir", stat = "wins_vampir", goal = 2, xp = 450, coins = 350, weekly = true },
            new QuestDef { id = "w_survive", name = "5 maçı hayatta bitir", icon = "shield", stat = "survived", goal = 5, xp = 350, coins = 250, weekly = true },
        };
        public static readonly List<AchDef> ACH = new List<AchDef>
        {
            new AchDef { id = "a_first", name = "İlk Adım", desc = "İlk maçını oyna", icon = "rocket", stat = "games", goal = 1 },
            new AchDef { id = "a_win1", name = "İlk Zafer", desc = "Bir maç kazan", icon = "trophy", stat = "wins", goal = 1 },
            new AchDef { id = "a_win10", name = "Usta", desc = "10 maç kazan", icon = "medal", stat = "wins", goal = 10, reward = "t_legend" },
            new AchDef { id = "a_vamp", name = "Gecenin Efendisi", desc = "Vampir olarak 3 kez kazan", icon = "vampir", stat = "wins_vampir", goal = 3, reward = "t_sly" },
            new AchDef { id = "a_seer", name = "Üçüncü Göz", desc = "Kâhin olarak 3 vampir bul", icon = "crystal", stat = "seer_found", goal = 3, reward = "t_oracle" },
            new AchDef { id = "a_hunter", name = "Son Ok", desc = "Avcı olarak bir vampiri vur", icon = "avci", stat = "hunter_hit", goal = 1, reward = "t_hunter" },
            new AchDef { id = "a_mayor", name = "Halkın Sesi", desc = "3 kez muhtar seçil", icon = "crown", stat = "mayor", goal = 3 },
            new AchDef { id = "a_survive", name = "Hayatta Kalan", desc = "10 maçı hayatta bitir", icon = "shield", stat = "survived", goal = 10 },
            new AchDef { id = "a_talk", name = "Köyün Sesi", desc = "100 mesaj yaz", icon = "speaker", stat = "msgs", goal = 100 },
            new AchDef { id = "a_style", name = "Moda İkonu", desc = "10 kozmetik ürüne sahip ol", icon = "palette", stat = "owned", goal = 10, reward = "fr_gold" },
        };

        public static void Load()
        {
            string j = null; try { j = PlayerPrefs.GetString(KEY, ""); } catch { }
            try { P = string.IsNullOrEmpty(j) ? new Profile() : JsonUtility.FromJson<Profile>(j); } catch { P = new Profile(); }
            if (P == null) P = new Profile();
            if (P.look == null) P.look = new Look();
            var nm = Bridge.Param("me"); if (!string.IsNullOrEmpty(nm)) P.name = nm.Length > 14 ? nm.Substring(0, 14) : nm;
            if (string.IsNullOrEmpty(P.look.victory)) P.look.victory = "vic_cheer";
            if (string.IsNullOrEmpty(P.look.title)) P.look.title = "t_hope";
            RollQuests();
        }
        public static void Save()
        {
            try { PlayerPrefs.SetString(KEY, JsonUtility.ToJson(P)); PlayerPrefs.Save(); } catch { }
            Changed?.Invoke();
        }

        // ---- cüzdan ve envanter
        public static bool Buy(Item it)
        {
            if (it == null || P.Owns(it.id) || P.coins < it.price) return false;
            P.coins -= it.price; P.owned.Add(it.id); P.stats.RemoveAll(s => s.k == "owned"); P.Add("owned", P.owned.Count);
            Bridge.Send("{\"type\":\"buy\",\"item\":\"" + it.id + "\",\"price\":" + it.price + "}");
            CheckAch(); Save(); return true;
        }
        public static void Equip(Look l) { P.look = l.Clone(); Bridge.Send("{\"type\":\"look\",\"look\":" + l.Json() + "}"); Save(); }

        // ---- deneyim
        public static int GainXp(int xp)
        {
            int ups = 0; P.xp += xp;
            while (P.xp >= Profile.XpFor(P.level)) { P.xp -= Profile.XpFor(P.level); P.level++; ups++; P.coins += 100; }
            return ups;
        }

        // ---- görevler: günlük/haftalık sayaç, ilerleme = şimdiki istatistik - başlangıç
        static string Today => DateTime.Now.ToString("yyyy-MM-dd");
        static string Week { get { var d = DateTime.Now; int diff = ((int)d.DayOfWeek + 6) % 7; return d.AddDays(-diff).ToString("yyyy-MM-dd"); } }
        static void RollQuests()
        {
            if (P.questDay != Today) { P.quests.RemoveAll(q => !q.id.StartsWith("w_")); foreach (var q in DAILY) P.quests.Add(new QuestState { id = q.id, start = P.Stat(q.stat) }); P.questDay = Today; }
            if (P.questWeek != Week) { P.quests.RemoveAll(q => q.id.StartsWith("w_")); foreach (var q in WEEKLY) P.quests.Add(new QuestState { id = q.id, start = P.Stat(q.stat) }); P.questWeek = Week; }
        }
        public static QuestState QState(string id) => P.quests.FirstOrDefault(q => q.id == id);
        public static int Progress(QuestDef d) { var s = QState(d.id); return s == null ? 0 : Mathf.Clamp(P.Stat(d.stat) - s.start, 0, d.goal); }
        public static bool Claim(QuestDef d)
        {
            var s = QState(d.id); if (s == null || s.claimed || Progress(d) < d.goal) return false;
            s.claimed = true; P.coins += d.coins; GainXp(d.xp); Save(); return true;
        }
        public static int Claimable() => DAILY.Concat(WEEKLY).Count(d => { var s = QState(d.id); return s != null && !s.claimed && Progress(d) >= d.goal; });

        public static List<AchDef> CheckAch()
        {
            var got = new List<AchDef>();
            foreach (var a in ACH)
            {
                if (P.achieved.Contains(a.id) || P.Stat(a.stat) < a.goal) continue;
                P.achieved.Add(a.id); got.Add(a);
                if (!string.IsNullOrEmpty(a.reward) && !P.owned.Contains(a.reward)) P.owned.Add(a.reward);
            }
            return got;
        }

        // ---- maç sonucu
        public class MatchResult { public bool win, alive, mvp; public string role, roleName; public int xp, coins, levelUps; public List<AchDef> ach = new List<AchDef>(); }
        public static MatchResult Report(bool win, bool alive, string role, string roleName, bool mvp, int votes, int msgs, bool mayor, int seerFound, bool hunterHit)
        {
            var r = new MatchResult { win = win, alive = alive, role = role, roleName = roleName, mvp = mvp };
            P.Add("games"); if (win) { P.Add("wins"); P.Add("wins_" + role); } else P.Add("losses");
            if (alive) P.Add("survived"); P.Add("role_" + role); P.Add("votes", votes); P.Add("msgs", msgs);
            if (mayor) P.Add("mayor"); if (seerFound > 0) P.Add("seer_found", seerFound); if (hunterHit) P.Add("hunter_hit"); if (mvp) P.Add("mvp");
            r.xp = (win ? 120 : 40) + (alive ? 20 : 0) + (mvp ? 50 : 0); r.coins = (win ? 50 : 15) + (mvp ? 25 : 0);
            P.coins += r.coins; r.levelUps = GainXp(r.xp);
            P.history.Insert(0, (win ? "W" : "L") + "|" + roleName + "|" + (alive ? "1" : "0") + "|" + (mvp ? "1" : "0"));
            while (P.history.Count > 12) P.history.RemoveAt(P.history.Count - 1);
            r.ach = CheckAch(); Save();
            return r;
        }

        public static List<KeyValuePair<string, int>> TopRoles()
        {
            return P.stats.Where(s => s.k.StartsWith("role_")).OrderByDescending(s => s.v).Select(s => new KeyValuePair<string, int>(s.k.Substring(5), s.v)).ToList();
        }
    }
}
