using Governor;

var tokenBucket = new TokenBucket(1, 1000);

var worker1 = new Worker("1", 500, () => tokenBucket.GetAsync(1, "1"));
var worker2 = new Worker("2", 1000, () => tokenBucket.GetAsync(1, "2"));
var worker3 = new Worker("3", 10000, () => tokenBucket.GetAsync(1, "3"));
var worker4 = new Worker("4", 50000, () => tokenBucket.GetAsync(1, "4"));
//var worker5 = new Worker("fifth", 500, () => tokenBucket.GetAsync(1));
//var worker6 = new Worker("sixth", 1000, () => tokenBucket.GetAsync(1));
//var worker7 = new Worker("seventh", 10000, () => tokenBucket.GetAsync(1));
//var worker8 = new Worker("eigth", 50000, () => tokenBucket.GetAsync(1));

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
//Task.Run(() => worker5.Start());
//Task.Run(() => worker6.Start());
//Task.Run(() => worker7.Start());
//Task.Run(() => worker8.Start());

Console.ReadKey();

public class Worker
{
    public Worker(string name, int internalLimit, Func<Task<int>> governor)
    {
        Name = name;
        Governor = governor;

        //InternalLimit = internalLimit;
        //InternalBucket = new TokenBucket(InternalLimit, 1000);

        //Clock = new System.Timers.Timer(1000);
        //Clock.Elapsed += (sender, e) =>
        //{
        //    Rate = Count - LastCount / (DateTime.Now - Timestamp).TotalSeconds;
        //    Count = 0;
        //    Timestamp = DateTime.Now;
        //};

        //Clock.Start();
    }
    private System.Timers.Timer Clock { get; set; }
    private string Name { get; init; }
    private Func<Task<int>> Governor { get; init; }
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
            var count = await Governor();

            if (count == 0)
            {
                continue;
            }

            Console.WriteLine($"Got bytes {Name}");
            //await InternalBucket.GetAsync(1);
            Count++;
        }
    }
}