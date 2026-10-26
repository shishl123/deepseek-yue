using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Text;

namespace DshPet
{
    public class AccountInfo
    {
        public string Uid = "";
        public string Nickname = "";
        public long Credits;
        public long CreditsTotal;
        public string EarliestExpiry = "";
        public long EarliestRemaining;
        public bool Cooling;
        public bool Disabled;
        public long Tokens;
        public long Requests;
    }

    public class Snapshot
    {
        public bool Ok;
        public string Error = "";

        public int Total;
        public int Healthy;
        public int Cooling;
        public int Disabled;

        public long CreditsSum;
        public long CreditsTotalSum;
        public long CreditFloor;
        public long ExpiringRemain;
        public int ExpiringCount;
        public string EarliestExpiry = "";
        public long EarliestRemaining;
        public long TokensSum;

        public List<AccountInfo> Accounts = new List<AccountInfo>();
        public DateTime At = DateTime.Now;
        public string Raw = "";

        public static Snapshot Fail(string err)
        {
            Snapshot s = new Snapshot();
            s.Ok = false;
            s.Error = err;
            return s;
        }

        static DateTime ParseTime(string s)
        {
            DateTime dt;
            if (string.IsNullOrEmpty(s)) return DateTime.MinValue;
            if (DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out dt)) return dt;
            return DateTime.MinValue;
        }

        public static Snapshot Parse(string json, int expiryWarnDays)
        {
            Snapshot snap = new Snapshot();
            snap.Raw = json;
            snap.At = DateTime.Now;
            Dictionary<string, object> root = Json.Obj(Json.Parse(json));
            if (root == null) throw new FormatException("root is not an object");

            snap.Total = (int)Json.Long(root, "total", 0);
            snap.Healthy = (int)Json.Long(root, "healthy", 0);
            snap.Cooling = (int)Json.Long(root, "cooling", 0);
            snap.Disabled = (int)Json.Long(root, "disabled", 0);
            snap.CreditFloor = Json.Long(root, "credit_floor", 0);

            long sum = 0, totalSum = 0, tokens = 0;
            long expiring = 0;
            int expiringCount = 0;
            string earliest = "";
            long earliestRemain = 0;
            DateTime earliestDt = DateTime.MaxValue;
            DateTime limit = DateTime.Now.AddDays(expiryWarnDays);
            DateTime now = DateTime.Now;

            List<object> arr = Json.Arr(Json.Get(root, "accounts"));
            if (arr != null)
            {
                foreach (object o in arr)
                {
                    Dictionary<string, object> d = Json.Obj(o);
                    if (d == null) continue;
                    AccountInfo a = new AccountInfo();
                    a.Uid = Json.Str(d, "uid", "");
                    a.Nickname = Json.Str(d, "nickname", "");
                    a.Credits = Json.Long(d, "credits", 0);
                    a.CreditsTotal = Json.Long(d, "credits_total", 0);
                    a.EarliestExpiry = Json.Str(d, "credits_earliest_expiry", "");
                    a.EarliestRemaining = Json.Long(d, "credits_earliest_remaining", 0);
                    a.Cooling = Json.Bool(d, "cooling", false);
                    a.Disabled = Json.Bool(d, "disabled", false);
                    Dictionary<string, object> tu = Json.Obj(Json.Get(d, "token_usage"));
                    if (tu != null)
                    {
                        a.Tokens = Json.Long(tu, "total_tokens", 0);
                        a.Requests = Json.Long(tu, "request_count", 0);
                    }
                    sum += a.Credits;
                    totalSum += a.CreditsTotal;
                    tokens += a.Tokens;

                    DateTime ap = ParseTime(a.EarliestExpiry);
                    if (ap != DateTime.MinValue && ap < earliestDt)
                    {
                        earliestDt = ap;
                        earliest = a.EarliestExpiry;
                        earliestRemain = a.EarliestRemaining;
                    }

                    List<object> buckets = Json.Arr(Json.Get(d, "credits_expiry_buckets"));
                    if (buckets != null)
                    {
                        foreach (object b in buckets)
                        {
                            Dictionary<string, object> bd = Json.Obj(b);
                            if (bd == null) continue;
                            DateTime bt = ParseTime(Json.Str(bd, "expires_at", ""));
                            long remain = Json.Long(bd, "remain", 0);
                            if (bt == DateTime.MinValue) continue;
                            if (bt <= limit && bt >= now.AddDays(-1))
                            {
                                expiring += remain;
                                if (remain > 0) expiringCount++;
                            }
                        }
                    }

                    snap.Accounts.Add(a);
                }
            }

            snap.CreditsSum = sum;
            snap.CreditsTotalSum = totalSum;
            snap.TokensSum = tokens;
            snap.ExpiringRemain = expiring;
            snap.ExpiringCount = expiringCount;
            snap.EarliestExpiry = earliest;
            snap.EarliestRemaining = earliestRemain;
            snap.Ok = true;
            snap.Accounts.Sort(delegate(AccountInfo x, AccountInfo y) { return x.Credits.CompareTo(y.Credits); });
            return snap;
        }
    }

    public class StatusClient : IDisposable
    {
        readonly string _url;
        readonly string _token;
        readonly HttpClient _http;

        public StatusClient(string url, string token, int timeoutSeconds)
        {
            _url = url;
            _token = token;
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
            HttpClientHandler handler = new HttpClientHandler();
            handler.UseProxy = false;
            handler.AllowAutoRedirect = true;
            handler.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            _http = new HttpClient(handler);
            _http.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
            _http.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", "Bearer " + token);
            _http.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "application/json");
        }

        public Snapshot Fetch(int expiryWarnDays)
        {
            try
            {
                using (HttpResponseMessage resp = _http.GetAsync(_url).GetAwaiter().GetResult())
                {
                    string body = resp.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    if (!resp.IsSuccessStatusCode)
                        return Snapshot.Fail("HTTP " + (int)resp.StatusCode + " " + Truncate(body, 120));
                    if (string.IsNullOrEmpty(body))
                        return Snapshot.Fail("空响应");
                    return Snapshot.Parse(body, expiryWarnDays);
                }
            }
            catch (Exception ex)
            {
                Exception e = ex;
                while (e.InnerException != null) e = e.InnerException;
                return Snapshot.Fail(e.GetType().Name + ": " + Truncate(e.Message, 160));
            }
        }

        static string Truncate(string s, int n)
        {
            if (string.IsNullOrEmpty(s)) return "";
            s = s.Replace("\r", " ").Replace("\n", " ");
            return s.Length <= n ? s : s.Substring(0, n) + "...";
        }

        public void Dispose()
        {
            try { _http.Dispose(); } catch { }
        }
    }
}
