namespace SyncSpaceBackend.Enums
{
    public class Enum
    {
        public enum TaskStatus
        {
            Todo,
            InProgress,
            UnderReview,
            Completed
        }

        public enum TaskPriority
        {
            Low,
            Medium,
            High,
            Urgent
        }

        public enum MilestoneStatus
        {
            Pending,
            InProgress,
            Completed,
            Delayed
        }

        public enum ProjectRole
        {
            Admin,
            Manager,
            Member,
            Viewer
        }

        public enum ProjectStatus
        {
            Planning,
            Active,
            OnHold,
            Completed,
            Cancelled
        }
    }
}
