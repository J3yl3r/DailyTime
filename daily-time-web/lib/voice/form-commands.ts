export type VoiceFormKind =
  | "task"
  | "note"
  | "person"
  | "project"
  | "status"
  | "category"
  | "vault_account"
  | "vault_password"
  | "career_catalog"
  | "vault_service"
  | "work_experience"
  | "job_application";

export type VoiceFormFieldName =
  | "title"
  | "content"
  | "workDate"
  | "statusId"
  | "categoryId"
  | "personId"
  | "projectId"
  | "startTime"
  | "endTime"
  | "name"
  | "description"
  | "color"
  | "isFinal"
  | "isActive"
  | "itemType"
  | "serviceName"
  | "username"
  | "password"
  | "url"
  | "notes"
  | "tags"
  | "companyName"
  | "positionName"
  | "locationName"
  | "fieldName"
  | "statusName"
  | "startDate"
  | "endDate"
  | "appliedAt"
  | "summary"
  | "achievements"
  | "technologies"
  | "contact"
  | "isCurrent";

export type VoiceFormFieldPatch = {
  id: number;
  field: VoiceFormFieldName;
  value: string | number | boolean | null;
  label: string;
};

export type VoiceFormCommandResult =
  | { type: "field"; patch: Omit<VoiceFormFieldPatch, "id">; message: string }
  | { type: "submit"; message: string }
  | { type: "cancel"; message: string }
  | { type: "unknown"; message: string };

type CatalogItem = { id: number; name: string; isActive?: boolean };

type ParseContext = {
  kind: VoiceFormKind;
  statuses: CatalogItem[];
  categories: CatalogItem[];
  people?: CatalogItem[];
  projects?: CatalogItem[];
  today?: Date;
};

