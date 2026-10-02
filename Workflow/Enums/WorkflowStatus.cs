namespace GranitWebApi.Workflow.Enums
{
    public enum WorkflowStatus
    {
        Taslak = 0,
        Onayda = 1,
        Reddedildi = 2,
        Revize = 3,
        Iptal = 4,
        Tamamlandi = 5
    }

    public enum WorkflowApprovalStatus
    {
        Onayda = 0,
        Onaylandi = 1,
        Reddedildi = 2,
        Revize = 3,
        Iptal = 4
    }
}