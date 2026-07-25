import type { Artifact, ReasoningTracePoint, SupervisorDecision, WorkflowRun } from "./types";

function download(filename: string, content: string, mime: string): void {
  const blob = new Blob([content], { type: `${mime};charset=utf-8` });
  const url = URL.createObjectURL(blob);
  const a = document.createElement("a");
  a.href = url;
  a.download = filename;
  document.body.appendChild(a);
  a.click();
  a.remove();
  URL.revokeObjectURL(url);
}

function slug(s: string): string {
  return s.toLowerCase().replace(/[^a-z0-9]+/g, "-").replace(/(^-|-$)/g, "").slice(0, 60);
}

export function exportExecutionSummaryJson(run: WorkflowRun, decisions: SupervisorDecision[], artifacts: Artifact[]) {
  download(
    `execution-summary-${slug(run.goal)}.json`,
    JSON.stringify({ run, supervisorDecisions: decisions, artifacts }, null, 2),
    "application/json",
  );
}

export function exportExecutionSummaryMarkdown(run: WorkflowRun, decisions: SupervisorDecision[], artifacts: Artifact[]) {
  const lines: string[] = [];
  lines.push(`# Execution Summary — ${run.goal}`);
  lines.push("");
  lines.push(`- **Run ID:** \`${run.id}\``);
  lines.push(`- **Status:** ${run.status}`);
  lines.push(`- **Started:** ${new Date(run.createdAt).toLocaleString()}`);
  lines.push(`- **Updated:** ${new Date(run.updatedAt).toLocaleString()}`);
  lines.push("");
  lines.push("## Tasks");
  lines.push("");
  lines.push("| Task | Agent | Status | Confidence | Attempts |");
  lines.push("|---|---|---|---|---|");
  for (const n of run.nodes) {
    lines.push(`| ${n.name} | ${n.assignedAgentName ?? "—"} | ${n.status} | ${n.confidence != null ? `${(n.confidence * 100).toFixed(0)}%` : "—"} | ${n.attemptCount} |`);
  }
  lines.push("");
  lines.push("## Supervisor Decisions");
  lines.push("");
  if (decisions.length === 0) lines.push("_None recorded._");
  for (const d of decisions) {
    lines.push(`- **${d.decisionType}** (${(d.confidence * 100).toFixed(0)}% confidence, ${new Date(d.createdAt).toLocaleString()}): ${d.rationale}`);
  }
  lines.push("");
  lines.push("## Artifacts Produced");
  lines.push("");
  if (artifacts.length === 0) lines.push("_None recorded._");
  for (const a of artifacts) {
    lines.push(`- **${a.name}** (${a.type}, v${a.version}) — by ${a.ownerAgent}`);
  }
  download(`execution-summary-${slug(run.goal)}.md`, lines.join("\n"), "text/markdown");
}

export function exportArtifactsJson(run: WorkflowRun, artifacts: Artifact[]) {
  download(`artifacts-${slug(run.goal)}.json`, JSON.stringify(artifacts, null, 2), "application/json");
}

export function exportGraphJson(run: WorkflowRun) {
  download(
    `graph-${slug(run.goal)}.json`,
    JSON.stringify({ nodes: run.nodes, edges: run.edges }, null, 2),
    "application/json",
  );
}

export function exportReasoningTraceJson(run: WorkflowRun, points: ReasoningTracePoint[]) {
  download(`reasoning-trace-${slug(run.goal)}.json`, JSON.stringify(points, null, 2), "application/json");
}

export function exportTelemetryJson(run: WorkflowRun, points: ReasoningTracePoint[]) {
  const byStage = new Map<string, { count: number; totalMs: number; totalTokens: number }>();
  for (const p of points) {
    const s = byStage.get(p.stage) ?? { count: 0, totalMs: 0, totalTokens: 0 };
    s.count += 1;
    s.totalMs += p.durationMs;
    s.totalTokens += p.tokens ?? 0;
    byStage.set(p.stage, s);
  }
  const summary = Array.from(byStage.entries()).map(([stage, s]) => ({
    stage, count: s.count, avgDurationMs: s.totalMs / s.count, totalTokens: s.totalTokens,
  }));
  download(`telemetry-${slug(run.goal)}.json`, JSON.stringify({ stageSummary: summary, points }, null, 2), "application/json");
}
