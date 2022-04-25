using System;

namespace DurableFunctionsDemo
{
    public class Result
    {
        public Status BuildResult { get; set; } = Status.None;
        public TimeSpan BuildDuration { get; set; }
        public int TestsPassed { get; set; } = 0;
        public Artifact[] Artifacts { get; set; } = Array.Empty<Artifact>();
        public DateTimeOffset DeployTime { get; set; }
    }

    public class Artifact
    {
        public string Name { get; set; }

        public Artifact(string name)
            => Name = name;
    }

    public enum Status
    {
        None,
        Success,
        Fail
    }
}
