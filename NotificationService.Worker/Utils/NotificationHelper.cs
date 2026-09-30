namespace NotificationService.Worker.Utils
{
    public static class NotificationHelper
    {
        public static async Task<(bool Successful, string ErrorMessage, string fileContent)> RetrieveTemplateAsync(string templateName, string webRootPath)
        {
            string fileContent = string.Empty;
            string path = Path.Combine(webRootPath, "Templates", templateName);
            if (!File.Exists(path))
            {
                return (false, "Template not found.", fileContent);
            }

            try
            {
                fileContent = await File.ReadAllTextAsync(path);
                return (true, null, fileContent);
            }
            catch (IOException ex)
            {
                return (false, "An error occurred while reading the template: " + ex.Message, fileContent);
            }
        }
    }
}
