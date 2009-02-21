﻿using System;

using BoxSync.Core.Primitives;


namespace BoxSync.Core
{
	/// <summary>
	/// The exception that is thrown when the operation with ObjectType is not supported
	/// </summary>
	public sealed class NotSupportedObjectTypeException : Exception
	{
		public NotSupportedObjectTypeException(ObjectType objectType)
			: base(string.Format("Not supported object type [{0}]", objectType))
		{ }
	}
}
