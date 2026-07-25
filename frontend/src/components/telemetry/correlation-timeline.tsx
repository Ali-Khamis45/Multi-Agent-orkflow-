"use client";

import { useMemo } from "react";
import type { ReasoningTracePoint } from "@/lib/types";

const STAGE_COLORS: Record<string, string> = {
  Observe: "bg-slate-400", Understand: "bg-sky-400", Think: "bg-indigo-400", Plan: "bg-violet-400",
  RetrieveContext: "bg-cyan-400", RetrieveMemory: "bg-teal-400", SelectTools: "bg-amber-400",
  Execute: "bg-status-running", Reflect: "bg-fuchsia-400", SelfCritique: "bg-pink-400",
  ConfidenceEvaluation: "bg-status-success", PublishResult: "bg-emerald-500",
};

export function CorrelationTimeline({ points }: { points: ReasoningTracePoint[] }) {
  const { lanes, min, max } = useMemo(() => {
    if (points.length === 0) return { lanes: [], min: 0, max: 1 };
    const times = points.map((p) => new Date(p.at).getTime());
    const min = Math.min(...times);
    const max = Math.max(...times);
    const byAgent = new Map<string, ReasoningTracePoint[]>();
    for (const p of points) {
      if (!byAgent.has(p.agent)) byAgent.set(p.agent, []);
      byAgent.get(p.agent)!.push(p);
    }
    return { lanes: Array.from(byAgent.entries()), min, max };
  }, [points]);

  if (points.length === 0) {
    return <div className="flex h-32 items-center justify-center text-xs text-muted-foreground">No reasoning activity recorded yet.</div>;
  }

  const span = Math.max(1, max - min);

  return (
    <div className="space-y-2 overflow-x-auto">
      {lanes.map(([agent, pts]) => (
        <div key={agent} className="flex items-center gap-2">
          <span className="w-32 shrink-0 truncate text-[11px] text-muted-foreground">{agent}</span>
          <div className="relative h-5 min-w-[600px] flex-1 rounded bg-secondary/30">
            {pts.map((p, i) => {
              const left = ((new Date(p.at).getTime() - min) / span) * 100;
              return (
                <span
                  key={i}
                  title={`${p.stage} · ${new Date(p.at).toLocaleTimeString()} · ${p.durationMs}ms`}
                  className={`absolute top-1/2 h-2.5 w-2.5 -translate-x-1/2 -translate-y-1/2 rounded-full ${STAGE_COLORS[p.stage] ?? "bg-muted-foreground"}`}
                  style={{ left: `${left}%` }}
                />
              );
            })}
          </div>
        </div>
      ))}
      <div className="flex flex-wrap gap-3 pt-1">
        {Object.entries(STAGE_COLORS).map(([stage, color]) => (
          <div key={stage} className="flex items-center gap-1 text-[10px] text-muted-foreground">
            <span className={`h-2 w-2 rounded-full ${color}`} /> {stage}
          </div>
        ))}
      </div>
    </div>
  );
}
