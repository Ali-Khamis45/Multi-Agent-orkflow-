"use client";

import { useEffect } from "react";
import { useRouter } from "next/navigation";
import {
  CommandDialog,
  CommandEmpty,
  CommandGroup,
  CommandInput,
  CommandItem,
  CommandList,
  CommandSeparator,
} from "@/components/ui/command";
import { useCommandPaletteStore } from "@/store/command-palette-store";
import { useWorkspaceStore } from "@/store/workspace-store";
import { useAgents, useWorkflowRuns, useArtifacts, usePrompts, useSubmitIntake } from "@/hooks/queries";
import { toast } from "sonner";
import {
  LayoutDashboard,
  GitBranch,
  Bot,
  FileStack,
  BrainCircuit,
  Activity,
  ScrollText,
  Settings,
  Plus,
  Radar,
  HeartPulse,
  RotateCw,
} from "lucide-react";

const NAV = [
  { href: "/", label: "Dashboard", icon: LayoutDashboard },
  { href: "/workflows", label: "Workflow Runs", icon: GitBranch },
  { href: "/agents", label: "Agents", icon: Bot },
  { href: "/artifacts", label: "Artifacts", icon: FileStack },
  { href: "/memory", label: "Memory", icon: BrainCircuit },
  { href: "/telemetry", label: "Telemetry", icon: Activity },
  { href: "/supervisor", label: "Supervisor", icon: Radar },
  { href: "/prompts", label: "Prompt Registry", icon: ScrollText },
  { href: "/health", label: "Project Health", icon: HeartPulse },
  { href: "/settings", label: "Settings", icon: Settings },
];

export function CommandPalette() {
  const { isOpen, close, toggle } = useCommandPaletteStore();
  const router = useRouter();
  const workspaceId = useWorkspaceStore((s) => s.currentWorkspaceId);
  const { data: agents } = useAgents();
  const { data: runs } = useWorkflowRuns({ workspaceId: workspaceId ?? undefined, limit: 20 });
  const { data: artifacts } = useArtifacts({ workspaceId: workspaceId ?? "" });
  const { data: prompts } = usePrompts();
  const submit = useSubmitIntake();

  useEffect(() => {
    function handler(e: KeyboardEvent) {
      if ((e.metaKey || e.ctrlKey) && e.key.toLowerCase() === "k") {
        e.preventDefault();
        toggle();
      }
    }
    window.addEventListener("keydown", handler);
    return () => window.removeEventListener("keydown", handler);
  }, [toggle]);

  const go = (href: string) => {
    router.push(href);
    close();
  };

  const replay = (goal: string) => {
    submit.mutate(
      { rawInput: goal, workspaceId: workspaceId ?? undefined },
      {
        onSuccess: (result) => {
          toast.success("Replaying workflow", { description: goal });
          router.push(`/workflows/${result.workflowRunId}`);
        },
        onError: (err) => toast.error("Failed to replay", { description: String(err) }),
      },
    );
    close();
  };

  return (
    <CommandDialog open={isOpen} onOpenChange={(open) => (open ? undefined : close())}>
      <CommandInput placeholder="Navigate, find an agent, open a run, search artifacts…" />
      <CommandList>
        <CommandEmpty>No results found.</CommandEmpty>

        <CommandGroup heading="Actions">
          <CommandItem onSelect={() => go("/workflows")}>
            <Plus className="h-4 w-4" />
            Start a new workflow
          </CommandItem>
        </CommandGroup>

        <CommandSeparator />

        <CommandGroup heading="Navigate">
          {NAV.map((item) => (
            <CommandItem key={item.href} onSelect={() => go(item.href)}>
              <item.icon className="h-4 w-4" />
              {item.label}
            </CommandItem>
          ))}
        </CommandGroup>

        {agents && agents.length > 0 && (
          <>
            <CommandSeparator />
            <CommandGroup heading="Agents">
              {agents.map((agent) => (
                <CommandItem key={agent.name} onSelect={() => go(`/agents/${encodeURIComponent(agent.name)}`)}>
                  <Bot className="h-4 w-4" />
                  {agent.name}
                </CommandItem>
              ))}
            </CommandGroup>
          </>
        )}

        {runs && runs.length > 0 && (
          <>
            <CommandSeparator />
            <CommandGroup heading="Recent runs">
              {runs.map((run) => (
                <CommandItem key={run.id} onSelect={() => go(`/workflows/${run.id}`)}>
                  <GitBranch className="h-4 w-4" />
                  {run.goal.slice(0, 60)}
                </CommandItem>
              ))}
            </CommandGroup>
            <CommandGroup heading="Replay">
              {runs.slice(0, 5).map((run) => (
                <CommandItem key={`replay-${run.id}`} onSelect={() => replay(run.goal)}>
                  <RotateCw className="h-4 w-4" />
                  Replay &quot;{run.goal.slice(0, 40)}&quot;
                </CommandItem>
              ))}
            </CommandGroup>
          </>
        )}

        {artifacts && artifacts.length > 0 && (
          <>
            <CommandSeparator />
            <CommandGroup heading="Artifacts">
              {artifacts.slice(0, 30).map((a) => (
                <CommandItem key={a.id} onSelect={() => go("/artifacts")}>
                  <FileStack className="h-4 w-4" />
                  {a.name}
                  <span className="ml-auto text-[10px] text-muted-foreground">{a.type}</span>
                </CommandItem>
              ))}
            </CommandGroup>
          </>
        )}

        {prompts && prompts.length > 0 && (
          <>
            <CommandSeparator />
            <CommandGroup heading="Prompts">
              {prompts.map((p) => (
                <CommandItem key={p.name} onSelect={() => go("/prompts")}>
                  <ScrollText className="h-4 w-4" />
                  {p.name}
                  <span className="ml-auto text-[10px] text-muted-foreground">v{p.currentVersion}</span>
                </CommandItem>
              ))}
            </CommandGroup>
          </>
        )}
      </CommandList>
    </CommandDialog>
  );
}
