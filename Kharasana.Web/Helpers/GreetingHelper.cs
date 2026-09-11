using System;

namespace Kharasana.Web.Helpers
{
    /// <summary>
    /// دوال مساعدة للتحيّات حسب وقت اليوم في لوحات المعلومات.
    /// </summary>
    public static class GreetingHelper
    {
        /// <summary>
        /// تحية صباحية/مسائية مناسبة حسب الساعة الحالية.
        /// </summary>
        public static string Greeting(DateTime? now = null)
        {
            var hour = (now ?? DateTime.Now).Hour;

            if (hour < 12)
                return "صباح الخير";
            if (hour < 17)
                return "مساء الخير";
            return "مساء النور";
        }
    }
}
