internal static class VoiceApi
{
    public static System.Net.HttpWebRequest Request(string path, int timeout)
    {
        var req = (System.Net.HttpWebRequest)System.Net.WebRequest.Create("http://127.0.0.1:3900" + path);
        req.Proxy = null;
        req.AllowAutoRedirect = false;
        req.Timeout = req.ReadWriteTimeout = timeout;
        return req;
    }

    public class Profile { public string id { get; set; } public string name { get; set; } }
    public static System.Collections.Generic.Dictionary<string,string> Profiles()
    {
        using (var resp = Request("/profiles", 2500).GetResponse())
        using (var reader = new System.IO.StreamReader(resp.GetResponseStream()))
            return ParseProfiles(reader.ReadToEnd());
    }
    public static System.Collections.Generic.Dictionary<string,string> ParseProfiles(string data)
    {
        var map = new System.Collections.Generic.Dictionary<string,string>();
        var json = new System.Web.Script.Serialization.JavaScriptSerializer();
        foreach (var p in json.Deserialize<Profile[]>(data)) {
            if (String.IsNullOrWhiteSpace(p.name) || String.IsNullOrWhiteSpace(p.id)) continue;
            string slug = System.Text.RegularExpressions.Regex.Replace(p.name.Trim().Normalize().ToLowerInvariant(), @"[^\p{L}\p{M}\p{Nd}_-]+", "-").Trim('-');
            if (slug.Length > 0) map[slug] = map.ContainsKey(slug) ? null : p.id;
        }
        return map;
    }
}
