namespace Confab.Emails
{
    public class ModQueueReminderScheduleData
    {
        public List<ModQueueReminderScheduleDataItem> Data;

        public ModQueueReminderScheduleData(List<int> sendHours)
        {
            Data = new List<ModQueueReminderScheduleDataItem>();

            foreach (int i in sendHours)
            {
                Data.Add(new ModQueueReminderScheduleDataItem
                {
                    Hours = i,
                });
            }
        }

        public void Reset()
        {
            foreach (ModQueueReminderScheduleDataItem item in Data)
            {
                item.Sent = false;
            }
        }
    }
}
