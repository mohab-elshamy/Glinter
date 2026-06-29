import { API_ORIGIN } from "@/shared/lib/api-client";

export const systemApi = {
  checkLiveness: async (): Promise<string> => {
    const response = await fetch(`${API_ORIGIN}/health/live`);

    if (!response.ok) {
      throw new Error(`Backend health check failed with HTTP ${response.status}`);
    }

    return response.text();
  },
};
