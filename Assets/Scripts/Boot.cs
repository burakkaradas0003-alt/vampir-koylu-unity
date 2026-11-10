// Açılış: dünya, arayüz ve oyunu kurar. Sahnede yalnız bu bileşen vardır (BuildSetup sahneyi oluşturur).
using UnityEngine;

namespace VK
{
    public class Boot : MonoBehaviour
    {
        void Start()
        {
            // WebGL'de kare hızı tarayıcıya bırakılmalı (sabitleme düzensiz kare/titreme yapar)
#if UNITY_WEBGL && !UNITY_EDITOR
            Application.targetFrameRate = -1;
#else
            Application.targetFrameRate = 60;
#endif
            Screen.orientation = ScreenOrientation.LandscapeLeft;
            Screen.autorotateToPortrait = false; Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToLandscapeLeft = true; Screen.autorotateToLandscapeRight = true;
            Screen.orientation = ScreenOrientation.AutoRotation;
            bool low = SystemInfo.systemMemorySize < 3000 || Bridge.Param("lite") == "1";
            var w = new GameObject("Dünya").AddComponent<World>(); w.Build(low);
            var h = new GameObject("Arayüz").AddComponent<Hud>(); h.Build();
            var g = new GameObject("Oyun").AddComponent<Game>();
            g.Begin(w, h, Bridge.Param("me"), false, System.Environment.TickCount & 0x7fffffff);
        }
    }

    // Zuplay (Flutter) ile haberleşme. Ayrı uygulama olarak çalışırken "exit" uygulamayı kapatır.
    // Zuplay'e gömüldüğünde (flutter_unity_widget) mesajlar Flutter'a iletilecek.
    public static class Bridge
    {
        static System.Collections.Generic.Dictionary<string, string> prm;
        public static void SetParams(string query)
        {
            prm = new System.Collections.Generic.Dictionary<string, string>();
            foreach (var kv in (query ?? "").Split('&'))
            {
                var i = kv.IndexOf('='); if (i <= 0) continue;
                prm[System.Uri.UnescapeDataString(kv.Substring(0, i))] = System.Uri.UnescapeDataString(kv.Substring(i + 1));
            }
        }
        public static string Param(string k) { if (prm == null) SetParams(""); return prm.TryGetValue(k, out var v) ? v : null; }
        public static void Send(string json)
        {
            Debug.Log("[Zu] " + json);
            if (json.Contains("\"exit\"")) Application.Quit();
        }
    }
}
