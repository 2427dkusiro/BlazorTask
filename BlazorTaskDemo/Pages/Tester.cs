using System.Text;

namespace BlazorTaskDemo.Pages;

public class Tester
{
    public List<TestCase> TestResults { get; } = new();

    public List<string> Headers { get; } = new();

    public TestConditionAccesser CreateNewCondition(string name)
    {
        if (Headers.Contains(name))
        {
            throw new InvalidOperationException("cannnot create a condition which already exists.");
        }

        Headers.Add(name);
        var index = Headers.Count - 1;
        TestConditionAccesser testConditionAccesser = new(this, index);
        return testConditionAccesser;
    }

    public string ExportAsMarkDown()
    {
        IEnumerable<string>? headerQuery = (new[] { "テスト名" }).Concat(Headers);
        var headerLine = $"|{string.Join('|', headerQuery)}|";
        var defineLine = $"|{string.Join('|', string.Join('|', Enumerable.Repeat(":---:", Headers.Count + 1)))}|";
        IEnumerable<string>? bodyLines = TestResults.Select(x =>
        {
            return $"|{x.Name}|{string.Join('|', x.Results.Select(time => time.TotalMilliseconds.ToString("F1") + "ms"))}|";
        });

        StringBuilder stringBuilder = new();
        stringBuilder.AppendLine(headerLine);
        stringBuilder.AppendLine(defineLine);
        stringBuilder.AppendJoin(Environment.NewLine, bodyLines);
        return stringBuilder.ToString();
    }

    public class TestConditionAccesser
    {
        private readonly Tester testResult;
        private readonly int index;
        public TestConditionAccesser(Tester testResult, int index)
        {
            this.testResult = testResult;
            this.index = index;
        }

        public TimeSpan this[string name]
        {
            get => GetResult(name);
            set => AddResult(name, value);
        }

        public void AddResult(string testCaseName, TimeSpan time)
        {
            TestCase? testCase = testResult.TestResults.FirstOrDefault(x => x.Name == testCaseName);
            if (testCase is null)
            {
                testCase = new TestCase(testCaseName);
                testResult.TestResults.Add(testCase);
            }
            if (testCase.Results.Count <= index)
            {
                var dif = index - (testCase.Results.Count - 1);
                testCase.Results.AddRange(Enumerable.Range(0, dif).Select(x => TimeSpan.Zero));
            }
            testCase.Results[index] = time;
        }

        public TimeSpan GetResult(string testCaseName)
        {
            TestCase? testCase = testResult.TestResults.FirstOrDefault(x => x.Name == testCaseName) ?? throw new ArgumentException("Case not found.", nameof(testCaseName));
            if (testCase.Results.Count <= index)
            {
                return TimeSpan.Zero;
            }
            return testCase.Results[index];
        }
    }
}


public class TestCase
{
    public TestCase(string name)
    {
        Name = name;
    }

    public string Name { get; }

    public List<TimeSpan> Results { get; } = new();
}