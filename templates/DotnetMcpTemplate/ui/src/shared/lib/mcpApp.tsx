import { createContext, useCallback, useContext, useEffect, useMemo, useState, type ReactNode } from "react";
import { applyDocumentTheme, useApp, useHostStyles, type App } from "@modelcontextprotocol/ext-apps/react";
import { AlertTriangle, MonitorSmartphone } from "lucide-react";
import type { z } from "zod";

import { StateCard } from "@/shared/components/StateCard";
import { outputOf, type ToolResult } from "@/shared/lib/toolResult";

// One MCP host connection per app, shared through context. Components use:
//   useToolOutput<T>()  the tool's output pushed by the host, validated against the app's schema
//   useCallTool()       call a server tool from the view: (name, args, schema) => typed output
//   useMcpApp()         the raw App for anything else (openLink, sendMessage, updateModelContext); null in the sandbox
//
// Inside a host the first paint waits for the tool result, so sample data never flashes. A failed or cancelled
// tool run shows an error instead of waiting forever. In the dev sandbox (`pnpm dev`, not framed) the app renders
// its sample output at once and `useCallTool` answers from the sample tools, so hooks have one code path.
const isEmbedded = typeof window !== "undefined" && window.parent !== window;

/** Sample output and tool answers for the dev sandbox. Only passed in dev builds (see the entries). */
export type Sandbox<T> = {
  output: T;
  tools?: Record<string, (args: Record<string, unknown>) => unknown>;
};

export type CallTool = <S extends z.ZodType>(name: string, args: Record<string, unknown>, schema: S) => Promise<z.infer<S>>;

type State<T> = { status: "waiting" } | { status: "ready"; data: T } | { status: "error"; message: string };

type McpAppContextValue = { data: unknown; app: App | null; callTool: CallTool };

const McpAppContext = createContext<McpAppContextValue | null>(null);

type McpAppProviderProps<S extends z.ZodType> = {
  /** Reported to the host; use the entry name. */
  name: string;
  /** Tool this view renders; used in messages. */
  tool: string;
  /** Shape of the tool's structured content. Mirrors the C# response record. */
  schema: S;
  /** Dev sandbox data; pass `import.meta.env.DEV ? sandbox : undefined` so it's not shipped. */
  sandbox?: Sandbox<z.infer<S>>;
  /** Shown inside a host until the first tool result arrives. */
  loading?: ReactNode;
  children: ReactNode;
};

export function McpAppProvider<S extends z.ZodType>({ name, tool, schema, sandbox, loading, children }: McpAppProviderProps<S>) {
  const [state, setState] = useState<State<z.infer<S>>>(() => initialState(tool, schema, sandbox));

  const { app } = useApp({
    appInfo: { name, version: "1.0.0" },
    capabilities: {},
    onAppCreated: (instance) => {
      instance.ontoolresult = (result) => {
        try {
          setState({ status: "ready", data: outputOf(tool, result as ToolResult, schema) });
        } catch (error) {
          setState({ status: "error", message: (error as Error).message });
        }
      };
      instance.ontoolcancelled = ({ reason }) =>
        setState({ status: "error", message: `The ${tool} call was cancelled${reason ? `: ${reason}` : "."}` });
    },
  });

  useHostStyles(app, app?.getHostContext()); // host theme, CSS variables, fonts (see styles.css)
  useEffect(() => {
    if (isEmbedded) return; // sandbox: follow the OS theme
    const media = window.matchMedia("(prefers-color-scheme: dark)");
    const apply = () => applyDocumentTheme(media.matches ? "dark" : "light");
    apply();
    media.addEventListener("change", apply);
    return () => media.removeEventListener("change", apply);
  }, []);

  const callTool = useCallback<CallTool>(
    async (toolName, args, outputSchema) => {
      if (app) return outputOf(toolName, (await app.callServerTool({ name: toolName, arguments: args })) as ToolResult, outputSchema);

      const answer = sandbox?.tools?.[toolName];
      if (isEmbedded || !answer) throw new Error(isEmbedded ? "Not connected to the MCP host yet." : `No sandbox answer for ${toolName}.`);
      await new Promise((resolve) => setTimeout(resolve, 300)); // let loading states show
      return outputOf(toolName, { structuredContent: answer(args) as Record<string, unknown> }, outputSchema);
    },
    [app, sandbox],
  );

  const data = state.status === "ready" ? state.data : undefined;
  const value = useMemo<McpAppContextValue>(() => ({ data, app, callTool }), [data, app, callTool]);

  if (state.status === "waiting") return <>{loading ?? <Spinner />}</>;
  if (state.status === "error") {
    const noHost = !isEmbedded && !sandbox; // built bundle opened directly: not an error, just the wrong place
    return (
      <div className="p-4">
        <StateCard
          icon={noHost ? MonitorSmartphone : AlertTriangle}
          title={noHost ? "Open this view in an MCP host" : "This view couldn't load"}
          tone={noHost ? "neutral" : "danger"}
        >
          <p className="whitespace-pre-line">{state.message}</p>
        </StateCard>
      </div>
    );
  }
  return <McpAppContext.Provider value={value}>{children}</McpAppContext.Provider>;
}

function initialState<S extends z.ZodType>(tool: string, schema: S, sandbox?: Sandbox<z.infer<S>>): State<z.infer<S>> {
  if (isEmbedded) return { status: "waiting" };
  if (!sandbox) return { status: "error", message: "It shows a tool result from the host. Run `pnpm dev` in ui/ to preview it with sample data." };
  try {
    return { status: "ready", data: outputOf(tool, { structuredContent: sandbox.output as Record<string, unknown> }, schema) };
  } catch (error) {
    return { status: "error", message: `Sandbox sample data: ${(error as Error).message}` };
  }
}

function Spinner() {
  return (
    <div className="flex min-h-40 items-center justify-center p-10" role="status" aria-label="Loading">
      <div className="size-6 animate-spin rounded-full border-2 border-border-primary border-t-accent" />
    </div>
  );
}

function useMcpAppContext(hook: string): McpAppContextValue {
  const context = useContext(McpAppContext);
  if (!context) throw new Error(`${hook} must be used inside <McpAppProvider>.`);
  return context;
}

/** The latest tool output pushed by the host (validated). A new object means the model ran the tool again. */
export function useToolOutput<T>(): T {
  return useMcpAppContext("useToolOutput").data as T;
}

/** Calls a server tool from the view (through the host, with the user's token) and validates its output. */
export function useCallTool(): CallTool {
  return useMcpAppContext("useCallTool").callTool;
}

/** The host connection for other App features; null in the sandbox and until connected. */
export function useMcpApp(): App | null {
  return useMcpAppContext("useMcpApp").app;
}
