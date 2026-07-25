"use client";

import { useEffect } from "react";
import { usePathname, useRouter } from "next/navigation";
import { useAuthStore, homeRouteFor } from "@/store/auth-store";
import { AppShell } from "@/components/layout/app-shell";

const PUBLIC_PATHS = ["/login", "/register"];

/**
 * Phase 2 ("AI Enterprise OS") route guard. A user belongs to exactly one
 * CompanyType, fixed at registration — this is the one place that enforces
 * "Software users cannot access Founder pages, Founder users cannot access
 * Software pages," so every route (existing and future) gets it for free
 * without its own auth logic.
 *
 * Client-side rather than proxy.ts: the session lives in localStorage (via
 * zustand/persist), matching this app's existing workspace-store pattern —
 * a server-side proxy can't read it without also switching to cookies.
 */
export function AuthGate({ children }: { children: React.ReactNode }) {
  const pathname = usePathname();
  const router = useRouter();
  const token = useAuthStore((s) => s.token);
  const user = useAuthStore((s) => s.user);
  const hasSession = !!(token && user);
  const isPublicPath = PUBLIC_PATHS.includes(pathname);
  const isFounderPath = pathname.startsWith("/founder");

  useEffect(() => {
    if (!hasSession) {
      if (!isPublicPath) router.replace("/login");
      return;
    }
    if (isPublicPath) {
      router.replace(homeRouteFor(user.companyType));
      return;
    }
    if (user.companyType === "Founder" && !isFounderPath) {
      router.replace("/founder");
      return;
    }
    if (user.companyType === "SoftwareCompany" && isFounderPath) {
      router.replace("/");
      return;
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [hasSession, isPublicPath, isFounderPath, user?.companyType, pathname]);

  if (isPublicPath) {
    if (hasSession) return null; // redirecting to the user's home
    return <>{children}</>;
  }

  if (!hasSession) return null; // redirecting to /login
  if (user.companyType === "Founder" && !isFounderPath) return null; // redirecting to /founder
  if (user.companyType === "SoftwareCompany" && isFounderPath) return null; // redirecting to /

  // Founder routes bring their own shell (app/founder/layout.tsx); the
  // Software Company workspace keeps its existing, unchanged AppShell here.
  if (isFounderPath) return <>{children}</>;
  return <AppShell>{children}</AppShell>;
}
