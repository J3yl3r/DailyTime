import { apiClient } from "@/lib/api/client";
import type { Person } from "@/types/api";
import type { PersonInput } from "@/schemas/person.schema";

export function getPeople(onlyActive?: boolean) {
  const query = onlyActive ? "?onlyActive=true" : "";
  return apiClient.get<Person[]>(`/api/people${query}`);
}

export function createPerson(body: PersonInput) {
  return apiClient.post<Person>("/api/people", body);
}

export function updatePerson(id: number, body: PersonInput) {
  return apiClient.put<Person>(`/api/people/${id}`, body);
}

export function deletePerson(id: number) {
  return apiClient.delete<object>(`/api/people/${id}`);
}
