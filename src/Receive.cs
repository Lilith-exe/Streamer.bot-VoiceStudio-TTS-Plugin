using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public class CPHInline
{
    private static readonly Dictionary<string, DateTime> users = new Dictionary<string, DateTime>();
    private static Dictionary<string,string> voices = new Dictionary<string,string>();
    private static DateTime refreshAt, nextGlobal;
    private static DateTime nextVoiceList;
    private const string Worker = "390710cd-85ea-4845-bc86-27d951fc327d";

    private bool Flag(string key)
    {
        bool value;
        return CPH.TryGetArg(key, out value) && value;
    }

    private int Number(string key, int min, int max)
    {
        int value;
        if (!CPH.TryGetArg(key, out value) || value < min || value > max)
            throw new ArgumentException("Check " + key + " (expected " + min + " to " + max + ").");
        return value;
    }

    public bool Execute()
    {
        string message, user;
        bool test = Flag("isTest");
        CPH.TryGetArg("message", out message);
        CPH.TryGetArg("userId", out user);
        if (test) {
            try {
                voices = VoiceApi.Profiles();
                refreshAt = DateTime.UtcNow.AddSeconds(5);
                string requested;
                CPH.TryGetArg("testVoice", out requested);
                string selected = (requested ?? "").Trim().TrimStart('!').ToLowerInvariant();
                var names = new List<string>(voices.Keys);
                names.Sort(StringComparer.Ordinal);
                if (selected == "" || selected == "auto")
                    selected = names.Find(n => voices[n] != null);
                if (String.IsNullOrEmpty(selected) || !voices.ContainsKey(selected) || voices[selected] == null)
                    throw new Exception("Save a uniquely named profile in VoiceStudio, or check testVoice.");
                CPH.LogInfo("[VoiceStudio] Available commands: !" + String.Join(", !", names.ToArray()));
                message = "!" + selected + " This is the voice check. The last word is butterfly.";
                user = "local-test";
            } catch (Exception e) { CPH.LogError("[VoiceStudio] Local test: " + e.Message); return false; }
        }
        if (String.IsNullOrEmpty(message) || !message.StartsWith("!") || String.IsNullOrEmpty(user))
            return true;
        var match = Regex.Match(message, @"^!(?<voice>[\p{L}\p{M}\p{Nd}_-]+)\s+(?<speech>\S[\s\S]*)$");
        var listing = Regex.Match(message.TrimEnd(), @"^!voices(?:\s+([0-9]+))?$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        if ((!match.Success && !listing.Success) || Flag("isFromSharedChatGuest")) return true;
        try
        {
            string access;
            CPH.TryGetArg("audience", out access);
            bool owner = test || Flag("isBroadcaster");
            string broadcaster;
            if (CPH.TryGetArg("broadcastUserId", out broadcaster)) owner |= user == broadcaster;
            switch ((access ?? "").ToLowerInvariant()) {
                case "everyone": break;
                case "moderators": if (!owner && !Flag("isModerator")) return true; break;
                case "broadcaster": if (!owner) return true; break;
                default: throw new ArgumentException("audience must be everyone, moderators or broadcaster.");
            }
            if (listing.Success) {
                if (DateTime.UtcNow < nextVoiceList) return true;
                nextVoiceList = DateTime.UtcNow.AddSeconds(3);
                var available = VoiceApi.Profiles();
                voices = available;
                refreshAt = DateTime.UtcNow.AddSeconds(5);
                var names = new List<string>();
                foreach (var item in available)
                    if (item.Value != null) names.Add("!" + item.Key);
                names.Sort(StringComparer.Ordinal);
                var pages = new List<string>();
                string line = "";
                foreach (string name in names) {
                    if (line.Length > 0 && line.Length + name.Length + 2 > 360) {
                        pages.Add(line);
                        line = "";
                    }
                    line += (line.Length == 0 ? "" : ", ") + name;
                }
                if (line.Length > 0) pages.Add(line);
                int page = 1;
                string reply;
                if (pages.Count == 0) reply = "No voices available yet.";
                else if (listing.Groups[1].Success && (!int.TryParse(listing.Groups[1].Value, out page) || page < 1 || page > pages.Count))
                    reply = "Choose a voice list page from 1 to " + pages.Count + ".";
                else reply = "Available voices: " + pages[page-1] +
                    (page < pages.Count ? " | More: !voices " + (page+1) : "");
                CPH.SendMessage(reply, true, true);
                return true;
            }
            string speech = match.Groups["speech"].Value.Trim();
            if (speech.Length > Number("characterLimit", 1, 500)) return true;
            var now = DateTime.UtcNow;
            if (now < nextGlobal) return true;
            DateTime nextUser;
            if (users.TryGetValue(user, out nextUser) && now < nextUser) return true;
            if (now >= refreshAt) {
                refreshAt = now.AddSeconds(5);
                voices.Clear();
                voices = VoiceApi.Profiles();
            }
            var slug = match.Groups["voice"].Value.Normalize().ToLowerInvariant();
            string id;
            if (!voices.TryGetValue(slug, out id)) return true;
            if (id == null) throw new ArgumentException("Two profiles map to !" + slug + ". Rename one in VoiceStudio.");
            int global = Number("globalCooldownSeconds", 0, 600);
            int personal = Number("userCooldownSeconds", 0, 3600);
            CPH.SetArgument("vsProfile", id);
            CPH.SetArgument("vsSlug", slug);
            CPH.SetArgument("vsSpeech", speech);
            CPH.SetArgument("vsQueued", now.Ticks.ToString());
            if (!CPH.RunActionById(Worker, false)) throw new Exception("Playback action could not be queued. Reimport both actions.");
            nextGlobal = now.AddSeconds(global);
            foreach (var key in new List<string>(users.Keys))
                if (users[key] <= now) users.Remove(key);
            users[user] = now.AddSeconds(personal);
            CPH.LogInfo("[VoiceStudio] Queued !" + slug + " (" + id + ", " + speech.Length + " characters).");
            return true;
        }
        catch (Exception e) { CPH.LogError("[VoiceStudio] " + e.Message); return false; }
    }
}
