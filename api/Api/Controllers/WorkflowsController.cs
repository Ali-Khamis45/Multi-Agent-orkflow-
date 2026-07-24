using AiAgentsTeam.Application.Workflows.Commands;
using AiAgentsTeam.Application.Workflows.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace AiAgentsTeam.Api.Controllers;

/// <summary>Workflow Engine endpoints (ARCHITECTURE.md §23) — building and running a DAG.</summary>
[ApiController]
[Route("api/workflows")]
public sealed class WorkflowsController(ISender sender) : ControllerBase
{
    public sealed record CreateRunRequest(Guid WorkspaceId, string Goal);

    [HttpPost("runs")]
    public async Task<ActionResult<Guid>> CreateRun(CreateRunRequest request, CancellationToken ct)
    {
        var id = await sender.Send(new CreateWorkflowRunCommand(request.WorkspaceId, request.Goal), ct);
        return Ok(id);
    }

    public sealed record AddNodeRequest(string Name, string TaskType, string InputsJson, bool RequiresApproval = false);

    [HttpPost("runs/{runId:guid}/nodes")]
    public async Task<ActionResult<Guid>> AddNode(Guid runId, AddNodeRequest request, CancellationToken ct)
    {
        var id = await sender.Send(
            new AddTaskNodeCommand(runId, request.Name, request.TaskType, request.InputsJson, request.RequiresApproval), ct);
        return Ok(id);
    }

    public sealed record AddDependencyRequest(Guid PredecessorNodeId, Guid SuccessorNodeId);

    [HttpPost("runs/{runId:guid}/edges")]
    public async Task<ActionResult<Guid>> AddDependency(Guid runId, AddDependencyRequest request, CancellationToken ct)
    {
        var id = await sender.Send(
            new AddTaskDependencyCommand(runId, request.PredecessorNodeId, request.SuccessorNodeId), ct);
        return Ok(id);
    }

    [HttpPost("runs/{runId:guid}/start")]
    public async Task<IActionResult> Start(Guid runId, CancellationToken ct)
    {
        await sender.Send(new StartWorkflowRunCommand(runId), ct);
        return NoContent();
    }

    [HttpPost("runs/{runId:guid}/reschedule")]
    public async Task<IActionResult> Reschedule(Guid runId, CancellationToken ct)
    {
        await sender.Send(new RescheduleWorkflowRunCommand(runId), ct);
        return NoContent();
    }

    [HttpGet("runs/{runId:guid}")]
    public async Task<ActionResult<WorkflowRunDto>> Get(Guid runId, CancellationToken ct)
    {
        var dto = await sender.Send(new GetWorkflowRunQuery(runId), ct);
        return dto is null ? NotFound() : Ok(dto);
    }
}
