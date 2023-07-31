namespace Models.DTOs.Input
{
    public class RequestFiltersDto
    {
        public int[]? Statuses { get; set; }
        public int[]? Destinations { get; set; }
    }
}