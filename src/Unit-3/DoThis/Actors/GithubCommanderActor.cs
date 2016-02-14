using System;
using System.Linq;
using Akka.Actor;
using Akka.Routing;

namespace GithubActors.Actors
{
    /// <summary>
    /// Top-level actor responsible for coordinating and launching repo-processing jobs
    /// </summary>
    public class GithubCommanderActor : ReceiveActor, IWithUnboundedStash
    {
        #region  -- Inner Types --

        public class AbleToAcceptJob
        {
            public RepoKey Repo { get; private set; }

            public AbleToAcceptJob(RepoKey repo)
            {
                Repo = repo;
            }
        }

        public class CanAcceptJob
        {
            public RepoKey Repo { get; private set; }

            public CanAcceptJob(RepoKey repo)
            {
                Repo = repo;
            }
        }

        public class UnableToAcceptJob
        {
            public RepoKey Repo { get; private set; }

            public UnableToAcceptJob(RepoKey repo)
            {
                Repo = repo;
            }
        }

        #endregion

        private IActorRef _canAcceptJobSender;

        private IActorRef _coordinator;

        private int _pendingJobReplies;

        private RepoKey _repoJob;

        public IStash Stash { get; set; }

        public GithubCommanderActor()
        {
            Ready();
        }

        private void Asking()
        {
            // stash any subsequent requests
            Receive<CanAcceptJob>(job => Stash.Stash());

            Receive<UnableToAcceptJob>(job =>
            {
                _pendingJobReplies--;

                if (_pendingJobReplies != 0)
                {
                    return;
                }

                _canAcceptJobSender.Tell(job);
                BecomeReady();
            });

            Receive<AbleToAcceptJob>(job =>
            {
                _canAcceptJobSender.Tell(job);

                // start processing messages
                Sender.Tell(new GithubCoordinatorActor.BeginJob(job.Repo));

                // launch the new window to view results of the processing
                Context.ActorSelection(ActorPaths.MainFormActor.Path).Tell(new MainFormActor.LaunchRepoResultsWindow(job.Repo, Sender));

                BecomeReady();
            });

            Receive<ReceiveTimeout>(timeout =>
            {
                _canAcceptJobSender.Tell(new UnableToAcceptJob(_repoJob));
                BecomeReady();
            });
        }

        private void BecomeAsking()
        {
            _canAcceptJobSender = Sender;
            // block, but ask the router for the number of routees. Avoids magic numbers.
            _pendingJobReplies = _coordinator.Ask<Routees>(new GetRoutees()).Result.Members.Count();
            Become(Asking);

            // send ourselves a ReceiveTimeout message if no message within 3 seonds
            Context.SetReceiveTimeout(TimeSpan.FromSeconds(3));
        }

        private void BecomeReady()
        {
            Become(Ready);
            Stash.UnstashAll();

            // cancel ReceiveTimeout
            Context.SetReceiveTimeout(null);
        }

        protected override void PreRestart(Exception reason, object message)
        {
            //kill off the old coordinator so we can recreate it from scratch
            _coordinator.Tell(PoisonPill.Instance);
            base.PreRestart(reason, message);
        }

        protected override void PreStart()
        {
            // create a broadcast router who will ask all of them if they're available for work
            _coordinator = Context.ActorOf(Props.Create(() => new GithubCoordinatorActor()).WithRouter(FromConfig.Instance), ActorPaths.GithubCoordinatorActor.Name);

            base.PreStart();
        }

        private void Ready()
        {
            Receive<CanAcceptJob>(job =>
            {
                _coordinator.Tell(job);
                _repoJob = job.Repo;
                BecomeAsking();
            });
        }
    }
}