using System.Net.Http;
using System.Threading;

namespace ShigureController
{
    public enum EventType
    {
        In,
        Out,
        Pause
    }
    public enum EventStatus
    {
        Planned,
        Proceessing,
        Done,
        Error,
        ErrorDone
    }
    internal class Event
    {
        private DateTime startTime;
        private DateTime endTime;
        private string hostname;

        public string SyringeType = "UNSET";
        public EventType Type;
        public EventStatus Status;
        public double Volume;// mL
        public double Speed;// mL/min

        public Event(EventType type, double volume, double speed, string hostname, string syringeType)
        {
            Type = type;
            Volume = volume;
            Speed = speed;
            Status = EventStatus.Planned;
            this.hostname = hostname;
            SyringeType = syringeType;
        }
        public async Task Run(string ip)
        {
            int maxRetries = 10;
            int delayBetweenRetries = 20; 

            startTime = DateTime.Now;
            endTime = startTime + TimeSpan.FromMinutes(Volume / Speed + 0.05); // 3 s sleep after each event to be compatible with possible timeout
            Status = EventStatus.Proceessing;
            if (SyringeType == "UNSET")
            {
                throw new Exception("Syringe type not set!");
            }

            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                using (HttpClient client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(0.3); 

                    try
                    {
                        if (Type == EventType.In || Type == EventType.Out)
                        {
                            var dir = Type == EventType.In ? 0 : 1;
                            var content = new FormUrlEncodedContent(new[]
                            {
                        new KeyValuePair<string, string>("dir", dir.ToString()),
                        new KeyValuePair<string, string>("syr", SyringeType),
                        new KeyValuePair<string, string>("vol", Volume.ToString()),
                        new KeyValuePair<string, string>("speed", Speed.ToString())
                    });

                            var response = await client.PostAsync($"http://{ip}/move", content);
                            response.EnsureSuccessStatusCode();
                        }
                        else if (Type == EventType.Pause)
                        {
                            var response = await client.PostAsync($"http://{ip}/stop", null);
                            response.EnsureSuccessStatusCode();
                        }
                        return;
                    }
                    catch (TaskCanceledException ex) //when (ex.CancellationToken == CancellationToken.None)
                    {
                        if (attempt == maxRetries - 1)
                        {
                            Status = EventStatus.Error;
                            throw new Exception(ip + " timeout.");
                        }
                    }
                    catch (Exception e)
                    {
                        Status = EventStatus.Error;
                        throw new Exception($"Pump not working: {e.Message}");
                    }
                }
                await Task.Delay(delayBetweenRetries);
            }
        }


        public EventStatus Check()
        {
            if (DateTime.Now > endTime)
            {
                Status = Status == EventStatus.Error ? EventStatus.ErrorDone : EventStatus.Done;
            }
            return Status;
        }

        public override string ToString()
        {
            string str = "";
            if (Status == EventStatus.Error || Status == EventStatus.ErrorDone)
                str = "ERROR! ";
            if (Type == EventType.Pause) 
            {
                return str + $"{hostname} | Pause {Volume:F3} min";
            }
            return str + $"{hostname} | {Type} | {Volume} mL | {Speed} mL/min";
        }
    }
}