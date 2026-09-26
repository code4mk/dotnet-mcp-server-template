import { z } from "zod";

// Schemas for response records shared by many tools. App-specific ones live in apps/<entry>/schema.ts.

/** PagedResult&lt;T&gt; (Models/Responses/PagedResult.cs). */
export const pagedResult = <T extends z.ZodType>(item: T) =>
  z.object({
    items: z.array(item),
    page: z.number().int(),
    pageSize: z.number().int(),
    totalCount: z.number().int(),
    totalPages: z.number().int(),
    hasNextPage: z.boolean(),
  });
