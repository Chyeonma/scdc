import base64
import importlib.util
from pathlib import Path
import tempfile
import unittest


spec = importlib.util.spec_from_file_location(
    'community_preview_secrets', Path(__file__).resolve().parents[1] / 'init_community_preview_secrets.py')
preview = importlib.util.module_from_spec(spec)
spec.loader.exec_module(preview)


class CommunityPreviewSecretsTests(unittest.TestCase):
    def test_restart_preserves_independent_credentials_and_only_a_public_jwt_key(self):
        with tempfile.TemporaryDirectory() as parent:
            path = Path(parent) / 'secrets'
            preview.initialize(path)
            before = {name: (path / name).read_bytes() for name in preview.FILES}
            self.assertNotEqual(before[preview.FILES[0]], before[preview.FILES[1]])
            self.assertEqual(len(base64.b64decode(before[preview.FILES[2]], validate=False)), 32)
            self.assertIn(b'BEGIN PUBLIC KEY', before[preview.FILES[3]])
            self.assertEqual(set(item.name for item in path.iterdir()), set(preview.FILES))
            self.assertEqual(path.stat().st_mode & 0o777, 0o700)
            self.assertTrue(all((path / name).stat().st_mode & 0o777 == 0o444 for name in preview.FILES))
            preview.initialize(path)
            self.assertEqual(before, {name: (path / name).read_bytes() for name in preview.FILES})

    def test_partial_secrets_are_rejected_without_replacing_existing_credentials(self):
        with tempfile.TemporaryDirectory() as parent:
            path = Path(parent) / 'secrets'
            path.mkdir()
            existing = path / preview.FILES[0]
            existing.write_text('preserve-existing-password')
            with self.assertRaisesRegex(ValueError, 'Partial'):
                preview.initialize(path)
            self.assertEqual(existing.read_text(), 'preserve-existing-password')

    def test_symlinked_directory_or_secret_is_rejected(self):
        with tempfile.TemporaryDirectory() as parent:
            path = Path(parent) / 'secrets'
            path.mkdir()
            linked = Path(parent) / 'linked'
            linked.symlink_to(path, target_is_directory=True)
            with self.assertRaisesRegex(ValueError, 'symlink'):
                preview.initialize(linked)
            (path / preview.FILES[0]).symlink_to(Path(parent) / 'outside')
            with self.assertRaisesRegex(ValueError, 'symlink'):
                preview.initialize(path)
