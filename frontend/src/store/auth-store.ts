import { create } from "zustand";
import { persist } from "zustand/middleware";
import { useWorkspaceStore } from "./workspace-store";

export type CompanyType = "SoftwareCompany" | "Founder";

export interface AuthUser {
  userId: string;
  email: string;
  name: string;
  companyType: CompanyType;
}

interface AuthState {
  token: string | null;
  user: AuthUser | null;
  setSession: (token: string, user: AuthUser) => void;
  clearSession: () => void;
}

export const useAuthStore = create<AuthState>()(
  persist(
    (set) => ({
      token: null,
      user: null,
      setSession: (token, user) => set({ token, user }),
      clearSession: () => {
        // A stale workspaceId from a previous account must not leak into the
        // next login — GetWorkspacesQuery is scoped per-user, so this would
        // otherwise resolve to "no such workspace" or (worse) another user's.
        useWorkspaceStore.setState({ currentWorkspaceId: null });
        set({ token: null, user: null });
      },
    }),
    { name: "mission-control-auth" },
  ),
);

// Non-hook accessor for the API client, which is not itself a React component.
export function getAuthToken(): string | null {
  return useAuthStore.getState().token;
}

export function homeRouteFor(companyType: CompanyType): string {
  return companyType === "Founder" ? "/founder" : "/";
}
