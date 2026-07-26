"use client";

import { useEffect } from "react";
import { useWorkspaces } from "./queries";
import { useWorkspaceStore } from "@/store/workspace-store";

/** Auto-selects the caller's first workspace once it loads. Needed anywhere that must
 * know the current workspaceId before WorkspaceSwitcher (which does this same
 * selection as a side effect of rendering) has actually mounted — e.g. the Founder
 * onboarding gate, which decides whether to render the shell WorkspaceSwitcher lives
 * in at all. */
export function useEnsureWorkspaceSelected(): string | null {
  const { data: workspaces } = useWorkspaces();
  const currentWorkspaceId = useWorkspaceStore((s) => s.currentWorkspaceId);
  const setWorkspace = useWorkspaceStore((s) => s.setWorkspace);

  useEffect(() => {
    if (!currentWorkspaceId && workspaces && workspaces.length > 0) {
      setWorkspace(workspaces[0].id);
    }
  }, [workspaces, currentWorkspaceId, setWorkspace]);

  return currentWorkspaceId;
}
