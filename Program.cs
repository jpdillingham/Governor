using Governor;

var tokenBucket = new TokenBucket2(12000, 1000);

var worker1 = new Worker("first", () => tokenBucket.WaitAsync(100));
var worker2 = new Worker("second", () => tokenBucket.WaitAsync(100));
var worker3 = new Worker("third", () => tokenBucket.WaitAsync(100));
var worker4 = new Worker("fourth", () => tokenBucket.WaitAsync(100));

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
    public Worker(string name, Func<Task> governor)
    {
        Name = name;
        Governor = governor;

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

    public async void Start()
    {
        Started = DateTime.Now;

        while (true)
        {
            await Governor();
            Count++;
        }
    }
}

public class TokenBucket2
{
    public TokenBucket2(int count, int interval)
    {
        Count = count;
        CurrentCount = Count;

        Clock = new System.Timers.Timer(interval);
        Clock.Elapsed += (sender, e) =>
        {
            CurrentCount = Count;
            AutoReset?.Set();
            Report?.Invoke();
        };

        Clock.Start();
    }

    private int Count { get; set; }
    private int CurrentCount { get; set; }
    public Action Report { get; set; }
    private System.Timers.Timer Clock { get; set; }
    private AsyncAutoResetEvent AutoReset { get; set; } = new AsyncAutoResetEvent();
    private SemaphoreSlim SyncRoot { get; set; } = new SemaphoreSlim(1, 1);

    public async Task WaitAsync(int count)
    {
        if (count > Count)
        {
            throw new ArgumentException("Requested count exceeds max; this will deadlock");
        }

        await SyncRoot.WaitAsync();

        try
        {
            if (CurrentCount >= count)
            {
                CurrentCount -= count;
                return;
            }

            //Console.WriteLine($"Not enough tokens; requested: {count}, available {CurrentCount}");
            await AutoReset.WaitAsync();
        }
        finally
        {
            SyncRoot.Release();
        }
    }
}