function normalize(text: string): string {
  return text
    .trim()
    .toLowerCase()
    .normalize("NFD")
    .replace(/[\u0300-\u036f]/g, "")
    .replace(/[.,;:¡!¿?"'`´]+/g, " ")
    .replace(/\s+/g, " ")
    .trim();
}

function restoreCasing(rawText: string, normalizedValue: string): string {
  const pattern = normalizedValue
    .split(/\s+/)
    .filter(Boolean)
    .map(escapeRegExp)
    .join("[\\s.,;:¡!¿?\"'`´]+");
  if (!pattern) return normalizedValue;
  const match = rawText.match(new RegExp(pattern, "i"));
  return match?.[0]?.replace(/^[.,;:\s]+|[.,;:\s]+$/g, "").trim() || normalizedValue;
}

function parseWorkDate(value: string, today = new Date()): string | null {
  const text = normalize(value);
  const base = new Date(today);
  base.setHours(0, 0, 0, 0);

  if (/\bhoy\b/.test(text)) return formatIsoDate(base);
  if (/\bmanana\b/.test(text)) {
    base.setDate(base.getDate() + 1);
    return formatIsoDate(base);
  }
  if (/\bayer\b/.test(text)) {
    base.setDate(base.getDate() - 1);
    return formatIsoDate(base);
  }

  const match = text.match(/\b(\d{1,2})[\/\-](\d{1,2})(?:[\/\-](\d{2,4}))?\b/);
  if (!match) return null;

  const day = Number(match[1]);
  const month = Number(match[2]);
  let year = match[3] ? Number(match[3]) : base.getFullYear();
  if (year < 100) year += 2000;

  const parsed = new Date(year, month - 1, day);
  if (Number.isNaN(parsed.getTime())) return null;
  return formatIsoDate(parsed);
}

function formatIsoDate(date: Date): string {
  const y = date.getFullYear();
  const m = String(date.getMonth() + 1).padStart(2, "0");
  const d = String(date.getDate()).padStart(2, "0");
  return `${y}-${m}-${d}`;
}

function parseTime(value: string): string | null {
  const text = normalize(value);
  const hhmm = text.match(/\b(\d{1,2}):(\d{2})\b/);
  if (hhmm) {
    const hours = Number(hhmm[1]);
    const minutes = Number(hhmm[2]);
    if (hours >= 0 && hours <= 23 && minutes >= 0 && minutes <= 59) {
      return `${String(hours).padStart(2, "0")}:${String(minutes).padStart(2, "0")}`;
    }
  }

  const hourOnly = text.match(/\b(\d{1,2})\b/);
  if (hourOnly) {
    const hours = Number(hourOnly[1]);
    if (hours >= 0 && hours <= 23) {
      return `${String(hours).padStart(2, "0")}:00`;
    }
  }

  return null;
}

function findByName(items: CatalogItem[], query: string): CatalogItem | null {
  const needle = normalize(query);
  if (!needle) return null;

  const exact = items.find((item) => normalize(item.name) === needle);
  if (exact) return exact;

  return (
    items.find((item) => normalize(item.name).includes(needle)) ??
    items.find((item) => needle.includes(normalize(item.name))) ??
    null
  );
}

function escapeRegExp(value: string): string {
  return value.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
}

function extractFieldValue(text: string, labels: string[]): string | null {
  for (const label of labels) {
    const token = escapeRegExp(normalize(label));
    const patterns = [
      new RegExp(
        `^(?:el|la|los|las)?\\s*${token}\\s*(?:de\\s+(?:la\\s+)?(?:tarea|nota|cuenta|credencial)?\\s*)?(?:es|sera|será|como)?\\s*(.+)$`,
        "i",
      ),
      new RegExp(`\\b${token}\\s+(?:es|sera|será|como)\\s+(.+)$`, "i"),
      new RegExp(`\\b${token}\\s+(.+)$`, "i"),
    ];
    for (const pattern of patterns) {
      const match = text.match(pattern);
      const value = match?.[1]?.trim();
      if (value) return value;
    }
  }
  return null;
}

function parseBoolean(value: string): boolean | null {
  const text = normalize(value);
  if (/^(si|sí|true|activo|activa|final|verdadero|1)$/.test(text)) return true;
  if (/^(no|false|inactivo|inactiva|ninguno|ninguna|falso|0)$/.test(text)) return false;
  return null;
}

function parseItemType(value: string): "task" | "note" | null {
  const text = normalize(value);
  if (/^(tarea|tareas|task)$/.test(text)) return "task";
  if (/^(nota|notas|note)$/.test(text)) return "note";
  return null;
}

const COLOR_MAP: Record<string, string> = {
  rojo: "#EF4444",
  verde: "#22C55E",
  azul: "#3B82F6",
  amarillo: "#EAB308",
  naranja: "#F97316",
  morado: "#A855F7",
  violeta: "#8B5CF6",
  rosa: "#EC4899",
  gris: "#64748B",
  negro: "#111827",
  blanco: "#F8FAFC",
  cyan: "#06B6D4",
  teal: "#14B8A6",
};

function parseColor(value: string): string | null {
  const text = normalize(value).replace(/\s+/g, "");
  const hex = text.match(/^#?[0-9a-f]{6}$/);
  if (hex) {
    const raw = hex[0].startsWith("#") ? hex[0] : `#${hex[0]}`;
    return raw.toUpperCase();
  }
  return COLOR_MAP[text] ?? null;
}

const SUBMIT_RE =
  /^(guardar|guardala|guardalo|crear|creala|crealo|confirmar|confirmala|confirmalo|listo|terminar|finalizar)$/;

const CANCEL_RE = /^(cancelar|cancela|cerrar|cierra|descartar|descarta|salir)$/;

const CATALOG_KINDS: VoiceFormKind[] = [
  "person",
  "project",
  "status",
  "category",
  "vault_account",
  "vault_password",
  "career_catalog",
  "vault_service",
];

const CAREER_RECORD_KINDS: VoiceFormKind[] = [
  "work_experience",
  "job_application",
];

function parseCareerRecordCommand(
  rawText: string,
  text: string,
  kind: VoiceFormKind,
  today: Date,
): VoiceFormCommandResult {
  const companyValue = extractFieldValue(text, ["empresa", "company"]);
  if (companyValue) {
    const display = restoreCasing(rawText, companyValue);
    return {
      type: "field",
      patch: { field: "companyName", value: display, label: "Empresa" },
      message: `Empresa: ${display}`,
    };
  }

  const positionValue = extractFieldValue(text, [
    "cargo",
    "puesto",
    "posicion",
    "position",
    "rol",
  ]);
  if (positionValue) {
    const display = restoreCasing(rawText, positionValue);
    return {
      type: "field",
      patch: { field: "positionName", value: display, label: "Cargo" },
      message: `Cargo: ${display}`,
    };
  }

  const locationValue = extractFieldValue(text, [
    "ubicacion",
    "location",
    "ciudad",
    "lugar",
  ]);
  if (locationValue) {
    const display = restoreCasing(rawText, locationValue);
    return {
      type: "field",
      patch: { field: "locationName", value: display, label: "Ubicación" },
      message: `Ubicación: ${display}`,
    };
  }

  const fieldValue = extractFieldValue(text, [
    "carrera",
    "area",
    "campo",
    "field",
  ]);
  if (fieldValue) {
    const display = restoreCasing(rawText, fieldValue);
    return {
      type: "field",
      patch: { field: "fieldName", value: display, label: "Carrera" },
      message: `Carrera: ${display}`,
    };
  }

  if (kind === "job_application") {
    const statusValue = extractFieldValue(text, ["estado", "status"]);
    if (statusValue) {
      const display = restoreCasing(rawText, statusValue);
      return {
        type: "field",
        patch: { field: "statusName", value: display, label: "Estado" },
        message: `Estado: ${display}`,
      };
    }

    const appliedValue = extractFieldValue(text, [
      "fecha",
      "aplicada",
      "aplicado",
      "postulada",
      "postulado",
      "dia",
    ]);
    if (appliedValue) {
      const parsed = parseWorkDate(appliedValue, today);
      if (parsed) {
        return {
          type: "field",
          patch: { field: "appliedAt", value: parsed, label: "Fecha" },
          message: `Fecha: ${parsed}`,
        };
      }
      return {
        type: "unknown",
        message: `No entendí la fecha «${appliedValue}». Prueba hoy o DD/MM/AAAA.`,
      };
    }

    const urlValue = extractFieldValue(text, ["url", "enlace", "link", "pagina"]);
    if (urlValue) {
      const display = restoreCasing(rawText, urlValue);
      return {
        type: "field",
        patch: { field: "url", value: display, label: "URL" },
        message: `URL: ${display}`,
      };
    }

    const contactValue = extractFieldValue(text, ["contacto", "contact"]);
    if (contactValue) {
      const display = restoreCasing(rawText, contactValue);
      return {
        type: "field",
        patch: { field: "contact", value: display, label: "Contacto" },
        message: `Contacto: ${display}`,
      };
    }

    const notesValue = extractFieldValue(text, ["notas", "nota", "comentarios"]);
    if (notesValue) {
      const display = restoreCasing(rawText, notesValue);
      return {
        type: "field",
        patch: { field: "notes", value: display, label: "Notas" },
        message: `Notas: ${display}`,
      };
    }
  }

  if (kind === "work_experience") {
    const startValue = extractFieldValue(text, [
      "inicio",
      "empieza",
      "desde",
      "start",
    ]);
    if (startValue) {
      const parsed = parseWorkDate(startValue, today);
      if (parsed) {
        return {
          type: "field",
          patch: { field: "startDate", value: parsed, label: "Inicio" },
          message: `Inicio: ${parsed}`,
        };
      }
      return {
        type: "unknown",
        message: `No entendí la fecha «${startValue}». Prueba hoy o DD/MM/AAAA.`,
      };
    }

    const endValue = extractFieldValue(text, ["fin", "hasta", "termino", "end"]);
    if (endValue) {
      const parsed = parseWorkDate(endValue, today);
      if (parsed) {
        return {
          type: "field",
          patch: { field: "endDate", value: parsed, label: "Fin" },
          message: `Fin: ${parsed}`,
        };
      }
      return {
        type: "unknown",
        message: `No entendí la fecha «${endValue}». Prueba hoy o DD/MM/AAAA.`,
      };
    }

    if (
      /^(trabajo\s+actual|actual|es\s+actual|empleo\s+actual)$/.test(text) ||
      /^actual\s+(si|sí)$/.test(text)
    ) {
      return {
        type: "field",
        patch: { field: "isCurrent", value: true, label: "Actual" },
        message: "Trabajo actual: sí",
      };
    }
    if (/^actual\s+no$/.test(text)) {
      return {
        type: "field",
        patch: { field: "isCurrent", value: false, label: "Actual" },
        message: "Trabajo actual: no",
      };
    }

    const summaryValue = extractFieldValue(text, [
      "resumen",
      "summary",
      "descripcion",
    ]);
    if (summaryValue) {
      const display = restoreCasing(rawText, summaryValue);
      return {
        type: "field",
        patch: { field: "summary", value: display, label: "Resumen" },
        message: `Resumen: ${display}`,
      };
    }

    const achievementsValue = extractFieldValue(text, [
      "logros",
      "logro",
      "achievements",
    ]);
    if (achievementsValue) {
      const display = restoreCasing(rawText, achievementsValue);
      return {
        type: "field",
        patch: { field: "achievements", value: display, label: "Logros" },
        message: `Logros: ${display}`,
      };
    }

    const techValue = extractFieldValue(text, [
      "tecnologias",
      "tecnologia",
      "tech",
      "stack",
    ]);
    if (techValue) {
      const display = restoreCasing(rawText, techValue);
      return {
        type: "field",
        patch: { field: "technologies", value: display, label: "Tecnologías" },
        message: `Tecnologías: ${display}`,
      };
    }
  }

  if (!text.match(/^(empresa|cargo|puesto|ubicacion|carrera|estado|fecha|inicio|fin)/) && text.length >= 2) {
    const display = restoreCasing(rawText, text);
    return {
      type: "field",
      patch: { field: "companyName", value: display, label: "Empresa" },
      message: `Empresa: ${display}`,
    };
  }

  return {
    type: "unknown",
    message:
      kind === "work_experience"
        ? "Di «empresa …», «cargo …», «inicio …» o «guardar»."
        : "Di «empresa …», «cargo …», «estado …», «fecha …» o «guardar».",
  };
}

function parseCatalogCommand(
  rawText: string,
  text: string,
  kind: VoiceFormKind,
): VoiceFormCommandResult {
  if (kind === "vault_password") {
    const serviceValue = extractFieldValue(text, [
      "servicio",
      "service",
      "sitio",
      "aplicacion",
    ]);
    if (serviceValue) {
      const display = restoreCasing(rawText, serviceValue);
      return {
        type: "field",
        patch: { field: "serviceName", value: display, label: "Servicio" },
        message: `Servicio: ${display}`,
      };
    }

    const usernameValue = extractFieldValue(text, [
      "usuario",
      "correo",
      "email",
      "user",
    ]);
    if (usernameValue) {
      const display = restoreCasing(rawText, usernameValue);
      return {
        type: "field",
        patch: { field: "username", value: display, label: "Usuario" },
        message: `Usuario: ${display}`,
      };
    }

    const passwordValue = extractFieldValue(text, [
      "contrasena",
      "password",
      "clave",
      "pass",
    ]);
    if (passwordValue) {
      return {
        type: "field",
        patch: { field: "password", value: passwordValue, label: "Contraseña" },
        message: "Contraseña actualizada",
      };
    }

    const urlValue = extractFieldValue(text, ["url", "enlace", "link", "pagina"]);
    if (urlValue) {
      const display = restoreCasing(rawText, urlValue);
      return {
        type: "field",
        patch: { field: "url", value: display, label: "URL" },
        message: `URL: ${display}`,
      };
    }

    const tagsValue = extractFieldValue(text, ["etiquetas", "etiqueta", "tags", "tag"]);
    if (tagsValue) {
      const display = restoreCasing(rawText, tagsValue);
      return {
        type: "field",
        patch: { field: "tags", value: display, label: "Etiquetas" },
        message: `Etiquetas: ${display}`,
      };
    }

    const notesValue = extractFieldValue(text, ["notas", "nota", "comentarios"]);
    if (notesValue) {
      const display = restoreCasing(rawText, notesValue);
      return {
        type: "field",
        patch: { field: "notes", value: display, label: "Notas" },
        message: `Notas: ${display}`,
      };
    }
  }

  const nameValue = extractFieldValue(text, [
    "nombre",
    "name",
    ...(kind === "vault_account" ? ["cuenta"] : []),
  ]);
  if (nameValue) {
    const display = restoreCasing(rawText, nameValue);
    return {
      type: "field",
      patch: { field: "name", value: display, label: "Nombre" },
      message: `Nombre: ${display}`,
    };
  }

  const descriptionValue = extractFieldValue(text, [
    "descripcion",
    "description",
    "detalle",
    "detalles",
  ]);
  if (descriptionValue) {
    const display = restoreCasing(rawText, descriptionValue);
    return {
      type: "field",
      patch: { field: "description", value: display, label: "Descripción" },
      message: `Descripción: ${display}`,
    };
  }

  if (kind === "status" || kind === "category") {
    const typeValue = extractFieldValue(text, ["tipo", "type", "para"]);
    if (typeValue) {
      const itemType = parseItemType(typeValue);
      if (itemType) {
        return {
          type: "field",
          patch: {
            field: "itemType",
            value: itemType,
            label: "Tipo",
          },
          message: `Tipo: ${itemType === "task" ? "tarea" : "nota"}`,
        };
      }
      return {
        type: "unknown",
        message: `Tipo no válido «${typeValue}». Usa tarea o nota.`,
      };
    }
  }

  if (kind === "status") {
    const colorValue = extractFieldValue(text, ["color", "colour"]);
    if (colorValue) {
      const color = parseColor(colorValue);
      if (color) {
        return {
          type: "field",
          patch: { field: "color", value: color, label: "Color" },
          message: `Color: ${color}`,
        };
      }
      return {
        type: "unknown",
        message: `No entendí el color «${colorValue}». Prueba rojo, azul o #3B82F6.`,
      };
    }

    const finalValue = extractFieldValue(text, ["final", "es final", "estado final"]);
    if (finalValue) {
      const parsed = parseBoolean(finalValue);
      if (parsed != null) {
        return {
          type: "field",
          patch: { field: "isFinal", value: parsed, label: "Final" },
          message: `Final: ${parsed ? "sí" : "no"}`,
        };
      }
    }
    if (/^(final|es final|estado final)$/.test(text)) {
      return {
        type: "field",
        patch: { field: "isFinal", value: true, label: "Final" },
        message: "Final: sí",
      };
    }
  }

  if (
    kind === "person" ||
    kind === "project" ||
    kind === "category" ||
    kind === "career_catalog" ||
    kind === "vault_service"
  ) {
    const activeValue = extractFieldValue(text, [
      "activa",
      "activo",
      "activa",
      "estado",
    ]);
    if (activeValue) {
      const parsed = parseBoolean(activeValue);
      if (parsed != null) {
        return {
          type: "field",
          patch: { field: "isActive", value: parsed, label: "Activo" },
          message: `Activo: ${parsed ? "sí" : "no"}`,
        };
      }
    }
    if (/^(activa|activo)$/.test(text)) {
      return {
        type: "field",
        patch: { field: "isActive", value: true, label: "Activo" },
        message: "Activo: sí",
      };
    }
    if (/^(inactiva|inactivo)$/.test(text)) {
      return {
        type: "field",
        patch: { field: "isActive", value: false, label: "Activo" },
        message: "Activo: no",
      };
    }
  }

  if (kind === "vault_service" || kind === "career_catalog") {
    const urlValue = extractFieldValue(text, ["url", "enlace", "link", "pagina"]);
    if (urlValue) {
      const display = restoreCasing(rawText, urlValue);
      return {
        type: "field",
        patch: { field: "url", value: display, label: "URL" },
        message: `URL: ${display}`,
      };
    }

    const notesValue = extractFieldValue(text, ["notas", "nota", "comentarios"]);
    if (notesValue) {
      const display = restoreCasing(rawText, notesValue);
      return {
        type: "field",
        patch: { field: "notes", value: display, label: "Notas" },
        message: `Notas: ${display}`,
      };
    }

    if (kind === "career_catalog") {
      const colorValue = extractFieldValue(text, ["color", "colour"]);
      if (colorValue) {
        const color = parseColor(colorValue);
        if (color) {
          return {
            type: "field",
            patch: { field: "color", value: color, label: "Color" },
            message: `Color: ${color}`,
          };
        }
        return {
          type: "unknown",
          message: `No entendí el color «${colorValue}». Prueba rojo, azul o #3B82F6.`,
        };
      }
    }
  }

  const looksLabeled =
    /^(nombre|name|descripcion|description|tipo|type|color|final|activa|activo|inactiva|inactivo|servicio|usuario|correo|contrasena|password|url|etiquetas|notas|cuenta)\b/.test(
      text,
    );

  if (!looksLabeled && text.length >= 2) {
    const display = restoreCasing(rawText, text);
    if (kind === "vault_password") {
      return {
        type: "field",
        patch: { field: "serviceName", value: display, label: "Servicio" },
        message: `Servicio: ${display}`,
      };
    }
    return {
      type: "field",
      patch: { field: "name", value: display, label: "Nombre" },
      message: `Nombre: ${display}`,
    };
  }

  const hints: Record<string, string> = {
    person: "Di «nombre …», «descripción …» o «guardar».",
    project: "Di «nombre …», «descripción …» o «guardar».",
    status: "Di «nombre …», «descripción …», «tipo tarea|nota», «color …» o «guardar».",
    category: "Di «nombre …», «descripción …», «tipo tarea|nota» o «guardar».",
    vault_account: "Di «nombre …», «descripción …» o «guardar».",
    vault_password:
      "Di «servicio …», «usuario …», «contraseña …» o «guardar».",
    career_catalog: "Di «nombre …», «descripción …» o «guardar».",
    vault_service: "Di «nombre …», «url …», «notas …» o «guardar».",
  };

  return {
    type: "unknown",
    message: hints[kind] ?? "No entendí el comando.",
  };
}

export function parseVoiceFormCommand(
  rawText: string,
  context: ParseContext,
): VoiceFormCommandResult {
  const text = normalize(rawText);
  if (!text) {
    return { type: "unknown", message: "No entendí el comando." };
  }

  if (SUBMIT_RE.test(text)) {
    return { type: "submit", message: "Guardando…" };
  }

  if (CANCEL_RE.test(text)) {
    return { type: "cancel", message: "Formulario cancelado." };
  }

  if (CATALOG_KINDS.includes(context.kind)) {
    return parseCatalogCommand(rawText, text, context.kind);
  }

  if (CAREER_RECORD_KINDS.includes(context.kind)) {
    return parseCareerRecordCommand(
      rawText,
      text,
      context.kind,
      context.today ?? new Date(),
    );
  }

  // --- task / note ---
  const titleValue = extractFieldValue(text, [
    "titulo",
    "titula",
    "llamada",
    "titulada",
    "title",
  ]);
  if (titleValue) {
    const display = restoreCasing(rawText, titleValue);
    return {
      type: "field",
      patch: { field: "title", value: display, label: "Título" },
      message: `Título: ${display}`,
    };
  }

  if (context.kind === "note") {
    const contentValue = extractFieldValue(text, [
      "contenido",
      "texto",
      "cuerpo",
      "descripcion",
    ]);
    if (contentValue) {
      const display = restoreCasing(rawText, contentValue);
      return {
        type: "field",
        patch: { field: "content", value: display, label: "Contenido" },
        message: `Contenido: ${display}`,
      };
    }
  }

  const dateValue = extractFieldValue(text, ["fecha", "dia", "día", "para"]);
  if (dateValue) {
    const parsed = parseWorkDate(dateValue, context.today);
    if (parsed) {
      return {
        type: "field",
        patch: { field: "workDate", value: parsed, label: "Fecha" },
        message: `Fecha: ${parsed}`,
      };
    }
    return {
      type: "unknown",
      message: `No entendí la fecha «${dateValue}». Prueba hoy, mañana o DD/MM/AAAA.`,
    };
  }

  if (/\b(hoy|manana|ayer)\b/.test(text) && /fecha|dia|día|para/.test(text)) {
    const parsed = parseWorkDate(text, context.today);
    if (parsed) {
      return {
        type: "field",
        patch: { field: "workDate", value: parsed, label: "Fecha" },
        message: `Fecha: ${parsed}`,
      };
    }
  }

  const statusValue = extractFieldValue(text, ["estado"]);
  if (statusValue) {
    const status = findByName(context.statuses, statusValue);
    if (status) {
      return {
        type: "field",
        patch: { field: "statusId", value: status.id, label: "Estado" },
        message: `Estado: ${status.name}`,
      };
    }
    return {
      type: "unknown",
      message: `No encontré el estado «${statusValue}».`,
    };
  }

  const categoryValue = extractFieldValue(text, ["categoria", "categoría", "category"]);
  if (categoryValue) {
    const category = findByName(
      context.categories.filter((item) => item.isActive !== false),
      categoryValue,
    );
    if (category) {
      return {
        type: "field",
        patch: { field: "categoryId", value: category.id, label: "Categoría" },
        message: `Categoría: ${category.name}`,
      };
    }
    return {
      type: "unknown",
      message: `No encontré la categoría «${categoryValue}».`,
    };
  }

  const personValue = extractFieldValue(text, [
    "persona",
    "person",
    "asignado",
    "asignada",
    "contacto",
  ]);
  if (personValue) {
    const cleared = /^(ninguna|ninguno|sin|vacia|vacio|nadie|null)$/.test(
      normalize(personValue),
    );
    if (cleared) {
      return {
        type: "field",
        patch: { field: "personId", value: null, label: "Persona" },
        message: "Persona: sin asignar",
      };
    }
    const person = findByName(
      (context.people ?? []).filter((item) => item.isActive !== false),
      personValue,
    );
    if (person) {
      return {
        type: "field",
        patch: { field: "personId", value: person.id, label: "Persona" },
        message: `Persona: ${person.name}`,
      };
    }
    return {
      type: "unknown",
      message: `No encontré la persona «${personValue}».`,
    };
  }

  const projectValue = extractFieldValue(text, [
    "proyecto",
    "project",
    "iniciativa",
  ]);
  if (projectValue) {
    const cleared = /^(ninguno|ninguna|sin|vacio|vacia|null)$/.test(
      normalize(projectValue),
    );
    if (cleared) {
      return {
        type: "field",
        patch: { field: "projectId", value: null, label: "Proyecto" },
        message: "Proyecto: sin asignar",
      };
    }
    const project = findByName(
      (context.projects ?? []).filter((item) => item.isActive !== false),
      projectValue,
    );
    if (project) {
      return {
        type: "field",
        patch: { field: "projectId", value: project.id, label: "Proyecto" },
        message: `Proyecto: ${project.name}`,
      };
    }
    return {
      type: "unknown",
      message: `No encontré el proyecto «${projectValue}».`,
    };
  }

  const startValue = extractFieldValue(text, [
    "hora inicio",
    "hora de inicio",
    "inicio",
    "desde",
    "empieza",
  ]);
  if (startValue) {
    const parsed = parseTime(startValue);
    if (parsed) {
      return {
        type: "field",
        patch: { field: "startTime", value: parsed, label: "Hora inicio" },
        message: `Inicio: ${parsed}`,
      };
    }
    return {
      type: "unknown",
      message: `No entendí la hora de inicio «${startValue}». Ejemplo: inicio 9:00`,
    };
  }

  const endValue = extractFieldValue(text, [
    "hora fin",
    "hora final",
    "hora de fin",
    "fin",
    "hasta",
    "termina",
  ]);
  if (endValue) {
    const parsed = parseTime(endValue);
    if (parsed) {
      return {
        type: "field",
        patch: { field: "endTime", value: parsed, label: "Hora fin" },
        message: `Fin: ${parsed}`,
      };
    }
    return {
      type: "unknown",
      message: `No entendí la hora final «${endValue}». Ejemplo: fin 10:30`,
    };
  }

  const looksLikeLabeledField =
    /^(titulo|titula|llamada|titulada|title|contenido|texto|cuerpo|descripcion|fecha|dia|estado|categoria|category|persona|person|proyecto|project|inicio|fin|desde|hasta|hora)\b/.test(
      text,
    );

  if (!looksLikeLabeledField && text.length >= 2) {
    const display = restoreCasing(rawText, text);
    if (context.kind === "task") {
      return {
        type: "field",
        patch: { field: "title", value: display, label: "Título" },
        message: `Título: ${display}`,
      };
    }
    return {
      type: "field",
      patch: { field: "content", value: display, label: "Contenido" },
      message: `Contenido: ${display}`,
    };
  }

  return {
    type: "unknown",
    message:
      context.kind === "task"
        ? "Di «título …», «persona …», «proyecto …», «categoría …», «estado …», «fecha …» o «guardar»."
        : "Di «contenido …», «persona …», «proyecto …», «título …», «categoría …» o «guardar».",
  };
}

export type VoiceFormDraft = {
  id?: number;
  title?: string;
  content?: string;
  workDate?: string;
  startTime?: string | null;
  endTime?: string | null;
  statusName?: string;
  categoryName?: string;
  personName?: string;
  projectName?: string;
  statusId?: number;
  categoryId?: number;
  personId?: number | null;
  projectId?: number | null;
  parentTaskId?: number | null;
  parentNoteId?: number | null;
  sortOrder?: number;
  durationMinutes?: number;
  isCompleted?: boolean;
  name?: string;
  description?: string;
  color?: string;
  isFinal?: boolean;
  isActive?: boolean;
  itemType?: "task" | "note";
  serviceName?: string;
  username?: string;
  password?: string;
  url?: string;
  notes?: string;
  tags?: string;
  accountId?: number;
  catalogKind?:
    | "companies"
    | "positions"
    | "locations"
    | "fields"
    | "technologies"
    | "application-statuses";
  companyName?: string;
  companyId?: number | null;
  positionName?: string;
  positionId?: number | null;
  locationName?: string;
  locationId?: number | null;
  fieldName?: string;
  fieldId?: number | null;
  startDate?: string;
  endDate?: string;
  isCurrent?: boolean;
  summary?: string;
  achievements?: string;
  technologies?: string;
  technologyIds?: number[];
  appliedAt?: string;
  contact?: string;
  workExperienceId?: number | null;
};
