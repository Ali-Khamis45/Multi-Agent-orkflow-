import { OnboardingGate } from "@/components/founder/onboarding-gate";

export default function FounderLayout({ children }: { children: React.ReactNode }) {
  return <OnboardingGate>{children}</OnboardingGate>;
}
