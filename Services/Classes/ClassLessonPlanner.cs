using EduBridge.Contracts.Classes;

namespace EduBridge.Services.Classes;

public interface IClassLessonPlanner
{
    IReadOnlyList<PlannedClassLesson> Build(
        DateOnly startDate,
        int totalSessions,
        IReadOnlyCollection<ClassScheduleRequest> schedules);
}

public sealed class ClassLessonPlanner : IClassLessonPlanner
{
    public IReadOnlyList<PlannedClassLesson> Build(
        DateOnly startDate,
        int totalSessions,
        IReadOnlyCollection<ClassScheduleRequest> schedules)
    {
        var schedulesByDay = schedules
            .GroupBy(schedule => schedule.DayOfWeek)
            .ToDictionary(
                group => group.Key,
                group => group.OrderBy(schedule => schedule.StartTime).ToList());

        var lessons = new List<PlannedClassLesson>(totalSessions);
        var currentDate = startDate;

        while (lessons.Count < totalSessions)
        {
            var dayOfWeek = MapDayOfWeek(currentDate.DayOfWeek);

            if (schedulesByDay.TryGetValue(dayOfWeek, out var daySchedules))
            {
                foreach (var schedule in daySchedules)
                {
                    lessons.Add(new PlannedClassLesson(
                        currentDate,
                        schedule.DayOfWeek,
                        schedule.StartTime!.Value,
                        schedule.EndTime!.Value));

                    if (lessons.Count == totalSessions)
                    {
                        break;
                    }
                }
            }

            currentDate = currentDate.AddDays(1);
        }

        return lessons;
    }

    private static byte MapDayOfWeek(DayOfWeek day) =>
        day == DayOfWeek.Sunday ? (byte)7 : (byte)day;
}

public sealed record PlannedClassLesson(
    DateOnly LessonDate,
    byte DayOfWeek,
    TimeOnly StartTime,
    TimeOnly EndTime);
