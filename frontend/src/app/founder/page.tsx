"use client";

import Link from "next/link";
import { Activity, CheckCircle2, Bot, FileStack, Rocket } from "lucide-react";
import { StatCard } from "@/components/dashboard/stat-card";
import { FounderIntakeForm } from "@/components/founder/founder-intake-form";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { StatusBadge } from "@/components/shared/status-badge";
import { useWorkflowRuns, useAgents, useArtifacts } from "@/hooks/queries";
import { useWorkspaceStore } from "@/store/workspace-store";
import { useAuthStore } from "@/store/auth-store";

function isToday(iso: string): boolean {
  const d = new Date(iso);
  const now = new Date();
  return d.toDateString() === now.toDateString();
}

export default function FounderDashboardPage() {
  const user = useAuthStore((s) => s.user);
  const workspaceId = useWorkspaceStore((s) => s.currentWorkspaceId);
  const { data: runs } = useWorkflowRuns({ workspaceId: workspaceId ?? undefined, limit: 100 });
  const { data: agents } = useAgents();
  const { data: artifacts } = useArtifacts({ workspaceId: workspaceId ?? "" });

  const activeCount = runs?.filter((r) => ["Running", "WaitingApproval", "Planning"].includes(r.status)).length ?? 0;
  const completedToday = (runs ?? []).filter((r) => isToday(r.createdAt) && r.status === "Completed").length;
  const availableAgents = agents?.filter((a) => a.status === "Available").length ?? 0;
  const recent = [...(runs ?? [])].sort((a, b) => b.updatedAt.localeCompare(a.updatedAt)).slice(0, 6);

  return (
    <div className="mx-auto max-w-6xl space-y-6 p-6">
      <div>
        <h1 className="text-lg font-semibold tracking-tight">Welcome{user ? `, ${user.name}` : ""}</h1>
        <p className="text-xs text-muted-foreground">Your business operating system — live status</p>
      </div>

      <FounderIntakeForm />

      <div className="grid grid-cols-2 gap-3 md:grid-cols-4">
        <StatCard label="Active AI tasks" value={activeCount} icon={Activity} tone="running" />
        <StatCard label="Completed today" value={completedToday} icon={CheckCircle2} tone="success" />
        <StatCard label="Artifacts produced" value={artifacts?.length ?? 0} icon={FileStack} />
        <StatCard
          label="Agents available"
          value={`${availableAgents}/${agents?.length ?? 0}`}
          icon={Bot}
          tone={availableAgents > 0 ? "success" : "warning"}
        />
      </div>

      <div className="grid gap-4 lg:grid-cols-2">
        <Card className="border-border/60">
          <CardHeader>
            <CardTitle className="text-sm">Recent AI Tasks</CardTitle>
          </CardHeader>
          <CardContent className="space-y-1.5">
            {recent.length === 0 && (
              <p className="py-6 text-center text-xs text-muted-foreground">
                No tasks yet — describe your business idea above to get started.
              </p>
            )}
            {recent.map((run) => (
              <Link
                key={run.id}
                href={`/founder/workflows/${run.id}`}
                className="flex items-center justify-between rounded-md px-2 py-1.5 text-xs transition-colors hover:bg-secondary/40"
              >
                <span className="truncate">{run.goal}</span>
                <StatusBadge status={run.status} />
              </Link>
            ))}
          </CardContent>
        </Card>

        <Card className="border-border/60">
          <CardHeader>
            <CardTitle className="flex items-center gap-2 text-sm">
              <Rocket className="h-3.5 w-3.5 text-amber-500" />
              Business Profile
            </CardTitle>
          </CardHeader>
          <CardContent className="space-y-2 text-xs">
            {[
              ["Current Stage", "Not set"],
              ["Funding Status", "Not set"],
              ["Revenue Goals", "Not set"],
              ["Launch Timeline", "Not set"],
            ].map(([label, value]) => (
              <div key={label} className="flex items-center justify-between border-b border-border/40 py-1.5 last:border-0">
                <span className="text-muted-foreground">{label}</span>
                <span className="text-muted-foreground/70">{value}</span>
              </div>
            ))}
            <p className="pt-1 text-[11px] text-muted-foreground">
              Business profile tracking is coming soon — for now, run an idea above to generate your Executive
              Summary, Business Model Canvas, and Launch Strategy.
            </p>
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
