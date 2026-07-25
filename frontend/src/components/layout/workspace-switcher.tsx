"use client";

import { useEffect } from "react";
import { Check, ChevronsUpDown, Building2 } from "lucide-react";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuGroup,
  DropdownMenuItem,
  DropdownMenuTrigger,
  DropdownMenuLabel,
  DropdownMenuSeparator,
} from "@/components/ui/dropdown-menu";
import { Button } from "@/components/ui/button";
import { useWorkspaces } from "@/hooks/queries";
import { useWorkspaceStore } from "@/store/workspace-store";
import { cn } from "@/lib/utils";

export function WorkspaceSwitcher() {
  const { data: workspaces, isLoading } = useWorkspaces();
  const currentWorkspaceId = useWorkspaceStore((s) => s.currentWorkspaceId);
  const setWorkspace = useWorkspaceStore((s) => s.setWorkspace);

  // Default to the most recently created workspace once the list loads.
  useEffect(() => {
    if (!currentWorkspaceId && workspaces && workspaces.length > 0) {
      setWorkspace(workspaces[0].id);
    }
  }, [workspaces, currentWorkspaceId, setWorkspace]);

  const current = workspaces?.find((w) => w.id === currentWorkspaceId) ?? workspaces?.[0];

  return (
    <DropdownMenu>
      <DropdownMenuTrigger
        render={
          <Button
            variant="outline"
            size="sm"
            className="h-8 max-w-52 justify-between gap-2 border-border/60 bg-secondary/40 px-2.5 text-xs font-normal"
          />
        }
      >
        <span className="flex items-center gap-1.5 truncate">
          <Building2 className="h-3.5 w-3.5 shrink-0 text-muted-foreground" />
          <span className="truncate">
            {isLoading ? "Loading…" : (current?.name ?? "No workspace")}
          </span>
        </span>
        <ChevronsUpDown className="h-3.5 w-3.5 shrink-0 text-muted-foreground" />
      </DropdownMenuTrigger>
      <DropdownMenuContent align="start" className="w-56">
        <DropdownMenuGroup>
          <DropdownMenuLabel className="text-xs text-muted-foreground">Workspaces</DropdownMenuLabel>
          <DropdownMenuSeparator />
          {workspaces?.map((ws) => (
            <DropdownMenuItem key={ws.id} onClick={() => setWorkspace(ws.id)} className="text-sm">
              <Check className={cn("mr-2 h-3.5 w-3.5", ws.id === current?.id ? "opacity-100" : "opacity-0")} />
              {ws.name}
            </DropdownMenuItem>
          ))}
          {(!workspaces || workspaces.length === 0) && !isLoading && (
            <div className="px-2 py-1.5 text-xs text-muted-foreground">No workspaces yet</div>
          )}
        </DropdownMenuGroup>
      </DropdownMenuContent>
    </DropdownMenu>
  );
}
