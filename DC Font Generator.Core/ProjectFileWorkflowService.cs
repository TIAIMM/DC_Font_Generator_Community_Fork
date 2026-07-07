namespace DC_Font_Generator
{
    internal static class ProjectFileWorkflowService
    {
        public static void Save(string selectedPath, ProjectSaveRequest request)
        {
            ProjectSerializationService.Save(
                ProjectSerializationService.GetSavePath(selectedPath),
                request);
        }

        public static ProjectDocument Load(string selectedPath)
        {
            return ProjectSerializationService.Load(
                ProjectSerializationService.GetLoadPath(selectedPath));
        }
    }
}
