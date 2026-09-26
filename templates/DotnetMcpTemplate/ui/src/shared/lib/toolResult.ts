import { z } from "zod";

/** The parts of a CallToolResult a view reads. */
export type ToolResult = {
  content?: Array<{ type: string; text?: string }>;
  structuredContent?: Record<string, unknown>;
  isError?: boolean;
};

/** Text of a tool result: the server's message when isError is set (validation, auth, AppException). */
export function textOf(result: ToolResult): string {
  return (result.content ?? [])
    .map((block) => (block.type === "text" ? (block.text ?? "") : ""))
    .join(" ")
    .trim();
}

/**
 * The typed output of a tool result, validated against the view's schema. Throws with a readable message when the
 * call failed or the shape doesn't match, e.g. after a C# record was renamed without updating the view's schema.
 */
export function outputOf<S extends z.ZodType>(tool: string, result: ToolResult, schema: S): z.infer<S> {
  if (result.isError) throw new Error(textOf(result) || `${tool} failed.`);

  let value: unknown = result.structuredContent;
  if (value === undefined) {
    // Tools without UseStructuredContent return their JSON as text.
    try {
      value = JSON.parse(textOf(result));
    } catch {
      throw new Error(`${tool} returned no structured content. Set UseStructuredContent = true on the tool.`);
    }
  }

  const parsed = schema.safeParse(value);
  if (!parsed.success) {
    throw new Error(`${tool} returned data this view doesn't understand (check its schema.ts):\n${describe(parsed.error)}`);
  }
  return parsed.data;
}

/** The first few problems, one per line: "byOwner.0.ownerName: expected string, received undefined". */
function describe(error: z.ZodError, max = 3): string {
  const lines = error.issues.slice(0, max).map((issue) => `${issue.path.join(".") || "(root)"}: ${issue.message}`);
  const more = error.issues.length - max;
  return more > 0 ? `${lines.join("\n")}\n…and ${more} more` : lines.join("\n");
}

export const messageOf = (error: unknown, fallback = "Something went wrong. Please try again."): string =>
  error instanceof Error && error.message ? error.message : fallback;
