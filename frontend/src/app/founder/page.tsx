"use client";

import Link from "next/link";
import { Activity, CheckCircle2, Bot, FileStack, Rocket, Sparkles, History } from "lucide-react";
import { StatCard } from "@/components/dashboard/stat-card";
import { FounderIntakeForm } from "@/components/founder/founder-intake-form";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { StatusBadge } from "@/components/shared/status-badge";
import { Progress } from "@/components/ui/progress";
import {
  useWorkflowRuns, useAgents, useArtifacts, useCompanyProfile, useBusinessHealth,
  useRecommendations, useBusinessTimeline,
} from "@/hooks/queries";
import { useWorkspaceStore } from "@/store/workspace-store";
import { useAuthStore } from "@/store/auth-store";
import type { CompanyProfileData } from "@/lib/api-client";

function isToday(iso: string): boolean {
  const d = new Date(iso);
  const now = new Date();
  return d.toDateString() === now.toDateString();
}

function formatMoney(n: number | null): string {
  if (n === null) return "Not set";
  return `$${n.toLocaleString()}`;
}

export default function FounderDashboardPage() {
  const user = useAuthStore((s) => s.user);
  const workspaceId = useWorkspaceStore((s) => s.currentWorkspaceId);
  const { data: runs } = useWorkflowRuns({ workspaceId: workspaceId ?? undefined, limit: 100 });
  const { data: agents } = useAgents();
  const { data: artifacts } = useArtifacts({ workspaceId: workspaceId ?? "" });
  const { data: profile } = useCompanyProfile(workspaceId ?? undefined);
  const { data: health } = useBusinessHealth(workspaceId ?? undefined);
  const { data: recommendations } = useRecommendations(workspaceId ?? undefined);
  const { data: timeline } = useBusinessTimeline(workspaceId ?? undefined);

  const activeCount = runs?.filter((r) => ["Running", "WaitingApproval", "Planning"].includes(r.status)).length ?? 0;
  const completedToday = (runs ?? []).filter((r) => isToday(r.createdAt) && r.status === "Completed").length;
  const availableAgents = agents?.filter((a) => a.status === "Available").length ?? 0;
  const recent = [...(runs ?? [])].sort((a, b) => b.updatedAt.localeCompare(a.updatedAt)).slice(0, 6);

  const data: CompanyProfileData | null = profile ? JSON.parse(profile.profileJson) : null;

  return (
    <div className="mx-auto max-w-6xl space-y-6 p-6">
      <div>
        <h1 className="text-lg font-semibold tracking-tight">Welcome{user ? `, ${user.name}` : ""}</h1>
        <p className="text-xs text-muted-foreground">
          {data?.basicInfo.companyName ? `${data.basicInfo.companyName} — ` : ""}your business operating system, live.
        </p>
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
              ["Current Stage", data?.basicInfo.launchStage ?? "Not set"],
              ["Funding Status", data?.business.fundingStatus ?? "Not set"],
              ["Budget", formatMoney(data?.business.budget ?? null)],
              ["Monthly Revenue Goal", formatMoney(data?.business.monthlyRevenueGoal ?? null)],
              ["Growth Goal", data?.business.growthGoal ?? "Not set"],
              ["Launch Timeline", data?.business.launchDate ?? "Not set"],
            ].map(([label, value]) => (
              <div key={label} className="flex items-center justify-between border-b border-border/40 py-1.5 last:border-0">
                <span className="text-muted-foreground">{label}</span>
                <span className={value === "Not set" ? "text-muted-foreground/70" : "font-medium"}>{value}</span>
              </div>
            ))}
          </CardContent>
        </Card>
      </div>

      <div className="grid gap-4 lg:grid-cols-3">
        <Card className="border-border/60 lg:col-span-1">
          <CardHeader>
            <CardTitle className="text-sm">Business Health</CardTitle>
          </CardHeader>
          <CardContent className="space-y-3">
            <div className="flex items-center justify-between">
              <span className="text-2xl font-semibold tabular-nums">{health?.overallScore ?? 0}%</span>
            </div>
            {health?.categories.map((c) => (
              <div key={c.category} className="space-y-1" title={c.explanation}>
                <div className="flex items-center justify-between text-[11px]">
                  <span className="text-muted-foreground">{c.category}</span>
                  <span className="tabular-nums text-muted-foreground">{c.score}%</span>
                </div>
                <Progress value={c.score} className="h-1" />
              </div>
            ))}
          </CardContent>
        </Card>

        <Card className="border-border/60 lg:col-span-1">
          <CardHeader>
            <CardTitle className="flex items-center gap-2 text-sm">
              <Sparkles className="h-3.5 w-3.5 text-amber-500" />
              Recommendations
            </CardTitle>
          </CardHeader>
          <CardContent className="space-y-2">
            {(recommendations ?? []).map((r, i) => (
              <div key={i} className="rounded-md border border-border/40 px-2.5 py-2 text-xs">
                {r.text}
              </div>
            ))}
            {(!recommendations || recommendations.length === 0) && (
              <p className="py-4 text-center text-xs text-muted-foreground">No recommendations yet.</p>
            )}
          </CardContent>
        </Card>

        <Card className="border-border/60 lg:col-span-1">
          <CardHeader>
            <CardTitle className="flex items-center gap-2 text-sm">
              <History className="h-3.5 w-3.5 text-amber-500" />
              Business Timeline
            </CardTitle>
          </CardHeader>
          <CardContent className="space-y-2">
            {(timeline ?? []).slice().reverse().map((m, i) => (
              <div key={i} className="border-b border-border/40 pb-2 text-xs last:border-0">
                <div className="font-medium">{m.title}</div>
                <div className="text-[11px] text-muted-foreground">
                  {new Date(m.at).toLocaleDateString()} · {m.ownerAgent}
                </div>
              </div>
            ))}
            {(!timeline || timeline.length === 0) && (
              <p className="py-4 text-center text-xs text-muted-foreground">No milestones yet.</p>
            )}
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
