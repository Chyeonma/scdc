import base64
import importlib.util
from pathlib import Path
import tempfile
import unittest

spec = importlib.util.spec_from_file_location('community_secrets', Path(__file__).resolve().parents[1]/'init_community_database_secrets.py')
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)


class CommunitySecretTests(unittest.TestCase):
    def test_adopts_existing_key_and_never_rotates_on_repeat(self):
        with tempfile.TemporaryDirectory() as root:
            directory = Path(root)/'keys'
            key = base64.b64encode(b'x'*32).decode()
            module.initialize(directory, key)
            before = {p.name: p.read_bytes() for p in directory.iterdir()}
            module.initialize(directory, key)
            self.assertEqual(before, {p.name: p.read_bytes() for p in directory.iterdir()})
            with self.assertRaisesRegex(ValueError, 'differs'):
                module.initialize(directory, base64.b64encode(b'y'*32).decode())
            self.assertEqual(before, {p.name: p.read_bytes() for p in directory.iterdir()})

    def test_partial_and_symlink_sets_are_rejected(self):
        with tempfile.TemporaryDirectory() as root:
            directory = Path(root)/'keys'
            directory.mkdir()
            (directory/module.FILES[0]).write_text('preserve')
            key = base64.b64encode(b'x'*32).decode()
            with self.assertRaisesRegex(ValueError, 'Partial'):
                module.initialize(directory, key)
            (directory/module.FILES[0]).unlink()
            (directory/module.FILES[0]).symlink_to(Path(root)/'outside')
            with self.assertRaisesRegex(ValueError, 'symlink'):
                module.initialize(directory, key)
