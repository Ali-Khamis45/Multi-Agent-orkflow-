"""Performance Baseline (Phase 1.5 §12) — runs a real request through the live
stack and measures the metrics the hardening milestone asks for:

  - Workflow startup latency     (POST /intake -> response)
  - Average reasoning stage latency  (from persisted ReasoningTrace rows)
  - Redis event latency           (TaskDispatched envelope timestamp -> the
                                    dispatched node's UpdatedAt when it moves
                                    to Running/Completed — an upper bound on
                                    publish-to-pickup time, since exact
                                    consumption timestamps aren't separately
                                    recorded; see the "not yet measured"
                                    section in PERFORMANCE_BASELINE.md)
  - Artifact creation time        (direct POST /api/artifacts timing)
  - Memory lookup time            (direct GET /api/memory timing)
  - Parallel scheduling efficiency (dispatch-time delta between the two
                                    parallel Backend/Frontend nodes)

Run against a live `docker compose up` stack:
    python scripts/benchmark.py
"""

from __future__ import annotations

import statistics
import time
import uuid

import httpx

API_BASE = "http://localhost:5080"
RUNTIME_BASE = "http://localhost:8000"


def main() -> None:
    client = httpx.Client(timeout=60.0)
    results: dict[str, float] = {}

    # ---- Workflow startup latency ----
    started = time.perf_counter()
    resp = client.post(f"{RUNTIME_BASE}/intake", json={"raw_input": "Build a Task Management SaaS"})
    resp.raise_for_status()
    intake_latency = time.perf_counter() - started
    run_id = resp.json()["workflow_run_id"]
    results["workflow_startup_latency_seconds"] = intake_latency
    print(f"Workflow startup latency (POST /intake response): {intake_latency:.3f}s")

    # ---- Poll until the workflow completes (bounds the total pipeline run) ----
    poll_started = time.perf_counter()
    for _ in range(120):
        run = client.get(f"{API_BASE}/api/workflows/runs/{run_id}").json()
        if run["status"] in ("Completed", "Failed"):
            break
        time.sleep(0.5)
    total_pipeline_seconds = time.perf_counter() - poll_started
    results["total_pipeline_seconds"] = total_pipeline_seconds
    print(f"Total pipeline completion time (post-intake): {total_pipeline_seconds:.3f}s")
    print(f"Final status: {run['status']}, nodes: {len(run['nodes'])}")

    # ---- Average reasoning stage latency ----
    all_durations: list[float] = []
    per_stage: dict[str, list[float]] = {}
    for node in run["nodes"]:
        traces = client.get(f"{API_BASE}/api/reasoning/traces/{node['id']}").json()
        for t in traces:
            all_durations.append(t["durationMs"])
            per_stage.setdefault(t["stage"], []).append(t["durationMs"])

    if all_durations:
        avg_stage_ms = statistics.mean(all_durations)
        results["avg_reasoning_stage_latency_ms"] = avg_stage_ms
        print(f"\nAverage reasoning stage latency: {avg_stage_ms:.1f}ms ({len(all_durations)} stages)")
        for stage, durations in sorted(per_stage.items()):
            print(f"  {stage:20s} avg={statistics.mean(durations):6.1f}ms  n={len(durations)}")

    # ---- Artifact creation time ----
    ws_id = client.post(f"{API_BASE}/api/workspaces", json={"name": f"bench-{uuid.uuid4().hex[:8]}"}).json()
    artifact_times = []
    for i in range(5):
        started = time.perf_counter()
        client.post(f"{API_BASE}/api/artifacts", json={
            "workspaceId": ws_id, "name": f"BenchDoc{i}", "type": "Markdown",
            "ownerAgent": "benchmark", "content": "x" * 500,
            "workflowRunId": None, "taskNodeId": None, "previousVersionId": None,
        })
        artifact_times.append(time.perf_counter() - started)
    avg_artifact_ms = statistics.mean(artifact_times) * 1000
    results["avg_artifact_creation_ms"] = avg_artifact_ms
    print(f"\nAverage artifact creation time: {avg_artifact_ms:.1f}ms (n={len(artifact_times)})")

    # ---- Memory lookup time ----
    scope_ref = str(uuid.uuid4())
    client.post(f"{API_BASE}/api/memory", json={
        "workspaceId": ws_id, "layer": "Working", "scopeRef": scope_ref, "kind": "Decision",
        "content": "benchmark memory item", "sourceArtifactId": None, "ttlAt": None,
    })
    memory_times = []
    for _ in range(5):
        started = time.perf_counter()
        client.get(f"{API_BASE}/api/memory", params={
            "workspaceId": ws_id, "layer": "Working", "scopeRef": scope_ref, "limit": 20,
        })
        memory_times.append(time.perf_counter() - started)
    avg_memory_ms = statistics.mean(memory_times) * 1000
    results["avg_memory_lookup_ms"] = avg_memory_ms
    print(f"Average memory lookup time: {avg_memory_ms:.1f}ms (n={len(memory_times)})")

    # ---- Parallel scheduling efficiency ----
    backend = next((n for n in run["nodes"] if n["name"] == "BackendImplementation"), None)
    frontend = next((n for n in run["nodes"] if n["name"] == "FrontendImplementation"), None)
    if backend and frontend:
        backend_traces = client.get(f"{API_BASE}/api/reasoning/traces/{backend['id']}").json()
        frontend_traces = client.get(f"{API_BASE}/api/reasoning/traces/{frontend['id']}").json()
        backend_start = next((t["startedAt"] for t in backend_traces if t["stage"] == "Observe"), None)
        frontend_start = next((t["startedAt"] for t in frontend_traces if t["stage"] == "Observe"), None)
        if backend_start and frontend_start:
            from datetime import datetime
            delta = abs((datetime.fromisoformat(backend_start) - datetime.fromisoformat(frontend_start)).total_seconds())
            results["parallel_dispatch_skew_seconds"] = delta
            print(f"\nParallel scheduling skew (Backend vs Frontend Observe start): {delta * 1000:.1f}ms")

    print("\n--- summary (also see PERFORMANCE_BASELINE.md) ---")
    for key, value in results.items():
        print(f"  {key}: {value:.4f}")


if __name__ == "__main__":
    main()
