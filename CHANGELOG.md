# Streamer.Bot VoiceStudio TTS changelog

## v1.1.1 · unified plugin name

- The import file, Streamer.bot actions, queues and logs now use the same plugin name. Reimport both actions to update their labels.

## v1.1.0 · The Chess Update

- Any saved voice can read a chess move or a full line of moves aloud. Try `!lola Nc3` for “knight to C three” or `!lola Ref7` for “rook from the E file to F seven.”
- Captures, castling, promotions, check and checkmate are supported. Chess pronunciation kicks in only when the whole message is chess notation.

## v1.0.3

- Start a voice name with a period to hide it from chat commands, `!voices` and the test voice picker.

## v1.0.2

- Added `!voices` so viewers can see which voice commands are available.

## v1.0.1

- Fixed an audio-processing overflow that could prevent generated speech from playing.

## v1.0.0

- Added Twitch chat TTS with automatic voice discovery, queued playback and adjustable volume.
