import type { Agent, AgentMetrics, Artifact, ReasoningTelemetry, WorkflowRun } from "./types";

export interface HealthMetric {
  key: string;
  label: string;
  score: number | null; // 0-100, or null if not measurable yet
  detail: string;
  methodology: string;
}

export interface HealthReport {
  metrics: HealthMetric[];
  overall: number | null;
}

const clamp = (n: number) => Math.max(0, Math.min(100, n));

/**
 * Every score below is derived from real platform data — nothing here is
 * invented. Two categories (Security, Maintainability) have no measurable
 * signal yet because the subsystems that would produce one (a Security
 * Analyzer, static-analysis/lint integration) aren't built — they're
 * reported as unmeasured rather than guessed at.
 */
export function computeHealthReport(
  runs: WorkflowRun[],
  agentMetrics: AgentMetrics[],
  agents: Agent[],
  artifacts: Artifact[],
  telemetry: ReasoningTelemetry | undefined,
): HealthReport {
  const metrics: HealthMetric[] = [];

  // Reliability: workflow completion rate + agent success rate, blended.
  const finished = runs.filter((r) => r.status === "Completed" || r.status === "Failed");
  const completionRate = finished.length > 0 ? finished.filter((r) => r.status === "Completed").length / finished.length : null;
  const avgSuccessRate = agentMetrics.length > 0 ? agentMetrics.reduce((s, m) => s + m.successRate, 0) / agentMetrics.length : null;
  const reliability = completionRate != null && avgSuccessRate != null ? clamp((completionRate * 0.5 + avgSuccessRate * 0.5) * 100) : null;
  metrics.push({
    key: "reliability",
    label: "Reliability",
    score: reliability,
    detail: finished.length > 0 ? `${finished.filter((r) => r.status === "Completed").length}/${finished.length} runs completed` : "No finished runs yet",
    methodology: "50% workflow completion rate + 50% average agent success rate.",
  });

  // AI Confidence: average confidence across every recorded reasoning stage.
  const confidencePoints = (telemetry?.recentPoints ?? []).filter((p) => p.confidence != null);
  const avgConfidence = confidencePoints.length > 0 ? confidencePoints.reduce((s, p) => s + (p.confidence ?? 0), 0) / confidencePoints.length : null;
  metrics.push({
    key: "confidence",
    label: "AI Confidence",
    score: avgConfidence != null ? clamp(avgConfidence * 100) : null,
    detail: confidencePoints.length > 0 ? `avg over ${confidencePoints.length} reasoning stages` : "No confidence samples yet",
    methodology: "Average self-reported confidence across every ConfidenceEvaluation reasoning stage.",
  });

  // Testing: QA-task success rate as a proxy (real coverage % isn't tracked).
  const qaNodes = runs.flatMap((r) => r.nodes).filter((n) => n.taskType.toLowerCase().includes("test") || n.taskType.toLowerCase().includes("qa") || n.name.toLowerCase().includes("qa"));
  const qaCompleted = qaNodes.filter((n) => n.status === "Completed");
  const testingScore = qaNodes.length > 0 ? clamp((qaCompleted.length / qaNodes.length) * 100) : null;
  metrics.push({
    key: "testing",
    label: "Testing",
    score: testingScore,
    detail: qaNodes.length > 0 ? `${qaCompleted.length}/${qaNodes.length} QA tasks completed` : "No QA tasks run yet",
    methodology: "Share of QA/test-type tasks that completed successfully. A proxy — line/branch coverage isn't instrumented.",
  });

  // Architecture: architecture-design task confidence.
  const archNodes = runs.flatMap((r) => r.nodes).filter((n) => n.taskType.toLowerCase().includes("architect") || n.name.toLowerCase().includes("architecture"));
  const archWithConfidence = archNodes.filter((n) => n.confidence != null);
  const archScore = archWithConfidence.length > 0 ? clamp((archWithConfidence.reduce((s, n) => s + (n.confidence ?? 0), 0) / archWithConfidence.length) * 100) : null;
  metrics.push({
    key: "architecture",
    label: "Architecture",
    score: archScore,
    detail: archWithConfidence.length > 0 ? `avg confidence over ${archWithConfidence.length} design task(s)` : "No architecture tasks run yet",
    methodology: "Average self-reported confidence of System Architect tasks.",
  });

  // Documentation: docs-to-total artifact ratio.
  const docArtifacts = artifacts.filter((a) => a.type === "Markdown");
  const docScore = artifacts.length > 0 ? clamp((docArtifacts.length / artifacts.length) * 100) : null;
  metrics.push({
    key: "documentation",
    label: "Documentation",
    score: docScore,
    detail: artifacts.length > 0 ? `${docArtifacts.length}/${artifacts.length} artifacts are docs` : "No artifacts produced yet",
    methodology: "Share of produced artifacts that are Markdown documentation (requirements, architecture docs, reports).",
  });

  // Performance: avg reasoning-stage latency, scored against a 500ms baseline (Phase 1.5 §12).
  const avgStageMs = agentMetrics.length > 0 ? agentMetrics.reduce((s, m) => s + m.avgStageDurationMs, 0) / agentMetrics.length : null;
  const BASELINE_MS = 500;
  const perfScoreClamped = avgStageMs != null ? clamp(avgStageMs <= BASELINE_MS ? 100 : Math.max(0, 100 - ((avgStageMs - BASELINE_MS) / BASELINE_MS) * 100)) : null;
  metrics.push({
    key: "performance",
    label: "Performance",
    score: perfScoreClamped,
    detail: avgStageMs != null ? `${avgStageMs.toFixed(0)}ms avg reasoning-stage latency` : "No timing data yet",
    methodology: `Scored against a ${BASELINE_MS}ms per-stage baseline (Phase 1.5 §12 Performance Baseline) — 100% at or under baseline, degrading linearly above it.`,
  });

  // Not measurable from anything the platform currently instruments.
  metrics.push({
    key: "security",
    label: "Security",
    score: null,
    detail: "Not yet measured",
    methodology: "Requires a Security Analyzer subsystem (static vulnerability scanning) — planned, not built.",
  });
  metrics.push({
    key: "maintainability",
    label: "Maintainability",
    score: null,
    detail: "Not yet measured",
    methodology: "Requires static analysis / lint-quality integration over produced code artifacts — planned, not built.",
  });

  const measured = metrics.filter((m) => m.score != null);
  const overall = measured.length > 0 ? clamp(measured.reduce((s, m) => s + (m.score ?? 0), 0) / measured.length) : null;

  return { metrics, overall };
}
