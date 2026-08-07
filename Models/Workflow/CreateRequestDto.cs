namespace GranitWebApi.Models.Workflow
{
    public class CreateRequestDto
    {
        public int ProcessTypeId { get; set; }
        public int CreatedBy { get; set; }
        public string Title { get; set; }
        public bool IsDraft { get; set; } 
    }
}
