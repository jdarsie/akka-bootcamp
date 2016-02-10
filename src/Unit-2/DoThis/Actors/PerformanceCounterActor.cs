using System;
using System.Collections.Generic;
using System.Diagnostics;
using Akka.Actor;

namespace ChartApp.Actors
{
    public class PerformanceCounterActor : UntypedActor
    {
        private readonly ICancelable _cancelPublishing;
        private readonly Func<PerformanceCounter> _performanceCounterGenerator;
        private readonly string _seriesName;

        private readonly HashSet<IActorRef> _subscriptions;
        private PerformanceCounter _counter;

        public PerformanceCounterActor(string seriesName, Func<PerformanceCounter> performanceCounterGenerator)
        {
            if (performanceCounterGenerator == null)
            {
                throw new ArgumentNullException(nameof(performanceCounterGenerator));
            }
            if (string.IsNullOrWhiteSpace(seriesName))
            {
                throw new ArgumentException("Argument is null or whitespace", nameof(seriesName));
            }

            _seriesName = seriesName;
            _performanceCounterGenerator = performanceCounterGenerator;

            _subscriptions = new HashSet<IActorRef>();
            _cancelPublishing = new Cancelable(Context.System.Scheduler);
        }

        protected override void OnReceive(object message)
        {
            if (message is GatherMetrics)
            {
                //publish latest counter value to all subscribers
                var metric = new Metric(_seriesName, _counter.NextValue());

                foreach (var sub in _subscriptions)
                {
                    sub.Tell(metric);
                }
            }
            else if (message is SubscribeCounter)
            {
                // add a subscription for this counter
                // (it's parent's job to filter by counter types)
                var sc = (SubscribeCounter)message;
                _subscriptions.Add(sc.Subscriber);
            }
            else if (message is UnsubscribeCounter)
            {
                // remove a subscription from this counter
                var uc = (UnsubscribeCounter)message;
                _subscriptions.Remove(uc.Subscriber);
            }
        }

        protected override void PostStop()
        {
            try
            {
                //terminate the scheduled task
                _cancelPublishing.Cancel(false);
                _counter.Dispose();
            }
            catch
            {
                //don't care about additional "ObjectDisposed" exceptions
            }
            finally
            {
                base.PostStop();
            }
        }

        protected override void PreStart()
        {
            //create a new instance of the performance counter
            _counter = _performanceCounterGenerator();

            Context.System.Scheduler.ScheduleTellRepeatedly(
                TimeSpan.FromMilliseconds(250),
                TimeSpan.FromMilliseconds(250),
                Self,
                new GatherMetrics(),
                Self,
                _cancelPublishing);
        }
    }
}