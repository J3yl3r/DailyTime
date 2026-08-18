export type VoiceIntent =
  | "create_task"
  | "create_note"
  | "open_task_form"
  | "open_note_form"
  | "open_person_form"
  | "create_person"
  | "open_project_form"
  | "create_project"
  | "open_status_form"
  | "create_status"
  | "open_category_form"
  | "create_category"
  | "open_vault_account_form"
  | "create_vault_account"
  | "open_vault_password_form"
  | "create_vault_password"
  | "open_career_catalog_form"
  | "create_career_catalog"
  | "open_edit_career_catalog_form"
  | "delete_career_catalog"
  | "open_work_experience_form"
  | "create_work_experience"
  | "open_edit_work_experience_form"
  | "delete_work_experience"
  | "open_job_application_form"
  | "create_job_application"
  | "open_edit_job_application_form"
  | "delete_job_application"
  | "open_vault_service_form"
  | "create_vault_service"
  | "open_edit_vault_service_form"
  | "delete_vault_service"
  | "open_edit_task_form"
  | "open_edit_note_form"
  | "open_edit_person_form"
  | "open_edit_project_form"
  | "open_edit_status_form"
  | "open_edit_category_form"
  | "open_edit_vault_account_form"
  | "open_edit_vault_password_form"
  | "list_tasks"
  | "list_notes"
  | "list_people"
  | "list_projects"
  | "list_statuses"
  | "list_categories"
  | "list_work_experiences"
  | "list_job_applications"
  | "complete_task"
  | "complete_note"
  | "delete_task"
  | "delete_note"
  | "delete_person"
  | "delete_project"
  | "delete_status"
  | "delete_category"
  | "delete_vault_account"
  | "delete_vault_password"
  | "add_time"
  | "filter_workspace"
  | "calendar_navigate"
  | "navigate"
  | "help"
  | "unknown";

export type VoiceCommandResponse = {
  success: boolean;
  transcript: string;
  intent: VoiceIntent;
  confidence: number;
  message: string;
  navigate_to: string | null;
  data: unknown;
  dry_run: boolean;
  requires_confirmation: boolean;
};

export type VoiceCommandRequest = {
  text: string;
  work_date?: string;
  dry_run?: boolean;
};

export type VoiceCalendarCommand = {
  action: "today" | "prev" | "next" | "set_mode";
  mode?: "day" | "week" | "month" | "year";
  unit?: "day" | "week" | "month" | "year";
};
