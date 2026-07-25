// Mirrors the .NET API's DTOs exactly (ASP.NET Core's default MVC JSON
// formatter uses camelCase property names, confirmed against live responses).
// Keep field names in sync with api/Application/**/Queries/*.cs.

export type WorkflowRunStatus =
  | "Planning"
  | "Running"
  | "WaitingApproval"
  | "Paused"
  | "Completed"
  | "Failed"
  | "RolledBack";

export type TaskNodeStatus =
  | "Pending"
  | "Ready"
  | "Dispatched"
  | "Running"
  | "Completed"
  | "Failed"
  | "Blocked"
  | "WaitingApproval";

export interface Workspace {
  id: string;
  name: string;
  createdAt: string;
}

export interface TaskNode {
  id: string;
  name: string;
  taskType: string;
  status: TaskNodeStatus;
  assignedAgentName: string | null;
  confidence: number | null;
  riskLevel: string | null;
  attemptCount: number;
  createdAt: string;
  updatedAt: string;
}

export interface TaskEdge {
  predecessorNodeId: string;
  successorNodeId: string;
}

export interface WorkflowRun {
  id: string;
  workspaceId: string;
  correlationId: string;
  goal: string;
  status: WorkflowRunStatus;
  createdAt: string;
  updatedAt: string;
  nodes: TaskNode[];
  edges: TaskEdge[];
}

export interface Agent {
  name: string;
  version: string;
  description: string;
  skills: string[];
  supportedTasks: string[];
  priority: number;
  status: "Available" | "Busy" | "Unavailable";
  inFlightTaskCount: number;
  lastHeartbeatAt: string;
}

export interface AgentMetrics {
  agentName: string;
  totalTasks: number;
  completedTasks: number;
  failedTasks: number;
  successRate: number;
  failureRate: number;
  avgAttemptCount: number;
  avgConfidence: number | null;
  avgStageDurationMs: number;
  toolCallCount: number;
  memoryReadCount: number;
  memoryWriteCount: number;
  modelUsage: Record<string, number>;
}

export interface ConfidencePoint {
  at: string;
  confidence: number;
}

export type ArtifactType =
  | "Code"
  | "Markdown"
  | "Json"
  | "Test"
  | "Dockerfile"
  | "Sql"
  | "Image"
  | "Diagram";

export interface Artifact {
  id: string;
  name: string;
  type: ArtifactType;
  ownerAgent: string;
  version: number;
  status: "Draft" | "Final" | "Superseded";
  content: string | null;
  previousVersionId: string | null;
  createdAt: string;
  correlationId: string | null;
  workflowRunId: string | null;
  taskNodeId: string | null;
}

export type MemoryLayer = "Working" | "Conversation" | "Workflow" | "Project" | "LongTerm";
export type MemoryKind = "Requirement" | "Architecture" | "Code" | "Doc" | "Decision" | "LessonLearned";

export interface MemoryItem {
  id: string;
  layer: string;
  scopeRef: string;
  kind: string;
  content: string;
  createdAt: string;
}

export interface MemoryOverviewItem {
  id: string;
  layer: string;
  scopeRef: string;
  kind: string;
  content: string;
  sourceArtifactId: string | null;
  version: number;
  supersededById: string | null;
  ttlAt: string | null;
  createdAt: string;
}

export interface MemoryLayerCount {
  layer: string;
  count: number;
}

export interface MemoryOverview {
  layerCounts: MemoryLayerCount[];
  items: MemoryOverviewItem[];
}

export type ReasoningStage =
  | "Observe"
  | "Understand"
  | "Think"
  | "Plan"
  | "RetrieveContext"
  | "RetrieveMemory"
  | "SelectTools"
  | "Execute"
  | "Reflect"
  | "SelfCritique"
  | "ConfidenceEvaluation"
  | "PublishResult";

export interface ReasoningTrace {
  id: string;
  workflowRunId: string;
  correlationId: string;
  agent: string;
  stage: ReasoningStage;
  startedAt: string;
  durationMs: number;
  inputJson: string | null;
  outputJson: string | null;
  tokens: number | null;
  confidence: number | null;
  modelUsed: string | null;
  retryCount: number;
  memoryReads: number;
  memoryWrites: number;
  toolCalls: number;
  costEstimate: number | null;
  errorMessage: string | null;
  createdAt: string;
}

export type SupervisorDecisionType = "Replan" | "Retry" | "Reassign" | "Debate" | "StrategySelection";

export interface SupervisorDecision {
  id: string;
  workflowRunId: string;
  correlationId: string;
  decisionType: SupervisorDecisionType;
  inputSnapshotJson: string;
  rationale: string;
  confidence: number;
  targetNodeIdsJson: string | null;
  createdAt: string;
}

export interface DecisionTypeCount {
  decisionType: SupervisorDecisionType;
  count: number;
}

export interface SupervisorSummary {
  counts: DecisionTypeCount[];
  recent: SupervisorDecision[];
}

export interface PromptVersionInfo {
  version: number;
  file: string;
  description: string;
  variables: string[];
  createdAt: string;
}

export interface PromptEntry {
  name: string;
  owner: string;
  compatibleAgent: string;
  currentVersion: number;
  versions: PromptVersionInfo[];
}

export interface ReasoningStageMetric {
  stage: string;
  count: number;
  avgDurationMs: number;
  avgConfidence: number | null;
  totalTokens: number;
  totalToolCalls: number;
  totalMemoryReads: number;
  totalMemoryWrites: number;
  totalRetries: number;
}

export interface ReasoningTracePoint {
  at: string;
  stage: string;
  agent: string;
  confidence: number | null;
  durationMs: number;
  tokens: number | null;
  workflowRunId: string;
}

export interface ReasoningTelemetry {
  stageMetrics: ReasoningStageMetric[];
  recentPoints: ReasoningTracePoint[];
}

// Live SignalR event (matches AiAgentsTeam.Api.EventRelay.SignalRRelayHostedService payload).
export interface WorkflowEvent {
  type: string;
  taskId: string | null;
  producedBy: string;
  timestamp: string;
  confidence: number | null;
  riskLevel: string | null;
}
