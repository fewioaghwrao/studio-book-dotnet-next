export function apiFetch(url: string, options: RequestInit = {}): Promise<Response> {
  const token = typeof window !== "undefined"
    ? localStorage.getItem("token") ?? ""
    : "";

  const { headers: optionHeaders, ...rest } = options;

  return fetch(url, {
    ...rest,
    headers: {
      "Content-Type": "application/json",
      ...(optionHeaders as Record<string, string>),
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
    },
  });
}