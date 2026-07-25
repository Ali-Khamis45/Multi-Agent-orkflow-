import { AgentProfile } from "@/components/agents/agent-profile";

export default async function AgentProfilePage({ params }: { params: Promise<{ name: string }> }) {
  const { name } = await params;
  return <AgentProfile agentName={decodeURIComponent(name)} />;
}
