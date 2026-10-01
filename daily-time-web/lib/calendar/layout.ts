/**
 * Reparto en columnas de los bloques que se solapan dentro de un día, como hace cualquier
 * calendario: los que coinciden en el tiempo se ponen lado a lado en vez de uno encima de otro.
 */

export type ScheduledEntry<T> = {
  item: T;
  /** Minutos desde medianoche. */
  startMinutes: number;
  endMinutes: number;
};

export type PositionedEntry<T> = ScheduledEntry<T> & {
  /** Fracción 0–1 del ancho de la columna del día. */
  left: number;
  width: number;
  /** Cuántos bloques comparten la franja; sirve para decidir cuánto texto cabe. */
  columnCount: number;
};

/**
 * Agrupa los bloques en racimos de solapamiento y, dentro de cada racimo, los reparte en
 * columnas. Dos bloques que solo se tocan (uno acaba a las 19:00 y el otro empieza a las
 * 19:00) no se consideran solapados: van en la misma columna, uno debajo del otro.
 */
export function layoutOverlapping<T>(entries: ScheduledEntry<T>[]): PositionedEntry<T>[] {
  // Primero por hora de inicio y, a igualdad, el más largo delante: así el bloque que
  // abarca toda la tarde queda a la izquierda y los cortos se ordenan a su derecha.
  const ordenados = [...entries].sort(
    (a, b) =>
      a.startMinutes - b.startMinutes ||
      b.endMinutes - b.startMinutes - (a.endMinutes - a.startMinutes),
  );

  const resultado: PositionedEntry<T>[] = [];
  // Un racimo es un grupo de bloques encadenados por solapamiento: todos comparten el
  // mismo número de columnas, para que el ancho no cambie a mitad del grupo.
  let racimo: { entry: ScheduledEntry<T>; columna: number }[] = [];
  let columnas: number[] = []; // fin del último bloque de cada columna
  let finDelRacimo = -Infinity;

  const cerrarRacimo = () => {
    const total = columnas.length || 1;
    for (const { entry, columna } of racimo) {
      resultado.push({
        ...entry,
        left: columna / total,
        width: 1 / total,
        columnCount: total,
      });
    }
    racimo = [];
    columnas = [];
    finDelRacimo = -Infinity;
  };

  for (const entry of ordenados) {
    if (entry.startMinutes >= finDelRacimo) {
      cerrarRacimo();
    }

    // Primera columna libre a esa hora; si no hay, se abre una nueva.
    let columna = columnas.findIndex((fin) => fin <= entry.startMinutes);
    if (columna === -1) {
      columna = columnas.length;
      columnas.push(entry.endMinutes);
    } else {
      columnas[columna] = entry.endMinutes;
    }

    racimo.push({ entry, columna });
    finDelRacimo = Math.max(finDelRacimo, entry.endMinutes);
  }

  cerrarRacimo();
  return resultado;
}
