# VoiceStudio Chat Voices

Let Twitch viewers speak through your saved VoiceStudio voices. Type `!voices` to see the list, then try `!lily Hello everyone!` with a profile named Lily. Messages play one at a time through Streamer.bot. You can try it in offline chat.

## Set up once

1. On your Windows streaming PC, open [VoiceStudio](https://github.com/debpalash/VoiceStudio), save a voice profile and check that it can speak in the app.
2. Open [Streamer.bot](https://streamer.bot/) 1.x and connect your Twitch account. Extract the download, click **Import**, drag **VoiceStudio-Chat.sb** into the import window and confirm.
3. In **Actions & Queues → Actions → VoiceStudio Chat**, select **VoiceStudio • Chat and settings**. Right-click its **Core Test** trigger and choose **Test Trigger**. Listen for the complete sentence ending in **“butterfly”**.
4. In your Twitch channel, type **`!voices`**, then use one of the listed commands followed by your message. Keep both apps open while using TTS.

## Add another voice

Save a uniquely named profile in VoiceStudio, then type `!voices` again. **Moon Light** becomes **`!moon-light`**. All uniquely named saved profiles are available to the configured audience. If two names produce the same command, rename one.

## Let the stream hear it

Choose your headphones or speakers as Streamer.bot’s audio output. If OBS already captures that device through Desktop Audio or a mixer, use that existing source. If you capture applications separately, add **Application Audio Capture** for **Streamer.bot.exe**.

Use one capture path and leave OBS monitoring off for that source. Make a short recording and replay it to check that the voice is audible exactly once on the stream track.

## Adjust the defaults

In **VoiceStudio • Chat and settings**, double-click a **Set Argument** entry and change its **Value**.

| Setting | Default | What it controls |
| --- | --- | --- |
| `audience` | everyone | Choose everyone, moderators (includes broadcaster) or broadcaster |
| `characterLimit` | 200 | Maximum message length |
| `userCooldownSeconds` / `globalCooldownSeconds` | 15 / 2 | Wait between requests from one viewer or the whole chat |
| `volumePercent` | 90 | Playback level, from 0 to 100 |
| `gainDb` / `reduceHum` | 5 / True | Gain with peak headroom and optional low-frequency hum reduction |
| `testVoice` | auto | Voice for the private test, e.g. lily |

**If it’s quiet:** try **Test Trigger** first. If the private test works, check the Twitch connection and the command returned by `!voices`. If the private test is silent, open the newest file in the **logs** folder beside **Streamer.bot.exe** and search for **[VoiceStudio]**. You can also open `http://127.0.0.1:3900/profiles` on the same PC to check that VoiceStudio is responding.

**For developers:** the editable C# actions are in `src/`. Run `python src/build.py` from the repository root to rebuild the import. Every generation explicitly requests `effect_preset=raw`. Audio processing preserves the complete WAV, including its final sample.
