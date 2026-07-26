"use client";

import { useEffect } from "react";
import { usePathname, useRouter } from "next/navigation";
import { useCompanyProfile } from "@/hooks/queries";
import { useEnsureWorkspaceSelected } from "@/hooks/use-ensure-workspace";
import { FounderAppShell } from "@/components/layout/founder-app-shell";

const ONBOARDING_PATH = "/founder/onboarding";

/**
 * Phase 3 "AI Company Operating System" onboarding gate: "The first time a Founder
 * logs in, DO NOT show an empty dashboard. Instead launch an AI onboarding flow."
 * Wraps every /founder/* route (see app/founder/layout.tsx) so no page has to check
 * IsOnboarded itself — same pattern as AuthGate one level up.
 */
export function OnboardingGate({ children }: { children: React.ReactNode }) {
  const pathname = usePathname();
  const router = useRouter();
  const workspaceId = useEnsureWorkspaceSelected();
  const { data: profile, isLoading } = useCompanyProfile(workspaceId ?? undefined);
  const isOnboardingPath = pathname === ONBOARDING_PATH;

  useEffect(() => {
    if (!profile) return;
    if (!profile.isOnboarded && !isOnboardingPath) {
      router.replace(ONBOARDING_PATH);
      return;
    }
    if (profile.isOnboarded && isOnboardingPath) {
      router.replace("/founder");
      return;
    }
  }, [profile, isOnboardingPath, router]);

  if (!workspaceId || isLoading || !profile) return null;
  if (!profile.isOnboarded && !isOnboardingPath) return null; // redirecting to onboarding
  if (profile.isOnboarded && isOnboardingPath) return null; // redirecting to dashboard

  if (isOnboardingPath) return <>{children}</>; // full-screen wizard, no sidebar/shell
  return <FounderAppShell>{children}</FounderAppShell>;
}
