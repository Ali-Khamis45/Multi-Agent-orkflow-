"""Tool Sandbox hardening (Phase 1.5 §7) — directory traversal, absolute-path
escape, symlink escape, and oversized-write defenses."""

import pytest

from app.tools.base import ToolError
from app.tools.filesystem_tool import FilesystemTool
from app.tools.sandbox import enforce_max_size, resolve_sandboxed_path


@pytest.fixture
def sandbox_root(tmp_path):
    return tmp_path / "sandbox"


class TestResolveSandboxedPath:
    def test_allows_a_normal_relative_path(self, sandbox_root):
        target = resolve_sandboxed_path(sandbox_root, "notes/todo.txt")
        assert target == (sandbox_root / "notes" / "todo.txt").resolve()

    def test_rejects_dotdot_traversal(self, sandbox_root):
        with pytest.raises(ToolError, match=r"\.\."):
            resolve_sandboxed_path(sandbox_root, "../../etc/passwd")

    def test_rejects_absolute_paths(self, sandbox_root):
        # Message differs by platform (pathlib's is_absolute() only recognizes
        # "/etc/passwd" as absolute on POSIX, where this app actually runs in
        # Docker; on Windows it falls through to the containment check instead)
        # — either way, the path must never resolve inside the sandbox.
        with pytest.raises(ToolError):
            resolve_sandboxed_path(sandbox_root, "/etc/passwd")

    def test_rejects_empty_path(self, sandbox_root):
        with pytest.raises(ToolError):
            resolve_sandboxed_path(sandbox_root, "")

    def test_rejects_symlink_escape(self, sandbox_root):
        sandbox_root.mkdir(parents=True)
        outside = sandbox_root.parent / "outside"
        outside.mkdir()
        (outside / "secret.txt").write_text("top secret")

        link = sandbox_root / "escape"
        try:
            link.symlink_to(outside, target_is_directory=True)
        except OSError:
            pytest.skip("Creating symlinks requires elevated privileges on this platform (e.g. Windows without Developer Mode).")

        with pytest.raises(ToolError, match="escapes"):
            resolve_sandboxed_path(sandbox_root, "escape/secret.txt")


class TestEnforceMaxSize:
    def test_allows_content_under_the_limit(self):
        enforce_max_size("small", max_bytes=1000)  # should not raise

    def test_rejects_content_over_the_limit(self):
        with pytest.raises(ToolError, match="exceeds"):
            enforce_max_size("x" * 100, max_bytes=10)


class TestFilesystemTool:
    @pytest.fixture
    def tool(self, sandbox_root):
        return FilesystemTool(root=sandbox_root, max_bytes=1000)

    async def test_write_then_read_round_trips(self, tool):
        write_result = await tool.execute({"operation": "write", "path": "a.txt", "content": "hello"})
        assert write_result.success

        read_result = await tool.execute({"operation": "read", "path": "a.txt"})
        assert read_result.success
        assert read_result.output == "hello"

    async def test_read_missing_file_fails_gracefully(self, tool):
        result = await tool.execute({"operation": "read", "path": "missing.txt"})
        assert not result.success

    async def test_traversal_attempt_is_rejected_not_raised_through_registry(self, tool):
        with pytest.raises(ToolError):
            await tool.execute({"operation": "write", "path": "../escape.txt", "content": "x"})

    async def test_oversized_write_is_rejected(self, tool):
        with pytest.raises(ToolError):
            await tool.execute({"operation": "write", "path": "big.txt", "content": "x" * 2000})
