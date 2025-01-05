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

        public enum NotificationType
        {
            TaskAssigned,
            TaskUpdated,
            ProjectDeadline,
            Mention,
            TeamUpdate,
            ProjectUpdate,
            System
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
        public enum DocumentPermissionLevel
        {
            View = 0,
            Download = 1,
            Edit = 2,
            Owner = 3
        }
    }
}
