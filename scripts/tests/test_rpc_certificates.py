import importlib.util
from pathlib import Path
import subprocess
import tempfile
import unittest


spec = importlib.util.spec_from_file_location('rpc', Path(__file__).resolve().parents[1] / 'init_rpc_certificates.py')
rpc = importlib.util.module_from_spec(spec)
spec.loader.exec_module(rpc)


class RpcCertificatesTests(unittest.TestCase):
    def test_community_identity_is_opt_in_and_does_not_rotate_an_existing_legacy_set(self):
        with tempfile.TemporaryDirectory() as parent:
            legacy = Path(parent) / 'legacy'
            rpc.initialize(legacy)
            before = {name: (legacy / name).read_bytes() for name in rpc.FILES}
            with self.assertRaisesRegex(ValueError, 'Partial'):
                rpc.initialize(legacy, include_community=True)
            self.assertEqual(before, {name: (legacy / name).read_bytes() for name in rpc.FILES})
            isolated = Path(parent) / 'integrated'
            rpc.initialize(isolated, include_community=True)
            names = rpc.FILES + ['community-service.crt', 'community-service.key']
            before = {name: (isolated / name).read_bytes() for name in names}
            subprocess.run(['openssl', 'verify', '-CAfile', str(isolated / 'ca.crt'), '-purpose', 'sslclient',
                            '-verify_hostname', 'community-service', str(isolated / 'community-service.crt')], check=True, capture_output=True)
            rpc.initialize(isolated, include_community=True)
            self.assertEqual(before, {name: (isolated / name).read_bytes() for name in names})

    def test_provisioned_identities_validate_and_rerun_preserves_all_keys(self):
        with tempfile.TemporaryDirectory() as parent:
            directory = Path(parent) / 'rpc'
            rpc.initialize(directory)
            before = {name: (directory / name).read_bytes() for name in rpc.FILES}
            for peer in ('identity-service', 'chat-service'):
                for purpose in ('sslclient', 'sslserver'):
                    subprocess.run(['openssl', 'verify', '-CAfile', str(directory / 'ca.crt'),
                                    '-purpose', purpose, '-verify_hostname', peer,
                                    str(directory / (peer + '.crt'))], check=True, capture_output=True)
            rpc.initialize(directory)
            self.assertEqual(before, {name: (directory / name).read_bytes() for name in rpc.FILES})
            self.assertEqual(directory.stat().st_mode & 0o777, 0o700)
            self.assertEqual((directory / 'ca.key').stat().st_mode & 0o777, 0o600)

    def test_partial_set_and_symlink_are_rejected_without_overwriting(self):
        with tempfile.TemporaryDirectory() as parent:
            directory = Path(parent) / 'partial'
            directory.mkdir()
            (directory / 'ca.key').write_bytes(b'preserve-me')
            with self.assertRaisesRegex(ValueError, 'Partial'):
                rpc.initialize(directory)
            self.assertEqual((directory / 'ca.key').read_bytes(), b'preserve-me')
            linked = Path(parent) / 'linked'
            linked.symlink_to(directory, target_is_directory=True)
            with self.assertRaisesRegex(ValueError, 'symlink'):
                rpc.initialize(linked)

    def test_mismatched_private_key_is_rejected(self):
        with tempfile.TemporaryDirectory() as parent:
            directory = Path(parent) / 'rpc'
            rpc.initialize(directory)
            key = directory / 'identity-service.key'
            key.chmod(0o600)
            key.write_bytes((directory / 'chat-service.key').read_bytes())
            with self.assertRaisesRegex(ValueError, 'do not match'):
                rpc.initialize(directory)


if __name__ == '__main__':
    unittest.main()
