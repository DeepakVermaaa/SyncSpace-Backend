namespace SyncSpaceBackend.Enums
{
    public class Enum
    {
        public enum TaskStatusEnum
        {
            Todo = 0,
            InProgress = 1,
            UnderReview = 2,
            Completed = 3
        }

        public enum TaskPriorityEnum
        {
            Low = 0,
            Medium = 1,
            High = 2,
            Urgent = 3
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
