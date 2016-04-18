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
            NameValueCollection values = new NameValueCollection() {
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

                NameValueCollection values = new NameValueCollection() {
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

        private void Send(NameValueCollection info) {
            Send(info, false);
        }

        private void Send(NameValueCollection info, bool wait) {
            NameValueCollection values = new NameValueCollection() {
                { "v", "1" },
                { "tid", TrackingId },
                { "cid", _guid.ToString() }
            };

            var items = info.AllKeys.SelectMany(info.GetValues, (k, v) => new { key = k, value = v });
            foreach (var item in items) {
                values.Add(item.key, item.value);
            }

            try {
                using (WebClient webClient = new WebClient()) {
                    NameValueCollection collection = new NameValueCollection();
                    webClient.UploadValues(GoogleAnalyticsUrl, "POST", values);
                }
            } catch (Exception) {}
        }
    }
}
