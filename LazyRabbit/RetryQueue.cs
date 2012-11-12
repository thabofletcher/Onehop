using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading;

namespace LazyRabbit
{
    public class RetryQueue
    {
        SortedList<DateTime, MessageHostSender> _PendingSends = new SortedList<DateTime, MessageHostSender>();
        Dictionary<MessageHostSender, List<DateTime>> _FailedAttempts = new Dictionary<MessageHostSender, List<DateTime>>();
        ReaderWriterLock _Lock = new ReaderWriterLock();

        Thread _RunThread = null;
        private List<TimeSpan> _RetryAttempts;

        public RetryQueue(List<TimeSpan> retryAttempts = null)
        {
            if (retryAttempts != null)
                _RetryAttempts = retryAttempts;
            else
            {
                _RetryAttempts = new List<TimeSpan>();
                _RetryAttempts.Add(TimeSpan.FromMinutes(1));
                _RetryAttempts.Add(TimeSpan.FromMinutes(30));
                _RetryAttempts.Add(TimeSpan.FromMinutes(30));
                for (int i=0; i<12; i++) // one more day every two hours
                    _RetryAttempts.Add(TimeSpan.FromHours(2));
                for (int i = 0; i < 32; i++) // 4 more days every 3 hours
                    _RetryAttempts.Add(TimeSpan.FromHours(3));
            }
        }

        private volatile bool _Run = true;
        private void Run()
        {
            while (_Run)
            {
                try
                {
                    _Lock.AcquireWriterLock(10000);
                    while (_PendingSends.Count > 0)
                    {
                        // if the next message isnt ready to resend yet
                        if (_PendingSends.Keys[0] > DateTime.Now)
                            break;

                        _PendingSends.Values[0].TrySend();
                        _PendingSends.RemoveAt(0);
                    }
                }
                finally
                {
                    try { _Lock.ReleaseWriterLock(); }
                    catch { }
                }

                Thread.Sleep(1000);
            }
        }
      
        // not thread safe
        private void CheckRunThread()
        {
            if (_RunThread == null || !_RunThread.IsAlive)
            {
                _RunThread = new Thread(new ThreadStart(Run));
                _RunThread.Start();
            }
        }

        public void BeginRetry(MessageHostSender messageHostSender, string reason, string details)
        {
            try
            {
                if (!_Run)
                    return;

                _Lock.AcquireWriterLock(10000);
                List<DateTime> fails;

                if (!_FailedAttempts.TryGetValue(messageHostSender, out fails))
                {
                    fails = new List<DateTime>();
                    _FailedAttempts.Add(messageHostSender, fails);
                }

                int retryCount = fails.Count;
                fails.Add(DateTime.Now);

                if (retryCount < _RetryAttempts.Count)
                {
                    DateTime retryTime = DateTime.Now.Add(_RetryAttempts[retryCount]);
                    _PendingSends.Add(retryTime, messageHostSender);
                    CheckRunThread();
                    throw new SendAttemptFailed(fails, messageHostSender, retryTime, reason, details);
                }
                else
                {
                    // you have failed me for the last time admiral
                    _FailedAttempts.Remove(messageHostSender);
                    throw new SendAttemptsExhaustedException(fails, messageHostSender, reason, details);
                }
            }
            finally
            {
                try { _Lock.ReleaseWriterLock(); }
                catch { }
            }

        }

        public void Stop()
        {
            _Run = false;
            try
            {
				if (_RunThread != null && _RunThread.IsAlive && !_RunThread.Join(5000))
                    _RunThread.Abort();
            }
            catch { }
        }

        #region locking convention
        //try
        //{
        //    _Lock.AcquireWriterLock(10000);
        //    CRITICAL SECTION HERE
        //}
        //finally
        //{
        //    try { _Lock.ReleaseWriterLock(); } catch { }
        //}
        #endregion
    }
}
