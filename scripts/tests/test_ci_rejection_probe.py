"""Temporary GitHub CI rejection probe; removed before the PR is ready."""
import unittest

class CiRejectionProbe(unittest.TestCase):
    def test_intentional_failure_must_make_required_ci_fail(self):
        self.fail("Intentional CI gate verification; do not merge this revision.")
