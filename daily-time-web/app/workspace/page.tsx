import { Suspense } from "react";
import { WorkspaceView } from "@/components/features/views/WorkspaceView";

export default function WorkspacePage() {
  return (
    <Suspense fallback={<p className="text-sm text-[var(--muted)]">Cargando informe…</p>}>
      <WorkspaceView />
    </Suspense>
  );
}
