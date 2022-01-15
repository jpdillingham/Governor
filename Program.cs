using Governor;

var tokenBucket = new TokenBucket(12000, 1000);

var worker1 = new Worker("first", 500, () => tokenBucket.WaitAsync(1));
var worker2 = new Worker("second", 1000, () => tokenBucket.WaitAsync(1));
var worker3 = new Worker("third", 10000, () => tokenBucket.WaitAsync(1));
var worker4 = new Worker("fourth", 50000, () => tokenBucket.WaitAsync(1));

tokenBucket.Report = () =>
{
    Console.WriteLine($"{worker1.Rate}/s \t {worker2.Rate}/s \t {worker3.Rate}/s \t {worker4.Rate}/s \t {worker1.Rate + worker2.Rate + worker3.Rate + worker4.Rate}/s");
};

Task.Run(() => worker1.Start());

Task.Run(async () => {
    //await Task.Delay(5000);
    worker2.Start();
});

Task.Run(async () => {
    //await Task.Delay(15000);
    worker3.Start();
});

Task.Run(() => worker4.Start());

Console.ReadKey();

public class Worker
{
    public Worker(string name, int internalLimit, Func<Task> governor)
    {
        Name = name;
        Governor = governor;

        InternalLimit = internalLimit;
        InternalBucket = new TokenBucket(InternalLimit, 1000);

        Clock = new System.Timers.Timer(1000);
        Clock.Elapsed += (sender, e) =>
        {
            Rate = Count - LastCount / (DateTime.Now - Timestamp).TotalSeconds;
            Count = 0;
            Timestamp = DateTime.Now;
        };

        Clock.Start();
    }
    private System.Timers.Timer Clock { get; set; }
    private string Name { get; init; }
    private Func<Task> Governor { get; init; }
    private DateTime Started { get; set; }
    private DateTime Timestamp { get; set; }
    private long Count { get; set; }
    private long LastCount { get; set; }
    public double Rate { get; set; }

    private int InternalLimit { get; set; }

    private TokenBucket InternalBucket { get; set; }

    public async void Start()
    {
        Started = DateTime.Now;

        while (true)
        {
            await Governor();
            await InternalBucket.WaitAsync(1);
            Count++;
        }
    }
}