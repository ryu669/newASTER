"""Original deterministic ambience and short life sounds; no sampled third-party audio."""
from pathlib import Path
import math, random, struct, wave, json, hashlib

ROOT = Path(__file__).resolve().parents[1]
DEST = ROOT / 'game/unity/Assets/Game/Resources/Audio'
RATE = 24000
loops = ['forest', 'wind', 'water', 'rain', 'snow', 'phenomenon']
effects = ['step', 'seat', 'book', 'table', 'water-use', 'furniture']
records = []
for index, name in enumerate(loops + effects):
    rng = random.Random(1200 + index)
    duration = 8 if name in loops else .45
    count = round(duration * RATE)
    previous = 0.0
    data = bytearray()
    for n in range(count):
        t = n / RATE
        noise = rng.uniform(-1, 1)
        previous = .965 * previous + .035 * noise
        fade = min(1, t * 8, (duration-t) * 8)
        if name == 'forest':
            chirp = max(0, math.sin(t * .9)) ** 30
            value = previous * .12 + .035 * chirp * math.sin(t * (1700 + 100 * math.sin(t * 7)) * 2 * math.pi)
        elif name in ['wind', 'snow']:
            value = previous * (.23 if name == 'wind' else .12) * (.65 + .3 * math.sin(t * 1.3))
        elif name in ['rain', 'water']:
            value = noise * (.035 if name == 'rain' else .015) + previous * .2
        elif name == 'phenomenon':
            value = .014 * (math.sin(t * 220 * 2 * math.pi) + math.sin(t * 330 * 2 * math.pi)) * (.5 + .4 * math.sin(t * .8))
        else:
            envelope = math.exp(-t * (18 if name == 'step' else 11))
            value = envelope * (previous * .6 + .05 * math.sin(t * (130 + index * 24) * 2 * math.pi))
            if name in ['book', 'water-use']: value += noise * .025 * math.exp(-t * 8)
        data.extend(struct.pack('<h', round(max(-1, min(1, value * fade)) * 32767)))
    path = DEST / ('garden-life-' + name + '-v1.wav')
    with wave.open(str(path), 'wb') as output:
        output.setnchannels(1); output.setsampwidth(2); output.setframerate(RATE); output.writeframes(data)
    records.append({'resourcePath': 'Audio/' + path.stem, 'duration': duration, 'sampleRate': RATE, 'sha256': hashlib.sha256(path.read_bytes()).hexdigest()})
(ROOT / 'game/art-source/garden-life-v1/audio-generation.json').write_text(json.dumps({'generator': 'tools/generate-garden-life-audio.py', 'source': 'original deterministic synthesis', 'assets': records}, indent=2) + '\n', encoding='utf8')
print('GARDEN_LIFE_AUDIO_PASS', len(records), 'original clips')
