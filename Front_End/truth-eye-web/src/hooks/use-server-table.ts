"use client";

import { useCallback, useMemo, useState } from "react";
import type { ServerMode } from "@/components/ui/data-table";

export function useServerTable(initialSize = 10) {
  const [page, setPage] = useState(1);
  const [pageSize, setPageSizeState] = useState(initialSize);
  const [search, setSearchState] = useState("");
  const [sort, setSort] = useState<{ key: string; dir: "asc" | "desc" } | null>(null);
  const [filters, setFilters] = useState<Record<string, string>>({});

  const onSearch = useCallback((s: string) => { setPage(1); setSearchState(s); }, []);
  const onPageSize = useCallback((n: number) => { setPage(1); setPageSizeState(n); }, []);
  const onSort = useCallback((key: string) => {
    setPage(1);
    setSort((s) => (s?.key !== key ? { key, dir: "asc" } : s.dir === "asc" ? { key, dir: "desc" } : null));
  }, []);
  const setFilter = useCallback((k: string, v: string) => {
    setPage(1);
    setFilters((f) => ({ ...f, [k]: v }));
  }, []);

  const params = useMemo(() => {
    const p: Record<string, string | number | undefined> = {
      page,
      pageSize,
      search: search || undefined,
      sort: sort?.key,
      dir: sort?.dir,
    };
    for (const [k, v] of Object.entries(filters)) if (v && v !== "all") p[k] = v;
    return p;
  }, [page, pageSize, search, sort, filters]);

  const bind = (total: number, fetching: boolean): ServerMode => ({
    total, page, pageSize, search, sort, fetching,
    onPage: setPage, onPageSize, onSearch, onSort,
  });

  return { params, filters, setFilter, bind };
}