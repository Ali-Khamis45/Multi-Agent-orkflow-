import type { TaskEdge, TaskNode } from "./types";

export interface LayoutedNode {
  node: TaskNode;
  x: number;
  y: number;
  column: number;
}

const COLUMN_WIDTH = 260;
const ROW_HEIGHT = 110;

/**
 * Simple layered (Sugiyama-style) left-to-right layout: each node's column is
 * its longest-path distance from a root (a node with no predecessor), so
 * parallel branches (e.g. Backend + Frontend) land in the same column —
 * visually proving they're dispatched together (§5.2 step 3).
 */
export function layoutDag(nodes: TaskNode[], edges: TaskEdge[]): LayoutedNode[] {
  const predecessors = new Map<string, string[]>();
  const successors = new Map<string, string[]>();
  for (const n of nodes) {
    predecessors.set(n.id, []);
    successors.set(n.id, []);
  }
  for (const e of edges) {
    predecessors.get(e.successorNodeId)?.push(e.predecessorNodeId);
    successors.get(e.predecessorNodeId)?.push(e.successorNodeId);
  }

  const column = new Map<string, number>();

  // Longest-path column assignment via repeated relaxation (DAG, so this converges).
  for (const n of nodes) column.set(n.id, 0);
  for (let iter = 0; iter < nodes.length; iter++) {
    let changed = false;
    for (const e of edges) {
      const predCol = column.get(e.predecessorNodeId) ?? 0;
      const succCol = column.get(e.successorNodeId) ?? 0;
      if (succCol < predCol + 1) {
        column.set(e.successorNodeId, predCol + 1);
        changed = true;
      }
    }
    if (!changed) break;
  }

  // Group by column, assign row index within each column.
  const byColumn = new Map<number, TaskNode[]>();
  for (const n of nodes) {
    const col = column.get(n.id) ?? 0;
    if (!byColumn.has(col)) byColumn.set(col, []);
    byColumn.get(col)!.push(n);
  }

  const result: LayoutedNode[] = [];
  for (const [col, colNodes] of byColumn) {
    colNodes.sort((a, b) => a.name.localeCompare(b.name));
    const totalHeight = colNodes.length * ROW_HEIGHT;
    colNodes.forEach((n, i) => {
      result.push({
        node: n,
        x: col * COLUMN_WIDTH,
        y: i * ROW_HEIGHT - totalHeight / 2,
        column: col,
      });
    });
  }

  return result;
}
