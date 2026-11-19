using System;
using System.IO;
using System.Net;
using System.Text;
using System.Globalization;

public class CPHInline
{
    public static byte[] Form(string id, string speech)
    {
        return Encoding.UTF8.GetBytes("effect_preset=raw&stream=false&wav_bits=16&profile_id=" +
            Uri.EscapeDataString(id) + "&text=" + Uri.EscapeDataString(speech));
    }
    public bool Execute()
    {
        string temp = null;
        try {
            string id, slug, speech, queued;
            if (!CPH.TryGetArg("vsProfile", out id) || !CPH.TryGetArg("vsSlug", out slug) ||
                !CPH.TryGetArg("vsSpeech", out speech) || !CPH.TryGetArg("vsQueued", out queued))
                throw new Exception("Use a chat command to run this action.");
            if ((DateTime.UtcNow - new DateTime(long.Parse(queued), DateTimeKind.Utc)).TotalSeconds > 120) {
                CPH.LogWarn("[Streamer.Bot VoiceStudio TTS] Skipped a request waiting over two minutes."); return true;
            }
            int volume, boost;
            bool reduceHum;
            if (!CPH.TryGetArg("volumePercent", out volume) || volume < 0 || volume > 100 ||
                !CPH.TryGetArg("gainDb", out boost) || boost < -20 || boost > 12 ||
                !CPH.TryGetArg("reduceHum", out reduceHum))
                throw new Exception("Check volumePercent (0–100), gainDb (-20 to 12) and reduceHum (True/False).");
            string current;
            if (!VoiceApi.Profiles().TryGetValue(slug, out current) || current != id)
                throw new Exception("Profile changed while queued. Send the command again.");
            var req = VoiceApi.Request("/generate", 180000);
            req.Method = "POST";
            req.ContentType = "application/x-www-form-urlencoded; charset=utf-8";
            var payload = Form(id, ChessSpeech.Translate(speech));
            req.ContentLength = payload.Length;
            using (var body = req.GetRequestStream()) body.Write(payload, 0, payload.Length);
            byte[] wav;
            using (var response = (HttpWebResponse)req.GetResponse()) {
                if (response.StatusCode != HttpStatusCode.OK || !response.ContentType.StartsWith("audio/wav", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("Expected an audio/wav response.");
                if (!String.IsNullOrEmpty(response.Headers["X-OmniVoice-Dropped-Chunks"]) &&
                    response.Headers["X-OmniVoice-Dropped-Chunks"] != "0") throw new InvalidDataException("VoiceStudio reported missing speech. Skipped incomplete take.");
                using (var input = response.GetResponseStream())
                using (var memory = new MemoryStream()) {
                    byte[] buf = new byte[32768]; int count;
                    while ((count = input.Read(buf, 0, buf.Length)) > 0) {
                        if (memory.Length + count > 32000000) throw new InvalidDataException("WAV exceeds 32 MB.");
                        memory.Write(buf, 0, count);
                    }
                    wav = memory.ToArray();
                }
                if (response.ContentLength >= 0 && response.ContentLength != wav.Length)
                    throw new InvalidDataException("Incomplete WAV download.");
            }
            VoiceAudio.Process(wav, boost, reduceHum);
            temp = Path.Combine(Path.GetTempPath(), "streamerbot-voicestudio-tts-" + Guid.NewGuid().ToString("N") + ".wav");
            File.WriteAllBytes(temp, wav);
            double duration = CPH.PlaySound(temp, volume / 100f, true);
            if (duration <= 0) throw new Exception("Playback reported no audio. Check the Streamer.bot output device.");
            CPH.LogInfo("[Streamer.Bot VoiceStudio TTS] Played !" + slug + " (" + duration.ToString("0.0", CultureInfo.InvariantCulture) + " s).");
            return true;
        }
        catch (WebException e) {
            if (e.Response != null) e.Response.Close();
            CPH.LogError("[Streamer.Bot VoiceStudio TTS] " + e.Message + " Check that VoiceStudio is open and the selected engine is ready."); return false;
        }
        catch (Exception e) { CPH.LogError("[Streamer.Bot VoiceStudio TTS] " + e.Message); return false; }
        finally {
            if (temp != null) try { File.Delete(temp); } catch (IOException) { CPH.LogWarn("[Streamer.Bot VoiceStudio TTS] Temporary WAV still in use."); }
        }
    }
}
