namespace OmniPort.Core.Models
{
    public class ImportProfile
    {
        public int Id { get; set; }
        public string ProfileName { get; set; } = string.Empty;
        public ImportTemplate Template { get; set; } = new();
        public List<FieldMapping> Mappings { get; set; } = new();
    }
}
