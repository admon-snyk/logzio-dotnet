﻿using log4net.Core;
using Logzio.DotNet.Core.Shipping;
using Logzio.DotNet.Log4net;
using NSubstitute;
using NUnit.Framework;

namespace Logzio.DotNet.UnitTests.Log4net
{
	[TestFixture]
	public class LogzioAppenderTests
	{
		private LogzioAppender _target;
		private IShipper _shipper;

		[SetUp]
		public void Setup()
		{
			_target = new LogzioAppender
			{
				Shipper = _shipper = Substitute.For<IShipper>()
			};
		}

		[Test]
		public void Append_Appending_CallsShipperWithCorrectValues()
		{
			_target.DoAppend(GetLoggingEventWithSomeData());
			_target.Close();
			_shipper.Received().Ship(Arg.Is<LogzioLoggingEvent>(x => x.LogData["domain"] == "Such domain"));
		}

		[Test]
		public void Append_AppendingWithCustomFields_CallsShipperWithCorrectValues()
		{
			_target.AddCustomField(new LogzioAppenderCustomField { Key = "DatKey", Value = "DatVal"});
			_target.DoAppend(GetLoggingEventWithSomeData());
			_target.Close();

			_shipper.Received().Ship(Arg.Is<LogzioLoggingEvent>(x => x.LogData["DatKey"] == "DatVal"));
		}

		private LoggingEvent GetLoggingEventWithSomeData()
		{
			return new LoggingEvent(new LoggingEventData
			{
				Domain = "Such domain",
				Level = Level.Info,
				Message = "That's a message alright"
			});
		}
	}
}