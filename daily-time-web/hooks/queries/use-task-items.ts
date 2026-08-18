"use client";

import { useQuery } from "@tanstack/react-query";
import { getTaskItemsByDateRange, getTaskItemChildren } from "@/lib/api/task-items";
import { taskItemKeys } from "@/lib/query/keys";

export function useTaskItemsByDate(fromDate: string, toDate = fromDate) {
  return useQuery({
    queryKey: taskItemKeys.byDate(fromDate, toDate),
    queryFn: () => getTaskItemsByDateRange(fromDate, toDate),
    enabled: !!fromDate && !!toDate,
  });
}

export function useTaskItemChildren(parentId: number, enabled = true) {
  return useQuery({
    queryKey: taskItemKeys.children(parentId),
    queryFn: () => getTaskItemChildren(parentId),
    enabled: enabled && parentId > 0,
  });
}