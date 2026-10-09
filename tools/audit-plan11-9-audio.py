"""Measure production PCM clips without changing them; listening remains a separate check."""
import argparse
import array
import hashlib
import json
import math
from pathlib import Path
import sys
import wave


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--output', default='tmp/plan11-9/audio-levels.json')
    args = parser.parse_args()
    repo = Path(__file__).resolve().parent.parent
    clips = []
    for kind, names in [('bgm', ['title', 'garden', 'battle', 'adv']),
                        ('se', ['confirm', 'cancel', 'page', 'unlock'])]:
        for name in names:
            path = repo / f'game/unity/Assets/Game/Resources/Audio/plan9-{kind}-{name}-candidate-v1.wav'
            with wave.open(str(path), 'rb') as source:
                if source.getsampwidth() != 2 or source.getcomptype() != 'NONE':
                    raise ValueError(f'{path.name}: expected uncompressed 16-bit PCM')
                samples = array.array('h', source.readframes(source.getnframes()))
                if sys.byteorder != 'little':
                    samples.byteswap()
                peak = max(abs(sample) for sample in samples) / 32768
                rms = math.sqrt(sum(sample * sample for sample in samples) / len(samples)) / 32768
                if rms <= 0 or peak >= .99:
                    raise ValueError(f'{path.name}: silence or clipping')
                clips.append(dict(kind=kind, name=name, sampleRate=source.getframerate(),
                                  channels=source.getnchannels(), seconds=source.getnframes() / source.getframerate(),
                                  peak=peak, rmsDbFS=20 * math.log10(rms),
                                  sha256=hashlib.sha256(path.read_bytes()).hexdigest()))
    spreads = {}
    for kind in ['bgm', 'se']:
        levels = [clip['rmsDbFS'] for clip in clips if clip['kind'] == kind]
        spreads[kind] = max(levels) - min(levels)
        if spreads[kind] > 6:
            raise ValueError(f'{kind}: RMS spread exceeds provisional 6 dB gate: {spreads[kind]}')
    report = dict(status='waveform-pass', clips=clips, rmsSpreadDb=spreads,
                  provisionalSpreadLimitDb=6, sourceModified=False,
                  scope='Full-clip PCM peak and RMS, not perceived loudness, mixer output, or listening acceptance.',
                  listeningAcceptance='manual-pending')
    output = Path(args.output)
    if not output.is_absolute():
        output = repo / output
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(json.dumps(report, indent=2) + '\n', encoding='utf-8')
    print(f'PLAN11_9_AUDIO_LEVEL_PASS clips=8 seSpreadDb={spreads["se"]:.3f} bgmSpreadDb={spreads["bgm"]:.3f} listening=0')


if __name__ == '__main__':
    main()
