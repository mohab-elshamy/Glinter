export type LoadState =
  | { status: "loading" }
  | { status: "ready" }
  | { status: "error"; message: string };
