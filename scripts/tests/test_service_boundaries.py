"""Build boundaries that assembly-reference tests cannot detect, including linked source."""

from pathlib import Path
import unittest
import xml.etree.ElementTree as ET


ROOT = Path(__file__).resolve().parents[2]
COMMUNITY = ROOT / "services/Community"


class ServiceSourceBoundaryTests(unittest.TestCase):
    def test_community_build_graph_uses_only_owner_source_and_public_dependencies(self):
        pending = list(COMMUNITY.glob("*/*.csproj"))
        seen = set()
        shared = {
            ROOT / "services/Shared/SCDC.Shared.Core/SCDC.Shared.Core.csproj",
            ROOT / "services/SCDC.Contracts/SCDC.Contracts.csproj",
        }
        while pending:
            project = pending.pop().resolve()
            if project in seen:
                continue
            seen.add(project)
            self.assertTrue(project.is_file(), project)
            self.assertTrue(
                project.is_relative_to(COMMUNITY)
                or project in shared
                or (project.parent.name.endswith(".Grpc") and project.suffix == ".csproj"),
                f"Community builds implementation outside its owner: {project}",
            )
            tree = ET.parse(project).getroot()
            for reference in tree.iter("ProjectReference"):
                pending.append((project.parent / reference.attrib["Include"]).resolve())
            for source in tree.iter("Compile"):
                include = source.attrib.get("Include")
                if include is not None:
                    self.assertTrue(
                        (project.parent / include).resolve().is_relative_to(project.parent),
                        f"Source linked from another project: {project}: {include}",
                    )

    def test_messaging_host_has_no_community_implementation_or_linked_source(self):
        project = ROOT / "services/SCDC.Api/SCDC.Api.csproj"
        tree = ET.parse(project).getroot()
        for reference in tree.iter("ProjectReference"):
            target = (project.parent / reference.attrib["Include"]).resolve()
            self.assertFalse(
                target.is_relative_to(COMMUNITY) and not target.parent.name.endswith(".Grpc"),
                f"Messaging host references Community implementation: {target}",
            )
        for source in tree.iter("Compile"):
            include = source.attrib.get("Include")
            if include is not None:
                self.assertTrue((project.parent / include).resolve().is_relative_to(project.parent))

    def test_migration_tool_does_not_depend_on_service_transport(self):
        project = ROOT / "tools/SCDC.DbMigrator/SCDC.DbMigrator.csproj"
        references = {
            (project.parent / node.attrib["Include"]).resolve()
            for node in ET.parse(project).getroot().iter("ProjectReference")
        }
        self.assertEqual(
            {ROOT / "services/Shared/SCDC.Shared.Core/SCDC.Shared.Core.csproj"},
            references,
        )


if __name__ == "__main__":
    unittest.main()
