import { z } from "zod";

import { pagedResult } from "@/shared/lib/schemas";

// Tool outputs this view reads, as JSON (camelCase). They mirror the C# records and are checked at runtime, so a
// renamed or removed property shows a clear error instead of a broken view. Extra properties are ignored.
//   show_projects_dashboard → ProjectsSummary        (Models/Responses/ProjectDto.cs)
//   list_projects           → PagedResult<ProjectDto> (Models/Responses/PagedResult.cs)

export const ownerProjectCountSchema = z.object({
  ownerUserId: z.number().int(),
  ownerName: z.string(),
  projects: z.number().int(),
});

export const projectsSummarySchema = z.object({
  totalProjects: z.number().int(),
  owners: z.number().int(),
  byOwner: z.array(ownerProjectCountSchema),
});

export const projectSchema = z.object({
  id: z.number().int(),
  name: z.string(),
  description: z.string(),
  ownerUserId: z.number().int(),
  priority: z.string(),
  status: z.string(),
});

export const projectPageSchema = pagedResult(projectSchema);

export type OwnerProjectCount = z.infer<typeof ownerProjectCountSchema>;
export type ProjectsSummary = z.infer<typeof projectsSummarySchema>;
export type Project = z.infer<typeof projectSchema>;
export type ProjectPage = z.infer<typeof projectPageSchema>;
