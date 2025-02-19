using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;

namespace ShigureController
{
    internal class Pump
    {
        private string pumpIp;
        public Queue<Event> EventPlanned;
        public Queue<Event> EventProcessing;
        public Queue<Event> EventDone;
        public string SyringeType = "UNSET";
        public string Name;
        public Pump(string ip, string name)
        {
            pumpIp = ip;
            Name = name;
            EventPlanned = new Queue<Event>();
            EventProcessing = new Queue<Event>();
            EventDone = new Queue<Event>();

        }
        public void AddMotionEvent(EventType type, double volume, double speed)
        {
            EventPlanned.Enqueue(new Event(type, volume, speed, Name, this.SyringeType));
        }
        public void AddPauseEvent(double duration)
        {
            EventPlanned.Enqueue(new Event(EventType.Pause, duration, 1, Name, this.SyringeType));
        }
        public async Task Check()
        {
            if(SyringeType == "UNSET")
                throw new Exception("Syringe type not set!");

            if (EventProcessing.Count == 0 && EventPlanned.Count > 0)
            {
                EventProcessing.Enqueue(EventPlanned.Dequeue());
                await EventProcessing.Peek().Run(pumpIp);
            }
            else if (EventProcessing.Count > 0)
            {
                var status = EventProcessing.Peek().Check();
                if (status == EventStatus.Done || status == EventStatus.ErrorDone)
                {
                    EventDone.Enqueue(EventProcessing.Dequeue());
                }
            }
        }
        public async Task Stop()
        {
            int maxRetries = 10;
            int delayBetweenRetries = 20;

            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                try
                {
                    using (HttpClient client = new HttpClient())
                    {
                        client.Timeout = TimeSpan.FromSeconds(0.3);
                        HttpResponseMessage response = await client.PostAsync($"http://{pumpIp}/stop", null);
                        if (response.IsSuccessStatusCode)
                        {
                            return; 
                        }
                        else
                        {
                            throw new Exception($"Failed to stop pump at {pumpIp}. Status code: {response.StatusCode}");
                        }
                    }
                }
                catch (TaskCanceledException ex)
                {
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"ERROR: Failed to stop pump at {pumpIp} on attempt {attempt + 1}: {ex.Message}");
                    break;
                }
                await Task.Delay(delayBetweenRetries);
            }
        }

    }
}
