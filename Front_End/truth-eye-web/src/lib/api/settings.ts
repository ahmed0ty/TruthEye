import { api } from "./client";

export interface ProctoringSettings {
  noFaceSeconds: number;
  multipleFacesSeconds: number;
}

export const settingsApi = {
  proctoring: () => api<ProctoringSettings>("/api/Settings/proctoring"),
};