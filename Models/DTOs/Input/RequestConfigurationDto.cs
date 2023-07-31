namespace Models.DTOs.Input
{
    public class RequestConfigurationDto
    {
        public int[]? Fields { get; set; }
        public int[]? Statuses { get; set; }
        public int[]? Destinations { get; set; }
    }
}