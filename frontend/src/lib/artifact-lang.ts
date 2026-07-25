import type { ArtifactType } from "./types";

/** Monaco language id for an artifact, preferring the file extension in its
 * name (e.g. "BackendCode.cs") and falling back to its declared type. */
export function artifactLanguage(name: string, type: ArtifactType): string {
  const ext = name.split(".").pop()?.toLowerCase();
  const byExt: Record<string, string> = {
    ts: "typescript", tsx: "typescript", js: "javascript", jsx: "javascript",
    cs: "csharp", py: "python", json: "json", md: "markdown", sql: "sql",
    yml: "yaml", yaml: "yaml", dockerfile: "dockerfile", html: "html", css: "css",
    sh: "shell", go: "go", rs: "rust", java: "java",
  };
  if (ext && byExt[ext]) return byExt[ext];

  const byType: Record<ArtifactType, string> = {
    Code: "typescript",
    Markdown: "markdown",
    Json: "json",
    Test: "typescript",
    Dockerfile: "dockerfile",
    Sql: "sql",
    Image: "plaintext",
    Diagram: "plaintext",
  };
  return byType[type] ?? "plaintext";
}

export function downloadArtifact(name: string, content: string): void {
  const blob = new Blob([content], { type: "text/plain;charset=utf-8" });
  const url = URL.createObjectURL(blob);
  const a = document.createElement("a");
  a.href = url;
  a.download = name.includes(".") ? name : `${name}.txt`;
  document.body.appendChild(a);
  a.click();
  a.remove();
  URL.revokeObjectURL(url);
}
