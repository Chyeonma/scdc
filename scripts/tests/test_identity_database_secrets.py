import importlib.util
from pathlib import Path
import tempfile
import unittest


spec = importlib.util.spec_from_file_location('identity_secrets', Path(__file__).resolve().parents[1] / 'init_identity_database_secrets.py')
secrets = importlib.util.module_from_spec(spec)
spec.loader.exec_module(secrets)


class IdentityDatabaseSecretsTests(unittest.TestCase):
    def test_restart_preserves_independent_passwords_and_private_directory(self):
        with tempfile.TemporaryDirectory() as parent:
            path = Path(parent) / 'secrets'
            secrets.initialize(path)
            before = [(path / name).read_bytes() for name in secrets.FILES]
            self.assertNotEqual(*before)
            self.assertTrue(all(len(value.strip()) >= 32 for value in before))
            secrets.initialize(path)
            self.assertEqual(before, [(path / name).read_bytes() for name in secrets.FILES])
            self.assertEqual(path.stat().st_mode & 0o777, 0o700)

    def test_partial_or_symlink_does_not_rotate_existing_credentials(self):
        with tempfile.TemporaryDirectory() as parent:
            path = Path(parent) / 'secrets'
            path.mkdir()
            (path / secrets.FILES[0]).write_bytes(b'keep-existing-password')
            with self.assertRaisesRegex(ValueError, 'Partial'):
                secrets.initialize(path)
            self.assertEqual((path / secrets.FILES[0]).read_bytes(), b'keep-existing-password')
            linked = Path(parent) / 'linked'
            linked.symlink_to(path, target_is_directory=True)
            with self.assertRaisesRegex(ValueError, 'symlink'):
                secrets.initialize(linked)
