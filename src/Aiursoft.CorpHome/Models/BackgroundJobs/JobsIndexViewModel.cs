using Aiursoft.UiStack.Layout;

namespace Aiursoft.CorpHome.Models.BackgroundJobs;

public class JobsIndexViewModel : UiStackLayoutViewModel
{
    public IEnumerable<JobInfo> AllRecentJobs { get; init; } = [];
}
