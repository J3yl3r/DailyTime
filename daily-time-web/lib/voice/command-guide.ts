export type VoiceCommandGuide = {
  id: string;
  title: string;
  description: string;
  examples: string[];
  voiceFields?: string[];
};

/** Catálogo visual de comandos soportados por daily-time-voice. */
export const VOICE_COMMAND_GUIDE: VoiceCommandGuide[] = [
  {
    id: "voice_toggle",
    title: "Activar la voz",
    description:
      "Pulsa el icono de ondas (abajo a la derecha). Quedará resaltado en azul mientras escucha. Vuelve a pulsar para desactivar.",
    examples: [],
  },
  {
    id: "create_task",
    title: "Nueva tarea (paso a paso)",
    description:
      "Di «nueva tarea» para abrir el formulario. Dicta un campo por frase y al terminar di «guardar».",
    voiceFields: [
      "título …",
      "fecha hoy | mañana | ayer",
      "categoría …",
      "estado …",
      "persona … | persona ninguna",
      "proyecto … | proyecto ninguno",
      "inicio 9:00 | fin 10:30",
      "guardar | cancelar",
    ],
    examples: [
      "nueva tarea",
      "título revisar informe",
      "persona Juan",
      "proyecto rediseño",
      "guardar",
    ],
  },
  {
    id: "create_note",
    title: "Nueva nota (paso a paso)",
    description:
      "Di «nueva nota» o «crea una nota que diga …». Completa por voz y di «guardar».",
    voiceFields: [
      "contenido …",
      "título …",
      "categoría …",
      "estado …",
      "persona …",
      "proyecto …",
      "guardar | cancelar",
    ],
    examples: ["nueva nota", "contenido comprar leche", "guardar"],
  },
  {
    id: "edit",
    title: "Editar",
    description:
      "Abre el formulario de un ítem existente con sus datos. Luego dicta cambios y di «guardar».",
    examples: [
      "edita la tarea revisar informe",
      "edita la nota comprar leche",
      "edita la persona Juan",
      "edita el proyecto rediseño",
      "edita la contraseña Gmail",
      "edita la empresa Google",
      "edita la experiencia Microsoft",
      "edita la postulación Google",
      "edita el servicio GitHub",
    ],
  },
  {
    id: "delete",
    title: "Eliminar",
    description: "Borra un ítem por nombre.",
    examples: [
      "elimina la tarea revisar informe",
      "borra la nota comprar leche",
      "elimina la persona Juan",
      "elimina el proyecto rediseño",
      "elimina la empresa Google",
      "elimina la experiencia Microsoft",
      "elimina la postulación Google",
      "elimina el servicio GitHub",
    ],
  },
  {
    id: "create_person",
    title: "Nueva persona",
    description: "Abre el formulario de persona y dicta los campos.",
    voiceFields: ["nombre …", "descripción …", "activa | inactiva", "guardar"],
    examples: ["nueva persona", "nombre Juan Pérez", "guardar"],
  },
  {
    id: "create_project",
    title: "Nuevo proyecto",
    description: "Abre el formulario de proyecto y dicta los campos.",
    voiceFields: ["nombre …", "descripción …", "guardar"],
    examples: ["nuevo proyecto", "nombre rediseño web", "guardar"],
  },
  {
    id: "create_status",
    title: "Nuevo estado",
    description: "Abre el formulario de estado. Indica tipo (tarea/nota) y color.",
    voiceFields: [
      "nombre …",
      "descripción …",
      "tipo tarea | tipo nota",
      "color rojo | color #3B82F6",
      "final sí | final no",
      "guardar",
    ],
    examples: ["nuevo estado", "nombre en progreso", "tipo tarea", "color azul", "guardar"],
  },
  {
    id: "create_category",
    title: "Nueva categoría",
    description: "Abre el formulario de categoría.",
    voiceFields: ["nombre …", "descripción …", "tipo tarea | tipo nota", "guardar"],
    examples: ["nueva categoría", "nombre trabajo", "tipo nota", "guardar"],
  },
  {
    id: "career",
    title: "Carrera",
    description:
      "Gestiona experiencias, postulaciones y datos reutilizables (empresas, cargos, ubicaciones, tecnologías, estados).",
    voiceFields: [
      "catálogo: nombre …, descripción …",
      "experiencia: empresa …, cargo …, inicio …, tecnologías …",
      "postulación: empresa …, cargo …, estado …, fecha …",
      "guardar",
    ],
    examples: [
      "nueva empresa Google",
      "nuevo cargo Desarrollador",
      "nueva ubicación Remoto",
      "nueva carrera Desarrollo",
      "nueva tecnología React",
      "nuevo estado de postulación",
      "nueva experiencia Microsoft",
      "nueva postulación Google",
      "lista experiencias",
      "lista postulaciones",
      "abre datos reutilizables",
      "abre experiencias",
      "abre postulaciones",
    ],
  },
  {
    id: "vault",
    title: "Bóveda",
    description:
      "Crea, edita o elimina cuentas, servicios y contraseñas. Si no hay cuenta, «nueva contraseña» pedirá crear una primero.",
    voiceFields: [
      "cuenta: nombre …",
      "servicio: nombre …, url …, notas …",
      "contraseña: servicio …, usuario …, contraseña …, url …, etiquetas …",
      "guardar",
    ],
    examples: [
      "nueva cuenta de bóveda",
      "nuevo servicio de bóveda",
      "nueva contraseña",
      "edita la contraseña Gmail",
      "elimina la cuenta principal",
      "abre servicios de la bóveda",
      "abre la bóveda",
    ],
  },
  {
    id: "list",
    title: "Listar",
    description: "Consulta listados por voz (aparecen en un aviso).",
    examples: [
      "muestra las tareas de hoy",
      "lista las notas",
      "lista las personas",
      "muestra los proyectos",
      "lista estados",
      "muestra categorías",
      "lista experiencias",
      "lista postulaciones",
    ],
  },
  {
    id: "complete",
    title: "Completar",
    description: "Marca una tarea o nota como finalizada.",
    examples: [
      "completa la tarea revisar informe",
      "completa la nota comprar leche",
    ],
  },
  {
    id: "add_time",
    title: "Registrar tiempo",
    description: "Registra minutos u horas en una tarea, una nota, o suelto.",
    examples: [
      "registra 30 minutos en la tarea revisar informe",
      "registra 15 minutos en la nota reunión",
      "agrega 1 hora",
    ],
  },
  {
    id: "workspace_filter",
    title: "Informe de tiempo",
    description: "Abre el informe y filtra por persona o proyecto.",
    examples: [
      "abre el informe",
      "filtra por persona Juan",
      "informe de proyecto rediseño",
      "quita el filtro",
    ],
  },
  {
    id: "calendar",
    title: "Calendario",
    description: "Navega periodos y cambia la vista.",
    examples: [
      "abre el calendario",
      "ve a hoy",
      "semana anterior",
      "mes siguiente",
      "vista semana",
      "vista mes",
    ],
  },
  {
    id: "navigate",
    title: "Navegar",
    description:
      "Cambia de pantalla: tablero, calendario, informe, catálogos, carrera, bóveda…",
    examples: [
      "abre el informe",
      "abre el tablero",
      "abre personas",
      "abre proyectos",
      "abre la bóveda",
      "abre servicios",
      "abre estados",
      "abre categorías",
      "abre experiencias",
      "abre postulaciones",
      "abre datos reutilizables",
    ],
  },
  {
    id: "help",
    title: "Ayuda por voz",
    description: "Abre este manual. Di «cerrar ayuda» para ocultarlo.",
    examples: ["ayuda", "manual", "qué puedes hacer", "comandos"],
  },
];

export const VOICE_GUIDE_INTRO =
  "Activa el micrófono (icono de ondas), habla con claridad y espera a que procese cada frase antes de continuar.";
