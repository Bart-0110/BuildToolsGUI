using System.Net.Http;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;

namespace BuildTools {
   
    public class GoogleAnalytics {

        private const string TrackingId = "UA-43509218-6";
        private const string GoogleAnalyticsUrl = "http://www.google-analytics.com/collect";
        private readonly Guid _guid;
        private Dictionary<string, DateTime> timer = new Dictionary<string, DateTime>(); 

        public GoogleAnalytics(Guid guid) {
            _guid = guid;
        }

        public void SendEvent(string category, string action) {
            Dictionary<string, string> values = new Dictionary<string, string>() {
                    { "t", "event" },
                    { "ec", category },
                    { "ea", action },
                    { "el", action },
                    { "ua", "Mozilla/5.0 (compatible; MSIE 10.0; Windows NT " + Environment.OSVersion.Version.Major + "." + Environment.OSVersion.Version.Minor +  "; Trident/6.0; Touch)" }
            };

            Send(values);
        }

        public void StartTimer(string key) {
            timer.Add(key, DateTime.Now);
        }

        public void EndTimer(string key) {
            if (timer.ContainsKey(key)) {
                DateTime start = timer[key];

                long time = (long) (DateTime.Now - start).TotalMilliseconds;

                timer.Clear();

                Dictionary<string, string> values = new Dictionary<string, string> {
                    { "t", "timing" },
                    { "utc", key },
                    { "utv", key },
                    { "utl", key },
                    { "utt", time.ToString() },
                     { "ua", "Mozilla/5.0 (compatible; MSIE 10.0; Windows NT " + Environment.OSVersion.Version.Major + "." + Environment.OSVersion.Version.Minor +  "; Trident/6.0; Touch)" }
                };

                Send(values);
            }
        }

        private void Send(Dictionary<string, string> info) {
            Send(info, false);
        }

        private void Send(Dictionary<string, string> info, bool wait) {
            using (HttpClient client = new HttpClient()) {
                Dictionary<string, string> values = new Dictionary<string, string> {
                    { "v", "1" },
                    { "tid", TrackingId },
                    { "cid", _guid.ToString() }
                };

                foreach (KeyValuePair<string, string> keyValuePair in info) {
                    values.Add(keyValuePair.Key, keyValuePair.Value);
                }

                using (WebClient webClient = new WebClient()) {
                    Console.WriteLine("Sending Post to: " + GoogleAnalyticsUrl);
                    Console.WriteLine("With: " + ToDebugString(values));
                    NameValueCollection collection = new NameValueCollection();
                    byte[] response = webClient.UploadValues(GoogleAnalyticsUrl, "POST", ToNameValueCollection(values));
                    Console.WriteLine(System.Text.Encoding.UTF8.GetString(response));
                }
            }
        }

        public static string ToDebugString<TKey, TValue>(IDictionary<TKey, TValue> dictionary) {
            return "{" + string.Join(",", dictionary.Select(kv => kv.Key.ToString() + "=" + kv.Value.ToString()).ToArray()) + "}";
        }

        public static NameValueCollection ToNameValueCollection<TKey, TValue>(IDictionary<TKey, TValue> dict) {
            NameValueCollection nameValueCollection = new NameValueCollection();

            foreach (KeyValuePair<TKey, TValue> kvp in dict) {
                string value = null;
                if (kvp.Value != null)
                    value = kvp.Value.ToString();

                nameValueCollection.Add(kvp.Key.ToString(), value);
            }

            return nameValueCollection;
        }
    }
}
