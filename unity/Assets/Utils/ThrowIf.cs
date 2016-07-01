using System;

namespace UnityClient.Utils
{
    public class ThrowIf
    {
        public static void Null(string value, string argName)
        {
            if (value == null || value == string.Empty || value == "")
                throw new ArgumentNullException(argName);
        }

        public static void NullOnly(object value, string argName)
        {
            if (value == null)
                throw new ArgumentNullException(argName);
        }
    }
}
