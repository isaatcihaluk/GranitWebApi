namespace GranitWebApi.Workflow.Enums
{
    public enum WorkflowStepType
    {
        Sequential = 0,   // Sırayla herkes
        Parallel = 1,     // Aynı anda herkes
        Any = 2           // İçlerinden biri yeterli
    }
}