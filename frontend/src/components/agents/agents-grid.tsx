"use client";

import { useMemo, useState } from "react";
import Link from "next/link";
import { Bot, Search } from "lucide-react";
import { motion } from "motion/react";
import { Input } from "@/components/ui/input";
import { Badge } from "@/components/ui/badge";
import { Skeleton } from "@/components/ui/skeleton";
import { StatusBadge } from "@/components/shared/status-badge";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { useAgents, useAgentMetrics, useWorkflowRuns } from "@/hooks/queries";
import { useWorkspaceStore } from "@/store/workspace-store";
import { agentCurrentTask } from "@/lib/agent-activity";
import { agentRole, initials } from "@/lib/utils";

const STATUS_OPTIONS = ["All", "Available", "Busy", "Unavailable"];

export function AgentsGrid() {
  const workspaceId = useWorkspaceStore((s) => s.currentWorkspaceId);
  const { data: agents, isLoading } = useAgents();
  const { data: metrics } = useAgentMetrics();
  const { data: runs } = useWorkflowRuns({ workspaceId: workspaceId ?? undefined, limit: 100 });

  const [search, setSearch] = useState("");
  const [status, setStatus] = useState("All");
  const [role, setRole] = useState("All");
  const [skill, setSkill] = useState("All");

  const metricsByAgent = useMemo(() => new Map(metrics?.map((m) => [m.agentName, m])), [metrics]);

  const roles = useMemo(
    () => ["All", ...Array.from(new Set(agents?.map((a) => agentRole(a.name)) ?? []))],
    [agents],
  );
  const skills = useMemo(
    () => ["All", ...Array.from(new Set(agents?.flatMap((a) => a.skills) ?? []))],
    [agents],
  );

  const filtered = useMemo(() => {
    return (agents ?? []).filter((a) => {
      if (status !== "All" && a.status !== status) return false;
      if (role !== "All" && agentRole(a.name) !== role) return false;
      if (skill !== "All" && !a.skills.includes(skill)) return false;
      if (search && !a.name.toLowerCase().includes(search.toLowerCase()) && !a.description.toLowerCase().includes(search.toLowerCase()))
        return false;
      return true;
    });
  }, [agents, status, role, skill, search]);

  return (
    <div className="space-y-4">
      <div className="flex flex-wrap items-center gap-2">
        <div className="relative flex-1 min-w-48">
          <Search className="pointer-events-none absolute left-2.5 top-1/2 h-3.5 w-3.5 -translate-y-1/2 text-muted-foreground" />
          <Input
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            placeholder="Search agents…"
            className="h-8 bg-secondary/30 pl-8 text-xs"
          />
        </div>
        <Select value={status} onValueChange={(v) => setStatus(v ?? "All")}>
          <SelectTrigger className="h-8 w-36 text-xs"><SelectValue placeholder="Status" /></SelectTrigger>
          <SelectContent>
            {STATUS_OPTIONS.map((s) => <SelectItem key={s} value={s}>{s}</SelectItem>)}
          </SelectContent>
        </Select>
        <Select value={role} onValueChange={(v) => setRole(v ?? "All")}>
          <SelectTrigger className="h-8 w-40 text-xs"><SelectValue placeholder="Role" /></SelectTrigger>
          <SelectContent>
            {roles.map((r) => <SelectItem key={r} value={r}>{r}</SelectItem>)}
          </SelectContent>
        </Select>
        <Select value={skill} onValueChange={(v) => setSkill(v ?? "All")}>
          <SelectTrigger className="h-8 w-40 text-xs"><SelectValue placeholder="Skill" /></SelectTrigger>
          <SelectContent>
            {skills.map((s) => <SelectItem key={s} value={s}>{s}</SelectItem>)}
          </SelectContent>
        </Select>
        <span className="ml-auto text-[11px] text-muted-foreground">
          {filtered.length} of {agents?.length ?? 0} agents
        </span>
      </div>

      {isLoading && (
        <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-3">
          {Array.from({ length: 6 }).map((_, i) => <Skeleton key={i} className="h-40 w-full" />)}
        </div>
      )}

      {!isLoading && filtered.length === 0 && (
        <p className="py-16 text-center text-sm text-muted-foreground">No agents match these filters.</p>
      )}

      <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-3">
        {filtered.map((agent, i) => {
          const m = metricsByAgent.get(agent.name);
          const current = agentCurrentTask(runs, agent.name);
          return (
            <motion.div
              key={agent.name}
              initial={{ opacity: 0, y: 6 }}
              animate={{ opacity: 1, y: 0 }}
              transition={{ duration: 0.15, delay: i * 0.02 }}
            >
              <Link
                href={`/agents/${encodeURIComponent(agent.name)}`}
                className="flex h-full flex-col rounded-xl border border-border/60 bg-card/60 p-4 transition-colors hover:bg-secondary/30"
              >
                <div className="flex items-start justify-between gap-2">
                  <div className="flex items-center gap-2.5">
                    <div className="flex h-9 w-9 shrink-0 items-center justify-center rounded-full bg-status-running/15 text-xs font-semibold text-status-running">
                      {initials(agent.name)}
                    </div>
                    <div className="min-w-0">
                      <div className="truncate text-sm font-semibold">{agent.name}</div>
                      <div className="truncate text-[11px] text-muted-foreground">{agentRole(agent.name)}</div>
                    </div>
                  </div>
                  <StatusBadge status={agent.status} className="shrink-0" />
                </div>

                <p className="mt-2.5 line-clamp-2 text-[11px] text-muted-foreground">{agent.description}</p>

                <div className="mt-2 flex flex-wrap gap-1">
                  {agent.skills.slice(0, 3).map((s) => (
                    <Badge key={s} variant="outline" className="text-[10px]">{s}</Badge>
                  ))}
                  {agent.skills.length > 3 && (
                    <Badge variant="outline" className="text-[10px]">+{agent.skills.length - 3}</Badge>
                  )}
                </div>

                <div className="mt-auto grid grid-cols-3 gap-1.5 pt-3 text-center">
                  <MiniStat label="Success" value={m ? `${(m.successRate * 100).toFixed(0)}%` : "—"} />
                  <MiniStat label="Confidence" value={m?.avgConfidence != null ? `${(m.avgConfidence * 100).toFixed(0)}%` : "—"} />
                  <MiniStat label="In-flight" value={agent.inFlightTaskCount} />
                </div>

                {current && (
                  <div className="mt-2 flex items-center gap-1.5 truncate rounded-md bg-status-running/10 px-2 py-1 text-[10px] text-status-running">
                    <Bot className="h-3 w-3 shrink-0 animate-pulse" />
                    <span className="truncate">{current.node.name} · {current.run.goal}</span>
                  </div>
                )}
              </Link>
            </motion.div>
          );
        })}
      </div>
    </div>
  );
}

function MiniStat({ label, value }: { label: string; value: React.ReactNode }) {
  return (
    <div className="rounded-md bg-secondary/40 py-1.5">
      <div className="text-xs font-semibold tabular-nums">{value}</div>
      <div className="text-[9px] uppercase tracking-wide text-muted-foreground">{label}</div>
    </div>
  );
}
