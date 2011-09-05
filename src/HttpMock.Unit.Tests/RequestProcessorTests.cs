﻿using System;
using System.Collections.Generic;
using Kayak;
using Kayak.Http;
using NUnit.Framework;
using Rhino.Mocks;

namespace HttpMock.Unit.Tests {
	[TestFixture]
	public class RequestProcessorTests
	{
		private RequestProcessor _processor;
		private IDataProducer _dataProducer;
		private IHttpResponseDelegate _httpResponseDelegate;
		private IMatchingRule _ruleThatReturnsFirstHandler;
		private IMatchingRule _ruleThatReturnsNoHandlers;
		private IStubResponse _defaultResponse;
		private RequestHandlerFactory _requestHandlerFactory;

		[SetUp]
		public void SetUp() {
			_processor = new RequestProcessor(_ruleThatReturnsFirstHandler, new List<RequestHandler>());
			_requestHandlerFactory = new RequestHandlerFactory(_processor);
			_dataProducer = MockRepository.GenerateStub<IDataProducer>();
			_httpResponseDelegate = MockRepository.GenerateStub<IHttpResponseDelegate>();

			_defaultResponse = MockRepository.GenerateStub<IStubResponse>();

			_ruleThatReturnsFirstHandler = MockRepository.GenerateStub<IMatchingRule>();
			_ruleThatReturnsFirstHandler.Stub(x => x.IsEndpointMatch(null, new HttpRequestHead())).IgnoreArguments().Return(true).Repeat.Once();

			_ruleThatReturnsNoHandlers = MockRepository.GenerateStub<IMatchingRule>();
			_ruleThatReturnsNoHandlers.Stub(x => x.IsEndpointMatch(null, new HttpRequestHead())).IgnoreArguments().Return(false);
		}

		[Test]
		public void Get_should_return_handler_with_get_method_set() {
			RequestHandler requestHandler = _requestHandlerFactory.Get("nowhere");
			Assert.That(requestHandler.Method, Is.EqualTo("GET"));
		}

		[Test]
		public void Post_should_return_handler_with_post_method_set() {
			RequestHandler requestHandler = _requestHandlerFactory.Post("nowhere");
			Assert.That(requestHandler.Method, Is.EqualTo("POST"));
		}

		[Test]
		public void Put_should_return_handler_with_put_method_set() {
			RequestHandler requestHandler = _requestHandlerFactory.Put("nowhere");
			Assert.That(requestHandler.Method, Is.EqualTo("PUT"));
		}

		[Test]
		public void Delete_should_return_handler_with_delete_method_set() {
			RequestHandler requestHandler = _requestHandlerFactory.Delete("nowhere");
			Assert.That(requestHandler.Method, Is.EqualTo("DELETE"));
		}

		[Test]
		public void Head_should_return_handler_with_head_method_set() {
			RequestHandler requestHandler = _requestHandlerFactory.Head("nowhere");
			Assert.That(requestHandler.Method, Is.EqualTo("HEAD"));
		}

		[Test]
		public void If_no_handlers_found_should_fire_onresponse_with_a_404() {
			_processor = new RequestProcessor(_ruleThatReturnsNoHandlers, new List<RequestHandler>());

			_processor.Add(_requestHandlerFactory.Get("test"));
			_processor.OnRequest(new HttpRequestHead(), _dataProducer, _httpResponseDelegate);
			_httpResponseDelegate.AssertWasCalled(x => x.OnResponse(Arg<HttpResponseHead>.Matches(y => y.Status == "404 NotFound"), Arg<IDataProducer>.Is.Null));
		}

		[Test]
		public void If_a_handler_found_should_fire_onresponse_with_that_repsonse() {
			_processor = new RequestProcessor(_ruleThatReturnsFirstHandler, new List<RequestHandler>());

			RequestHandler requestHandler = _requestHandlerFactory.Get("test");
			_processor.Add(requestHandler);
			_processor.OnRequest(new HttpRequestHead{ Headers =  new Dictionary<string, string>()}, _dataProducer, _httpResponseDelegate);

			_httpResponseDelegate.AssertWasCalled(x => x.OnResponse(requestHandler.ResponseBuilder.BuildHeaders(), requestHandler.ResponseBuilder.BuildBody()));
		}
		
		[Test]
		public void Matching_HEAD_handler_should_output_handlers_expected_response_with_null_body() {

			_processor = new RequestProcessor(_ruleThatReturnsFirstHandler, new List<RequestHandler>());

			RequestHandler requestHandler = _requestHandlerFactory.Head("test");
			_processor.Add(requestHandler);
			var httpRequestHead = new HttpRequestHead { Method = "HEAD", Headers = new Dictionary<string, string>() };
			_processor.OnRequest(httpRequestHead, _dataProducer, _httpResponseDelegate);

			_httpResponseDelegate.AssertWasCalled(x => x.OnResponse(requestHandler.ResponseBuilder.BuildHeaders(), null));
		}

		[Test]
		public void When_a_handler_is_added_should_be_able_to_find_it() {
			string expectedPath = "/blah/test";
			string expectedMethod = "GET";

			var requestProcessor = new RequestProcessor(null, new List<RequestHandler>());

			requestProcessor.Add(_requestHandlerFactory.Get(expectedPath));

			var handler = requestProcessor.FindHandler(expectedMethod, expectedPath);

			Assert.That(handler.Path, Is.EqualTo(expectedPath));
			Assert.That(handler.Method, Is.EqualTo(expectedMethod));

		}

		[Test]
		public void When_a_handler_is_hit_handlers_request_count_is_incremented() {

			string expectedPath = "/blah/test";
			string expectedMethod = "GET";

			var requestProcessor = new RequestProcessor(_ruleThatReturnsFirstHandler, new List<RequestHandler>());

			requestProcessor.Add(_requestHandlerFactory.Get(expectedPath));
			var httpRequestHead = new HttpRequestHead { Headers = new Dictionary<string, string>() };
			httpRequestHead.Path = expectedPath;
			httpRequestHead.Method = expectedPath;
			requestProcessor.OnRequest(httpRequestHead, _dataProducer, _httpResponseDelegate);

			var handler = requestProcessor.FindHandler(expectedMethod, expectedPath);
			Assert.That(handler.RequestCount(), Is.EqualTo(1));
		}
	}
}
