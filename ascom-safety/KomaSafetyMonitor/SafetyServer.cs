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
                var request = WebRequest.Create(mServerAddress) as HttpWebRequest;
                request.Timeout = 1000;
                using (var response = request.GetResponse() as HttpWebResponse)
                {
                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        throw new Exception(String.Format("Server error (HTTP {0}: {1}).",
                            response.StatusCode,
                            response.StatusDescription));
                    }

                    string jsonResponse;
                    using (var responseStream = response.GetResponseStream())
                    {
                        using (var reader = new StreamReader(responseStream, System.Text.Encoding.UTF8))
                        {
                            jsonResponse = reader.ReadToEnd();
                        }
                    }

                    values = JObject.Parse(jsonResponse);
                }

                Dictionary<string, bool> details = new Dictionary<string, bool>();
                foreach (JProperty item in values["details"])
                {
                    var name = item.Name;
                    var safe = Boolean.Parse(item.Value["safe"].ToString());
                    details[name] = safe;
                }

                return new SafetyStatus(Boolean.Parse(values["safe"].ToString()), details);
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
