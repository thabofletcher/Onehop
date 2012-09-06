using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;

namespace LazyRabbit
{
	public abstract class MessageHostSenderException : Exception
	{
		protected MessageHostSender _MessageSender;
		public MessageHostSenderException(MessageHostSender message)
		{
			_MessageSender = message;
		}
	}

	public class HostRejectedException : MessageHostSenderException
	{
		SmtpStatusCode _StatusCode;

		public HostRejectedException(SmtpStatusCode statusCode, MessageHostSender message) : base(message)
		{
			_StatusCode = statusCode;
		}

		public override string ToString()
		{
			return Message + Environment.NewLine + _MessageSender;
		}

		public override string Message
		{
			get
			{
				return String.Format("The mail host {0} rejected the message. The message will not be retried. Status code: {1}", _MessageSender.Host, _StatusCode);
			}
		}
	}

	public class FailedRecipientException : MessageHostSenderException
	{
		protected string _FailedRecipients;

		public FailedRecipientException(string failedRecipients, MessageHostSender messageSender) : base(messageSender)
		{
			_FailedRecipients = failedRecipients;
		}

		public override string ToString()
		{
			return Message + Environment.NewLine + _MessageSender;
		}

		public override string Message
		{
			get
			{
				return String.Format("The mail host {0} rejected one or more recipients when attempting to send mail. The message will not be retried. Failed for: {1}", _MessageSender.Host, _FailedRecipients);
			}
		}
	}

	public class SendAttemptsExhaustedException : MessageHostSenderException
	{
		protected string _CurrentFailureMessage;
		protected List<DateTime> _SendAttempts;
		
		protected string _CurrentFailureDetails;
		public SendAttemptsExhaustedException(List<DateTime> sendAttempts, MessageHostSender message, string failure, string details) : base(message)
		{
			_SendAttempts = sendAttempts;
			_MessageSender = message;
			_CurrentFailureMessage = failure;
			_CurrentFailureDetails = details;
		}

		public override string ToString()
		{
			return Message + Environment.NewLine + _MessageSender + Environment.NewLine + "Send Attempts: " + Environment.NewLine + _SendAttempts.Select(x => x.ToString()).Aggregate((i, j) => i + Environment.NewLine + j) + Environment.NewLine + _CurrentFailureDetails;
		}

		public override string Message
		{
			get
			{
				return String.Format("An attempt to send mail to {0} has failed permanently. Current failure: {1}", _MessageSender.Host, _CurrentFailureMessage);
			}
		}
	}

	public class SendAttemptFailed : SendAttemptsExhaustedException
	{
		private DateTime _NextAttempt;
		public DateTime NextAttempt { get { return _NextAttempt; } }
		public SendAttemptFailed(List<DateTime> sendAttempts, MessageHostSender message, DateTime nextAttempt, string failure, string details)
			: base(sendAttempts, message, failure, details)
		{
			_NextAttempt = nextAttempt;
		}

		public override string ToString()
		{
			return Message + Environment.NewLine + _MessageSender + Environment.NewLine + "First Attempt: " + _SendAttempts.FirstOrDefault() + Environment.NewLine + "Last Attempt: " + _SendAttempts.LastOrDefault() + Environment.NewLine + "Total Attempts: " + _SendAttempts.Count + Environment.NewLine + _CurrentFailureDetails;
		}

		public override string Message
		{
			get
			{
				return String.Format("An attempt to send mail to {0} has failed temporarily. The system will try again at {1}. Current failure: {2}", _MessageSender.Host, _NextAttempt, _CurrentFailureMessage);
			}
		}
	}
}
