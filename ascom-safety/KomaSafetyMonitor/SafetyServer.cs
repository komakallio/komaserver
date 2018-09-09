using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;

namespace ASCOM.Komakallio
{
    internal class SafetyServer
    {
        public SafetyServer(string address)
        {
            mServerAddress = address;
        }

        private string mServerAddress;

        public SafetyStatus Status
        {
            get
            {
                JObject values;
                var request = WebRequest.Create(mServerAddress);
                request.Timeout = 1000;
                using (var response = request.GetResponse() as HttpWebResponse)
                {
                    checkStatusCode(response);
                    values = readJsonFrom(response);
                }

                var overallSafety = Boolean.Parse(values["safe"].ToString());
                var safetyDetails = parseSafetyFrom(values);


                return new SafetyStatus(overallSafety, safetyDetails);
            }
        }

        private static Dictionary<string, bool> parseSafetyFrom(JObject values)
        {
            Dictionary<string, bool> details = new Dictionary<string, bool>();
            foreach (JProperty item in values["details"])
            {
                var name = item.Name;
                var safe = Boolean.Parse(item.Value["safe"].ToString());
                details[name] = safe;
            }

            return details;
        }

        private static JObject readJsonFrom(HttpWebResponse response)
        {
            string jsonResponse;
            using (var responseStream = response.GetResponseStream())
            {
                using (var reader = new StreamReader(responseStream, System.Text.Encoding.UTF8))
                {
                    jsonResponse = reader.ReadToEnd();
                }
            }

            return JObject.Parse(jsonResponse);
        }

        private static void checkStatusCode(HttpWebResponse response)
        {
            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new Exception(String.Format("Server error (HTTP {0}: {1}).",
                    response.StatusCode,
                    response.StatusDescription));
            }
        }
    }

    internal struct SafetyStatus
    {
        public SafetyStatus(bool isSafe, Dictionary<string, bool> details)
        {
            IsSafe = isSafe;
            Details = details;
        }
        public bool IsSafe { get; private set; }
        public Dictionary<string, bool> Details { get; private set; }
    }
}
