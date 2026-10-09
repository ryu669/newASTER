"""Exercise waveform rejection using disposable PCM fixtures, never game assets."""
import importlib.util
from pathlib import Path
import sys
import tempfile
import unittest
from unittest.mock import patch
import wave
import array

spec = importlib.util.spec_from_file_location('audio_audit', Path(__file__).with_name('audit-plan11-9-audio.py'))
audit = importlib.util.module_from_spec(spec)
spec.loader.exec_module(audit)


class WaveformValidationTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        self.audio = self.root / 'game/unity/Assets/Game/Resources/Audio'
        self.audio.mkdir(parents=True)
        actual = Path(__file__).resolve().parent.parent / 'game/unity/Assets/Game/Resources/Audio'
        # Preserve every actual filename; substitute tiny, identical disposable waveforms.
        for source in actual.glob('*.wav'):
            self.write(source.name)
        self.target = 'plan9-bgm-title-candidate-v1.wav'

    def write(self, name, amplitude=1000, frames=100, width=2):
        with wave.open(str(self.audio / name), 'wb') as stream:
            stream.setparams((1, width, 22050, 0, 'NONE', 'not compressed'))
            samples = array.array('h', [amplitude, -amplitude] * frames)
            if sys.byteorder != 'little':
                samples.byteswap()
            stream.writeframes(samples.tobytes())

    def run_audit(self):
        with patch.object(audit, '__file__', str(self.root / 'tools/audit.py')), patch.object(sys, 'argv', ['audit']):
            audit.main()

    def test_all_production_groups(self):
        self.run_audit()
        import json
        report = json.loads((self.root / 'tmp/plan11-9/audio-levels.json').read_text())
        self.assertEqual(len(report['clips']), 25)
        self.assertEqual(len(report['rmsSpreadDb']), 5)
        self.assertFalse(report['sourceModified'])

    def test_silence(self):
        self.write(self.target, amplitude=0)
        with self.assertRaisesRegex(ValueError, 'silence or clipping'):
            self.run_audit()

    def test_clipping(self):
        self.write(self.target, amplitude=32767)
        with self.assertRaisesRegex(ValueError, 'silence or clipping'):
            self.run_audit()

    def test_empty(self):
        self.write(self.target, frames=0)
        with self.assertRaisesRegex(ValueError, 'expected uncompressed'):
            self.run_audit()

    def test_format(self):
        self.write(self.target, width=1)
        with self.assertRaisesRegex(ValueError, 'expected uncompressed'):
            self.run_audit()

    def test_bgm_spread(self):
        self.write(self.target, amplitude=10000)
        with self.assertRaisesRegex(ValueError, 'RMS spread'):
            self.run_audit()


if __name__ == '__main__':
    unittest.main()
