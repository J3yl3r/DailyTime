"use client";

import { useCallback, useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { useQueryClient } from "@tanstack/react-query";
import { AudioLines, BookOpen, Loader2, X } from "lucide-react";
import { toast } from "sonner";
import { useVoiceCommand } from "@/hooks/use-voice-command";
import { useVoiceForm } from "@/providers/voice-form-provider";
import { useVoiceUiBridge } from "@/providers/voice-ui-bridge";
import { VOICE_COMMAND_GUIDE, VOICE_GUIDE_INTRO } from "@/lib/voice/command-guide";
import type { VoiceFormDraft } from "@/lib/voice/form-commands";
import {
  categoryKeys,
  careerCatalogKeys,
  jobApplicationKeys,
  noteKeys,
  personKeys,
  projectKeys,
  statusKeys,
  taskItemKeys,
  timeEntryKeys,
  vaultAccountKeys,
  vaultPasswordKeys,
  vaultServiceKeys,
  workExperienceKeys,
} from "@/lib/query/keys";
import { cn } from "@/lib/utils/cn";
import type {
  VoiceCalendarCommand,
  VoiceCommandResponse,
  VoiceIntent,
} from "@/types/voice";

const WRITE_INTENTS: VoiceIntent[] = [
  "complete_task",
  "complete_note",
  "add_time",
  "delete_task",
  "delete_note",
  "delete_person",
  "delete_project",
  "delete_status",
  "delete_category",
  "delete_vault_account",
  "delete_vault_password",
  "delete_career_catalog",
  "delete_work_experience",
  "delete_job_application",
  "delete_vault_service",
];

const FORM_OPEN_INTENTS: VoiceIntent[] = [
  "create_task",
  "create_note",
  "open_task_form",
  "open_note_form",
  "open_person_form",
  "create_person",
  "open_project_form",
  "create_project",
  "open_status_form",
  "create_status",
  "open_category_form",
  "create_category",
  "open_vault_account_form",
  "create_vault_account",
  "open_vault_password_form",
  "create_vault_password",
  "open_career_catalog_form",
  "create_career_catalog",
  "open_edit_career_catalog_form",
  "open_work_experience_form",
  "create_work_experience",
  "open_edit_work_experience_form",
  "open_job_application_form",
  "create_job_application",
  "open_edit_job_application_form",
  "open_vault_service_form",
  "create_vault_service",
  "open_edit_vault_service_form",
  "open_edit_task_form",
  "open_edit_note_form",
  "open_edit_person_form",
  "open_edit_project_form",
  "open_edit_status_form",
  "open_edit_category_form",
  "open_edit_vault_account_form",
  "open_edit_vault_password_form",
];

function invalidateForIntent(
  queryClient: ReturnType<typeof useQueryClient>,
  intent: VoiceIntent,
) {
  void queryClient.invalidateQueries({ queryKey: taskItemKeys.all });
  void queryClient.invalidateQueries({ queryKey: noteKeys.all });
  void queryClient.invalidateQueries({ queryKey: timeEntryKeys.all });
  void queryClient.invalidateQueries({ queryKey: statusKeys.all });
  void queryClient.invalidateQueries({ queryKey: categoryKeys.all });
  void queryClient.invalidateQueries({ queryKey: personKeys.all });
  void queryClient.invalidateQueries({ queryKey: projectKeys.all });
  if (intent.includes("vault")) {
    void queryClient.invalidateQueries({ queryKey: vaultAccountKeys.all });
    void queryClient.invalidateQueries({ queryKey: vaultPasswordKeys.all });
    void queryClient.invalidateQueries({ queryKey: vaultServiceKeys.all });
  }
  if (
    intent.includes("career") ||
    intent.includes("work_experience") ||
    intent.includes("job_application")
  ) {
    void queryClient.invalidateQueries({ queryKey: careerCatalogKeys.all });
    void queryClient.invalidateQueries({ queryKey: workExperienceKeys.all });
    void queryClient.invalidateQueries({ queryKey: jobApplicationKeys.all });
  }
}

export function VoiceDock() {
  const router = useRouter();
  const queryClient = useQueryClient();
  const voiceForm = useVoiceForm();
  const { dispatchCalendarCommand } = useVoiceUiBridge();
  const [guideOpen, setGuideOpen] = useState(false);

  const beforeSend = useCallback(
    async (text: string) => {
      const normalized = text
        .trim()
        .toLowerCase()
        .normalize("NFD")
        .replace(/[\u0300-\u036f]/g, "");
      if (
        guideOpen &&
        /^(cerrar|cierra|cancelar)(\s+(la\s+)?(ayuda|manual|guia))?$/.test(
          normalized,
        )
      ) {
        setGuideOpen(false);
        toast.info("Ayuda cerrada");
        return false;
      }

      if (!voiceForm.isOpen) return true;
      const { handled, message } = voiceForm.handleVoiceText(text);
      if (!handled) return true;
      if (message) toast.info(message);
      return false;
    },
    [guideOpen, voiceForm],
  );

  const quickVoice = useVoiceCommand({ sessionMode: true, beforeSend });

  const handleVoiceResult = useCallback(
    (result: VoiceCommandResponse | null) => {
      if (!result) return;

      if (result.intent === "help" && result.success) {
        setGuideOpen(true);
        toast.info("Manual de ayuda abierto");
        return;
      }

      if (FORM_OPEN_INTENTS.includes(result.intent) && result.requires_confirmation) {
        const draft = (result.data ?? {}) as VoiceFormDraft;
        switch (result.intent) {
          case "create_task":
          case "open_task_form":
          case "open_edit_task_form":
            voiceForm.openTaskForm(draft);
            break;
          case "create_note":
          case "open_note_form":
          case "open_edit_note_form":
            voiceForm.openNoteForm(draft);
            break;
          case "create_person":
          case "open_person_form":
          case "open_edit_person_form":
            voiceForm.openPersonForm(draft);
            break;
          case "create_project":
          case "open_project_form":
          case "open_edit_project_form":
            voiceForm.openProjectForm(draft);
            break;
          case "create_status":
          case "open_status_form":
          case "open_edit_status_form":
            voiceForm.openStatusForm(draft);
            break;
          case "create_category":
          case "open_category_form":
          case "open_edit_category_form":
            voiceForm.openCategoryForm(draft);
            break;
          case "create_vault_account":
          case "open_vault_account_form":
          case "open_edit_vault_account_form":
            voiceForm.openVaultAccountForm(draft);
            break;
          case "create_vault_password":
          case "open_vault_password_form":
          case "open_edit_vault_password_form":
            voiceForm.openVaultPasswordForm(draft);
            break;
          case "create_career_catalog":
          case "open_career_catalog_form":
          case "open_edit_career_catalog_form":
            voiceForm.openCareerCatalogForm(draft);
            break;
          case "create_vault_service":
          case "open_vault_service_form":
          case "open_edit_vault_service_form":
            voiceForm.openVaultServiceForm(draft);
            break;
          case "create_work_experience":
          case "open_work_experience_form":
          case "open_edit_work_experience_form":
            voiceForm.openWorkExperienceForm(draft);
            break;
          case "create_job_application":
          case "open_job_application_form":
          case "open_edit_job_application_form":
            voiceForm.openJobApplicationForm(draft);
            break;
          default:
            break;
        }
        toast.info(result.message.split("\n")[0] ?? "Formulario abierto");
        if (result.navigate_to) router.push(result.navigate_to);
        return;
      }

      if (result.success) {
        const lines = result.message
          .split("\n")
          .map((line) => line.trim())
          .filter(Boolean);
        const title = lines[0] ?? "Comando ejecutado";
        const description =
          lines.length > 1 ? lines.slice(1).join("\n").slice(0, 400) : undefined;
        toast.success(title, description ? { description } : undefined);
      } else {
        toast.error(result.message);
      }

      if (result.success && WRITE_INTENTS.includes(result.intent)) {
        invalidateForIntent(queryClient, result.intent);
      }

      if (result.success && result.intent === "calendar_navigate") {
        const data = (result.data ?? {}) as VoiceCalendarCommand;
        if (result.navigate_to) {
          router.push(result.navigate_to);
          window.setTimeout(() => {
            dispatchCalendarCommand(data);
          }, 120);
        } else {
          dispatchCalendarCommand(data);
        }
        return;
      }

      if (result.success && result.navigate_to) {
        router.push(result.navigate_to);
      }
    },
    [dispatchCalendarCommand, queryClient, router, voiceForm],
  );

  useEffect(() => {
    handleVoiceResult(quickVoice.lastResult);
  }, [quickVoice.lastResult, handleVoiceResult]);

  const quickListening = quickVoice.status === "listening";
  const quickSending = quickVoice.status === "sending";

  const disarmQuickVoice = useCallback(() => {
    quickVoice.setSessionArmed(false);
    quickVoice.stopListening();
  }, [quickVoice]);

  const runExample = async (example: string) => {
    setGuideOpen(false);
    if (!quickVoice.armed) {
      quickVoice.toggleSession();
    }
    await quickVoice.sendText(example);
  };

  const toggleQuickVoice = () => {
    if (quickVoice.armed) {
      disarmQuickVoice();
      return;
    }
    setGuideOpen(false);
    quickVoice.toggleSession();
  };

  const openGuide = () => {
    setGuideOpen((current) => !current);
    if (!guideOpen) disarmQuickVoice();
  };

  return (
    <div className="pointer-events-none fixed bottom-5 right-5 z-[60] flex flex-col items-end gap-3">
      {guideOpen && (
        <section className="pointer-events-auto flex max-h-[min(70dvh,32rem)] w-[min(100vw-2rem,24rem)] flex-col overflow-hidden rounded-2xl border border-[var(--border)] bg-[var(--surface)] shadow-[var(--shadow-card)]">
          <header className="flex shrink-0 items-center justify-between gap-2 border-b border-[var(--border)] px-4 py-3">
            <div className="flex items-center gap-2">
              <BookOpen className="size-4 text-[var(--accent)]" />
              <div>
                <p className="text-sm font-medium text-[var(--ink)]">Qué puedes hacer</p>
                <p className="text-xs text-[var(--muted)]">{VOICE_GUIDE_INTRO}</p>
              </div>
            </div>
            <button
              type="button"
              onClick={() => setGuideOpen(false)}
              className="rounded-md p-1.5 text-[var(--muted)] hover:bg-[var(--surface-muted)] hover:text-[var(--ink)]"
              aria-label="Cerrar guía de comandos"
            >
              <X className="size-4" />
            </button>
          </header>
          <ul className="flex min-h-0 flex-1 flex-col gap-3 overflow-y-auto p-4">
            {VOICE_COMMAND_GUIDE.map((command) => (
              <li
                key={command.id}
                className="rounded-xl border border-[var(--border)] bg-[var(--surface-muted)]/40 px-3 py-2.5"
              >
                <p className="text-sm font-medium text-[var(--ink)]">{command.title}</p>
                <p className="mt-0.5 text-xs text-[var(--muted)]">{command.description}</p>
                {command.voiceFields?.length ? (
                  <div className="mt-2 rounded-md border border-[var(--border)] bg-[var(--surface)] px-2.5 py-2">
                    <p className="text-[10px] font-semibold uppercase tracking-wide text-[var(--muted)]">
                      Campos por voz
                    </p>
                    <ul className="mt-1 space-y-0.5 text-xs text-[var(--ink)]">
                      {command.voiceFields.map((field) => (
                        <li key={field}>• {field}</li>
                      ))}
                    </ul>
                  </div>
                ) : null}
                {command.examples.length > 0 ? (
                  <div className="mt-2 flex flex-col gap-1.5">
                    {command.examples.map((example) => (
                      <button
                        key={example}
                        type="button"
                        onClick={() => void runExample(example)}
                        disabled={quickSending}
                        className="rounded-md border border-[var(--border)] bg-[var(--surface)] px-2.5 py-1.5 text-left text-xs text-[var(--ink)] transition-colors hover:border-[var(--accent)] hover:text-[var(--accent)] disabled:opacity-50"
                        title="Probar este ejemplo"
                      >
                        “{example}”
                      </button>
                    ))}
                  </div>
                ) : null}
              </li>
            ))}
          </ul>
        </section>
      )}

      <div className="pointer-events-auto flex flex-col items-center gap-2">
        <button
          type="button"
          onClick={openGuide}
          className={cn(
            "inline-flex size-12 items-center justify-center rounded-full border border-[var(--border)] bg-[var(--surface)] text-[var(--accent)] shadow-[var(--shadow-card)] transition-transform hover:scale-[1.03]",
            guideOpen && "ring-4 ring-[var(--accent)]/20",
          )}
          aria-label={guideOpen ? "Cerrar guía de comandos" : "Qué puedes hacer"}
          title="Qué puedes hacer"
        >
          <BookOpen className="size-5" />
        </button>

        <button
          type="button"
          onClick={toggleQuickVoice}
          disabled={!quickVoice.supported || quickSending}
          className={cn(
            "inline-flex size-14 items-center justify-center rounded-full border shadow-[var(--shadow-card)] transition-all hover:scale-[1.03] disabled:opacity-50",
            quickVoice.armed
              ? "border-[var(--accent)] bg-[var(--accent)] text-white ring-4 ring-[var(--accent)]/30"
              : "border-[var(--border)] bg-[var(--surface)] text-[var(--accent)]",
            quickListening && quickVoice.armed && "animate-pulse",
          )}
          aria-label={
            quickVoice.armed ? "Desactivar voz rápida" : "Activar voz rápida"
          }
          title={
            quickVoice.armed
              ? "Voz activa — clic para desactivar"
              : "Activar comandos por voz"
          }
        >
          {quickSending ? (
            <Loader2 className="size-6 animate-spin" />
          ) : (
            <AudioLines className="size-6" />
          )}
        </button>
      </div>
    </div>
  );
}
