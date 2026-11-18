internal static class VoiceAudio
{
    // A shallow low shelf attenuates low-frequency hum. No gate, fades or trimming.
    public static void Process(byte[] wav, int gainDb, bool hum)
    {
        if (wav.Length < 44 || Tag(wav, 0) != "RIFF" || Tag(wav, 8) != "WAVE" ||
            BitConverter.ToUInt32(wav, 4) != wav.Length - 8) throw new InvalidDataException("Incomplete RIFF/WAVE.");
        int rate = 0, channels = 0, start = -1, size = 0;
        for (int pos = 12; pos + 8 <= wav.Length;) {
            string tag = Tag(wav, pos);
            int length = BitConverter.ToInt32(wav, pos + 4);
            int data = pos + 8;
            if (length < 0 || length > wav.Length - data) throw new InvalidDataException("Truncated WAV chunk.");
            if (tag == "fmt ") {
                if (length < 16 || BitConverter.ToUInt16(wav, data) != 1 || BitConverter.ToUInt16(wav, data + 14) != 16)
                    throw new InvalidDataException("Expected 16-bit PCM.");
                channels = BitConverter.ToUInt16(wav, data + 2);
                rate = BitConverter.ToInt32(wav, data + 4);
            }
            if (tag == "data") { start = data; size = length; }
            pos = data + length + (length & 1);
        }
        if (start < 0 || size == 0 || channels < 1 || channels > 8 || rate < 8000 || rate > 192000 || size % (2 * channels) != 0)
            throw new InvalidDataException("Invalid PCM format or frame count.");
        int samples = size / 2;
        var filtered = new float[samples];
        var low = new double[channels];
        double alpha = 1 - Math.Exp(-2 * Math.PI * 110 / rate);
        double peak = 0;
        for (int i = 0; i < samples; i++) {
            double value = BitConverter.ToInt16(wav, start + 2*i);
            if (hum) {
                int channel = i % channels;
                low[channel] += alpha * (value - low[channel]);
                value -= 0.50 * low[channel];
            }
            filtered[i] = (float)value;
            peak = Math.Max(peak, Math.Abs(value));
        }
        // Apply a single gain to the complete take, leaving 1 dB of peak headroom.
        double gain = Math.Min(Math.Pow(10, gainDb / 20.0), 29204.0 / Math.Max(1, peak));
        for (int i = 0; i < samples; i++) {
            int sample = (int)Math.Round(filtered[i] * gain);
            wav[start + 2*i] = (byte)(sample & 0xff);
            wav[start + 2*i + 1] = (byte)((sample >> 8) & 0xff);
        }
    }
    private static string Tag(byte[] wav, int index) { return Encoding.ASCII.GetString(wav, index, 4); }
}
