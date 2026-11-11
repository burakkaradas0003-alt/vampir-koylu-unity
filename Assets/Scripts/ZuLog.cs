// Hata kaydı: aynı hatayı her karede tekrar yazmaz; nerede olduğunu (bileşen adı) gösterir.
using System.Collections.Generic;
using UnityEngine;

namespace VK
{
    public static class ZuLog
    {
        static readonly Dictionary<string, int> seen = new Dictionary<string, int>();
        public static void Err(string where, System.Exception e)
        {
            seen.TryGetValue(where, out int n); seen[where] = n + 1;
            if (n < 3 || n % 300 == 0) Debug.LogError("[Zu] " + where + " (" + (n + 1) + "): " + e.GetType().Name + " " + e.Message + "\n" + e.StackTrace);
        }
    }
}
