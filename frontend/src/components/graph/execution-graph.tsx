"use client";

import { useEffect, useMemo } from "react";
import {
  ReactFlow,
  Background,
  Controls,
  MiniMap,
  useNodesState,
  useEdgesState,
  BackgroundVariant,
  MarkerType,
  type Node,
  type Edge,
} from "@xyflow/react";
import "@xyflow/react/dist/style.css";
import { layoutDag } from "@/lib/dag-layout";
import { useInspectorStore } from "@/store/inspector-store";
import { DagTaskNode, type DagTaskNodeData } from "@/components/graph/dag-task-node";
import type { WorkflowRun } from "@/lib/types";

const nodeTypes = { taskNode: DagTaskNode };

const LIVE_STATUSES = new Set(["Running", "Dispatched"]);

function buildGraph(run: WorkflowRun): { nodes: Node<DagTaskNodeData>[]; edges: Edge[] } {
  const layouted = layoutDag(run.nodes, run.edges);

  const nodes: Node<DagTaskNodeData>[] = layouted.map(({ node, x, y }) => ({
    id: node.id,
    type: "taskNode",
    position: { x, y },
    data: { task: node },
  }));

  const edges: Edge[] = run.edges.map((e) => {
    const successor = run.nodes.find((n) => n.id === e.successorNodeId);
    const animated = successor ? LIVE_STATUSES.has(successor.status) : false;
    return {
      id: `${e.predecessorNodeId}-${e.successorNodeId}`,
      source: e.predecessorNodeId,
      target: e.successorNodeId,
      animated,
      style: { stroke: animated ? "var(--color-status-running)" : "var(--color-border)", strokeWidth: 1.5 },
      markerEnd: { type: MarkerType.ArrowClosed, width: 14, height: 14 },
    };
  });

  return { nodes, edges };
}

export function ExecutionGraph({ run }: { run: WorkflowRun }) {
  const openInspector = useInspectorStore((s) => s.open);
  const initial = useMemo(() => buildGraph(run), [run]);

  const [nodes, setNodes, onNodesChange] = useNodesState(initial.nodes);
  const [edges, setEdges, onEdgesChange] = useEdgesState(initial.edges);

  useEffect(() => {
    const { nodes: nextNodes, edges: nextEdges } = buildGraph(run);
    setNodes(nextNodes);
    setEdges(nextEdges);
    // run identity changes each SignalR-driven refetch, so this re-syncs positions/status live.
  }, [run, setNodes, setEdges]);

  return (
    <div className="h-full w-full">
      <ReactFlow
        nodes={nodes}
        edges={edges}
        onNodesChange={onNodesChange}
        onEdgesChange={onEdgesChange}
        nodeTypes={nodeTypes}
        onNodeClick={(_, node) =>
          openInspector({ kind: "task-node", workflowRunId: run.id, taskNodeId: node.id })
        }
        fitView
        fitViewOptions={{ padding: 0.3 }}
        proOptions={{ hideAttribution: true }}
        minZoom={0.3}
        maxZoom={1.5}
      >
        <Background variant={BackgroundVariant.Dots} gap={20} size={1} className="opacity-40" />
        <Controls showInteractive={false} className="!bg-card !border-border [&>button]:!border-border [&>button]:!bg-card [&>button]:!fill-foreground" />
        <MiniMap
          pannable
          zoomable
          className="!bg-card !border !border-border"
          nodeColor={() => "var(--color-border)"}
          maskColor="rgba(0,0,0,0.6)"
        />
      </ReactFlow>
    </div>
  );
}
