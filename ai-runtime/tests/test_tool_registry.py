"""Tool Marketplace permission enforcement (ARCHITECTURE.md §8) — an agent may
only invoke a tool if its manifest permissions include the tool's required one."""

from app.tools.base import Tool, ToolMetadata, ToolResult
from app.tools.registry import ToolRegistry


class _GatedTool(Tool):
    @property
    def metadata(self) -> ToolMetadata:
        return ToolMetadata(name="gated", description="test", input_schema={}, required_permission="gated_permission")

    async def execute(self, params: dict) -> ToolResult:
        return ToolResult(success=True, output="ran")


class _OpenTool(Tool):
    @property
    def metadata(self) -> ToolMetadata:
        return ToolMetadata(name="open", description="test", input_schema={}, required_permission=None)

    async def execute(self, params: dict) -> ToolResult:
        return ToolResult(success=True, output="ran")


class TestToolRegistry:
    async def test_unknown_tool_returns_failure_not_exception(self):
        registry = ToolRegistry()
        result = await registry.invoke("nonexistent", [], {})
        assert not result.success
        assert "Unknown tool" in result.error

    async def test_denies_when_agent_lacks_required_permission(self):
        registry = ToolRegistry()
        registry.register(_GatedTool())

        result = await registry.invoke("gated", agent_permissions=["something_else"], params={})
        assert not result.success
        assert "Permission" in result.error

    async def test_allows_when_agent_has_required_permission(self):
        registry = ToolRegistry()
        registry.register(_GatedTool())

        result = await registry.invoke("gated", agent_permissions=["gated_permission"], params={})
        assert result.success
        assert result.output == "ran"

    async def test_tool_with_no_required_permission_is_always_allowed(self):
        registry = ToolRegistry()
        registry.register(_OpenTool())

        result = await registry.invoke("open", agent_permissions=[], params={})
        assert result.success

    async def test_missing_required_param_is_rejected_before_execute(self):
        class RequiresFoo(Tool):
            @property
            def metadata(self) -> ToolMetadata:
                return ToolMetadata(name="needs_foo", description="test", input_schema={"required": ["foo"]})

            async def execute(self, params: dict) -> ToolResult:
                return ToolResult(success=True, output=params["foo"])

        registry = ToolRegistry()
        registry.register(RequiresFoo())

        result = await registry.invoke("needs_foo", [], {})
        assert not result.success
        assert "foo" in result.error
