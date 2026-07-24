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
import { useAgents, useWorkflowRuns } from "@/hooks/queries";
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
} from "lucide-react";

const NAV = [
  { href: "/", label: "Dashboard", icon: LayoutDashboard },
  { href: "/workflows", label: "Workflow Runs", icon: GitBranch },
  { href: "/agents", label: "Agents", icon: Bot },
  { href: "/artifacts", label: "Artifacts", icon: FileStack },
  { href: "/memory", label: "Memory", icon: BrainCircuit },
  { href: "/telemetry", label: "Telemetry", icon: Activity },
  { href: "/prompts", label: "Prompt Registry", icon: ScrollText },
  { href: "/settings", label: "Settings", icon: Settings },
];

export function CommandPalette() {
  const { isOpen, close, toggle } = useCommandPaletteStore();
  const router = useRouter();
  const { data: agents } = useAgents();
  const { data: runs } = useWorkflowRuns({ limit: 8 });

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

  return (
    <CommandDialog open={isOpen} onOpenChange={(open) => (open ? undefined : close())}>
      <CommandInput placeholder="Navigate, find an agent, open a run…" />
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
                <CommandItem key={agent.name} onSelect={() => go("/agents")}>
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
          </>
        )}
      </CommandList>
    </CommandDialog>
  );
}
