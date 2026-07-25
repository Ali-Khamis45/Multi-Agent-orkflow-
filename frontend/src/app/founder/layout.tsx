import { FounderAppShell } from "@/components/layout/founder-app-shell";

export default function FounderLayout({ children }: { children: React.ReactNode }) {
  return <FounderAppShell>{children}</FounderAppShell>;
}
