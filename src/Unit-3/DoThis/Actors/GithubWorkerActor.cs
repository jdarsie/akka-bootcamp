using System;
using System.Collections.Generic;
using System.Linq;
using Akka.Actor;
using Octokit;

namespace GithubActors.Actors
{
    /// <summary>
    /// Individual actor responsible for querying the Github API
    /// </summary>
    public class GithubWorkerActor : ReceiveActor
    {
        #region  -- Inner Types --

        /// <summary>
        /// Query an individual starrer
        /// </summary>
        public class QueryStarrer
        {
            public string Login { get; private set; }

            public QueryStarrer(string login)
            {
                Login = login;
            }
        }

        public class QueryStarrers
        {
            public RepoKey Key { get; private set; }

            public QueryStarrers(RepoKey key)
            {
                Key = key;
            }
        }

        public class StarredReposForUser
        {
            public string Login { get; private set; }

            public IEnumerable<Repository> Repos { get; private set; }

            public StarredReposForUser(string login, IEnumerable<Repository> repos)
            {
                Repos = repos;
                Login = login;
            }
        }

        #endregion

        private readonly Func<IGitHubClient> _gitHubClientFactory;

        private IGitHubClient _gitHubClient;

        public GithubWorkerActor(Func<IGitHubClient> gitHubClientFactory)
        {
            _gitHubClientFactory = gitHubClientFactory;
            InitialReceives();
        }

        private void InitialReceives()
        {
            // query an individual starrer
            Receive<RetryableQuery>(query => query.Query is QueryStarrer, query =>
            {
                var starrer = ((QueryStarrer)query.Query).Login;

                // close over the Sender in an instance variable

                var sender = Sender;
                _gitHubClient.Activity.Starring.GetAllForUser(starrer).
                    ContinueWith<object>(tr =>
                    {
                        // query faulted
                        if (tr.IsFaulted || tr.IsCanceled)
                        {
                            return query.NextTry();
                        }
                        // query succeeded
                        return new StarredReposForUser(starrer, tr.Result);
                    }).PipeTo(sender);
            });

            // query all starrers for a repository
            Receive<RetryableQuery>(query => query.Query is QueryStarrers, query =>
            {
                var starrers = ((QueryStarrers)query.Query).Key;

                // close over the Sender in an instance variable
                var sender = Sender;

                _gitHubClient.Activity.Starring.GetAllStargazers(starrers.Owner, starrers.Repo).
                    ContinueWith<object>(tr =>
                    {
                        // query faulted
                        if (tr.IsFaulted || tr.IsCanceled)
                            return query.NextTry();
                        return tr.Result.ToArray();
                    }).
                    PipeTo(sender);
            });
        }

        protected override void PreStart()
        {
            _gitHubClient = _gitHubClientFactory();
        }
    }
}