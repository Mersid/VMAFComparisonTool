namespace VMAFComparisonTool;

public class Application
{
    private event Action? EventTestA;

    public void Run(Options options)
    {
        Console.WriteLine("Start");
        Task wait5 = new Task(() => Thread.Sleep(5000));
        wait5.Start();

        new Task(() =>
        {
            EventTestA += EventTest;
            Thread.Sleep(2000);
            EventTestA?.Invoke();
        }).Start();

        wait5.Wait();
        Console.WriteLine("End");
    }

    private void EventTest()
    {
        Console.WriteLine("EventTest");
    }
}