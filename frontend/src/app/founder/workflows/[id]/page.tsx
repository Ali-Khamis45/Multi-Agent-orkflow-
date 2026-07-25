import { WorkflowDetail } from "@/components/workflows/workflow-detail";

export default async function FounderWorkflowRunPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;
  return <WorkflowDetail runId={id} />;
}
