import base64
import gzip
import json
import uuid
from pathlib import Path

work = Path(__file__).resolve().parent
out = work.parent
out.mkdir(exist_ok=True)
plugin_name = "Streamer.Bot VoiceStudio TTS"
worker = "390710cd-85ea-4845-bc86-27d951fc327d"

def uid(name):
    return str(uuid.uuid5(uuid.NAMESPACE_URL, "urn:voicestudio:chat:v1:" + name))

def queue(key, label):
    return dict(id=uid(key), name=plugin_name + " • " + label, blocking=True)

def trigger(name, number):
    item = dict(id=uid(name), type=number, enabled=True, exclusions=[])
    if number == 702:
        item["variables"] = {}
    return item

def sub(key, index, kind, **props):
    return dict(props, id=uid(key), type=kind, weight=0.0, group=None, enabled=True, index=index)

def code(name, index, parts):
    body = "\n".join((work / part).read_text(encoding="utf8") for part in parts)
    base = "C:\\Windows\\Microsoft.NET\\Framework64\\v4.0.30319\\"
    refs = [base + n + ".dll" for n in ["mscorlib", "System", "System.Core", "System.Web.Extensions"]]
    return sub(name, index, 99999, name=name, description="", references=refs,
               byteCode=base64.b64encode(body.encode()).decode(), precompile=True,
               delayStart=False, saveResultToVariable=False, saveToVariable="")

def action(name, action_id, queue_id, triggers, subs):
    return dict(id=action_id, queue=queue_id, enabled=True, excludeFromHistory=False,
                excludeFromPending=False, name=name, group=plugin_name,
                alwaysRun=False, randomAction=False, concurrent=False, triggers=triggers,
                actions=subs, actionGroups=[], collapsedGroups=[])

intake, playback = queue("VoiceStudio chat intake", "Chat intake"), queue("VoiceStudio speech playback", "Speech playback")
settings = dict(audience="everyone", characterLimit=200, userCooldownSeconds=15,
                globalCooldownSeconds=2, volumePercent=90, gainDb=5, reduceHum=True, testVoice="auto")
args = [sub(k, i, 123, variableName=k, value=v, autoType=True) for i,(k,v) in enumerate(settings.items())]
receive = action(plugin_name + " • Chat and settings", uid("intake-action"), intake["id"],
                 [trigger("twitch-chat",133),trigger("offline-test",702)],
                 args + [code("Discover and queue", len(args), ["Receive.cs","Common.cs"])])
play = action(plugin_name + " • Generate and play", worker, playback["id"], [],
              [code("Generate and play",0,["Speak.cs","Common.cs","Audio.cs","ChessSpeech.cs"])])
doc = dict(version=11, exportedFrom="0.2.6", minimumVersion="1.0.0",
           meta=dict(name=plugin_name,author="Lilith & Lola",version="1.1.1",
                     description="Saved VoiceStudio profiles as Twitch chat voices. Includes a local voice check.",
                     autoRunAction=None,minimumVersion="1.0.0"),
           data=dict(actions=[receive,play],queues=[intake,playback],commands=[],
                     websocketServers=[],websocketClients=[],timers=[]))
encoded = json.dumps(doc,ensure_ascii=False).encode("utf8")
(out / (plugin_name + ".sb")).write_bytes(base64.b64encode(b"SBAE"+gzip.compress(encoded,mtime=0)))
