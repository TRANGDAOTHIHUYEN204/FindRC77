using UnityEngine;

namespace JKit.Monetize.Ads
{
    public class LoadAdError
    {
        private UnityEngine.AndroidJavaObject error;
        private string message;

        public LoadAdError(UnityEngine.AndroidJavaObject error)
        {
            this.error = error;
        }

        // ctor để hỗ trợ iOS (UnitySendMessage gửi chuỗi JSON hoặc thông báo)
        public LoadAdError(string message)
        {
            this.message = message;
        }

        public string Message
        {
            get
            {
                if (!string.IsNullOrEmpty(message)) return message;
                if (error != null)
                {
                    try
                    {
                        return error.Call<string>("toString");
                    }
                    catch
                    {
                        return "(android error)";
                    }
                }
                return null;
            }
        }

        public override string ToString()
        {
            return Message ?? base.ToString();
        }
    }
}