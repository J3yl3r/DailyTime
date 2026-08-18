export function formatDurationMinutes(minutes: number): string {
    if (minutes < 60) return `${minutes} min`;
    const hours = Math.floor(minutes / 60);
    const mins = minutes % 60;
    return mins > 0 ? `${hours}h ${mins}m` : `${hours}h`;
  }
  
  export function sumDurationMinutes(items: { durationMinutes: number }[]): number {
    return items.reduce((acc, item) => acc + item.durationMinutes, 0);
  }