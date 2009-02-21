﻿namespace BoxSync.Core.Statuses
{

	/// <summary>
	/// Specifies statuses of 'get_ticket' web method
	/// </summary>
	public enum GetTicketStatus
	{
		/// <summary>
		/// Used if status string doen't match to any of enum members
		/// </summary>
		Unknown = 0,

		/// <summary>
		/// Represents 'get_ticket_ok' status string
		/// </summary>
		Successful = 1
	}
}