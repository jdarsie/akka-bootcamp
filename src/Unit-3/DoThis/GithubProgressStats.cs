using System;
using Octokit;

namespace GithubActors
{
    /// <summary>
    /// used to sort the list of similar repos
    /// </summary>
    public class SimilarRepo : IComparable<SimilarRepo>
    {
        public Repository Repo { get; private set; }

        public int SharedStarrers { get; set; }

        public SimilarRepo(Repository repo)
        {
            Repo = repo;
        }

        #region

        public int CompareTo(SimilarRepo other)
        {
            return SharedStarrers.CompareTo(other.SharedStarrers);
        }

        #endregion
    }

    /// <summary>
    /// Used to report on incremental progress.
    /// 
    /// Immutable.
    /// </summary>
    public class GithubProgressStats
    {
        public TimeSpan Elapsed
        {
            get { return (EndTime.HasValue ? EndTime.Value : DateTime.UtcNow) - StartTime; }
        }

        public DateTime? EndTime { get; private set; }
        public int ExpectedUsers { get; private set; }

        public bool IsFinished
        {
            get { return ExpectedUsers == UsersThusFar + QueryFailures; }
        }

        public int QueryFailures { get; private set; }
        public DateTime StartTime { get; private set; }
        public int UsersThusFar { get; private set; }

        public GithubProgressStats()
        {
            StartTime = DateTime.UtcNow;
        }

        private GithubProgressStats(DateTime startTime, int expectedUsers, int usersThusFar, int queryFailures, DateTime? endTime)
        {
            EndTime = endTime;
            QueryFailures = queryFailures;
            UsersThusFar = usersThusFar;
            ExpectedUsers = expectedUsers;
            StartTime = startTime;
        }

        /// <summary>
        /// Creates a deep copy of the <see cref="GithubProgressStats"/> class
        /// </summary>
        public GithubProgressStats Copy(int? expectedUsers = null, int? usersThusFar = null, int? queryFailures = null,
            DateTime? startTime = null, DateTime? endTime = null)
        {
            return new GithubProgressStats(startTime ?? StartTime, expectedUsers ?? ExpectedUsers, usersThusFar ?? UsersThusFar,
                queryFailures ?? QueryFailures, endTime ?? EndTime);
        }

        /// <summary>
        /// Query is finished! Set's the <see cref="EndTime"/>
        /// </summary>
        public GithubProgressStats Finish()
        {
            return Copy(endTime: DateTime.UtcNow);
        }

        /// <summary>
        /// Add <see cref="delta"/> to the running <see cref="QueryFailures"/> total
        /// </summary>
        public GithubProgressStats IncrementFailures(int delta = 1)
        {
            return Copy(queryFailures: QueryFailures + delta);
        }

        /// <summary>
        /// Set the <see cref="ExpectedUsers"/> total
        /// </summary>
        public GithubProgressStats SetExpectedUserCount(int totalExpectedUsers)
        {
            return Copy(expectedUsers: totalExpectedUsers);
        }

        /// <summary>
        /// Add <see cref="delta"/> users to the running total of <see cref="UsersThusFar"/>
        /// </summary>
        public GithubProgressStats UserQueriesFinished(int delta = 1)
        {
            return Copy(usersThusFar: UsersThusFar + delta);
        }
    }
}