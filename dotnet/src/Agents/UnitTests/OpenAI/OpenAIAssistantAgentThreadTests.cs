﻿// Copyright (c) Microsoft. All rights reserved.

using System;
using System.ClientModel;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents.OpenAI;
using Microsoft.SemanticKernel.ChatCompletion;
using Moq;
using OpenAI;
using OpenAI.Assistants;
using Xunit;

namespace SemanticKernel.Agents.UnitTests.OpenAI;

/// <summary>
/// Tests for the <see cref="OpenAIAssistantAgentThread"/> class.
/// </summary>
public class OpenAIAssistantAgentThreadTests : IDisposable
{
    private readonly HttpMessageHandlerStub _messageHandlerStub;
    private readonly HttpClient _httpClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenAIAssistantAgentThreadTests"/> class.
    /// </summary>
    public OpenAIAssistantAgentThreadTests()
    {
        this._messageHandlerStub = new HttpMessageHandlerStub();
        this._httpClient = new HttpClient(this._messageHandlerStub, disposeHandler: false);
    }

    /// <summary>
    /// Tests that the constructor verifies parameters and throws <see cref="ArgumentNullException"/> when necessary.
    /// </summary>
    [Fact]
    public void ConstructorShouldVerifyParams()
    {
        // Arrange
        var mockClient = new Mock<AssistantClient>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new OpenAIAssistantAgentThread(null!));
        Assert.Throws<ArgumentNullException>(() => new OpenAIAssistantAgentThread(null!, "threadId"));
        Assert.Throws<ArgumentNullException>(() => new OpenAIAssistantAgentThread(mockClient.Object, id: null!));

        var thread = new OpenAIAssistantAgentThread(mockClient.Object);
        Assert.NotNull(thread);
    }

    /// <summary>
    /// Tests that the constructor for resuming a thread uses the provided parameters.
    /// </summary>
    [Fact]
    public void ConstructorForResumingThreadShouldUseParams()
    {
        // Arrange
        var mockClient = new Mock<AssistantClient>();

        // Act
        var threadWithId = new OpenAIAssistantAgentThread(mockClient.Object, "threadId");

        // Assert
        Assert.NotNull(threadWithId);
        Assert.Equal("threadId", threadWithId.Id);
    }

    /// <summary>
    /// Tests that the CreateAsync method invokes the client and sets the thread ID.
    /// </summary>
    [Fact]
    public async Task CreateShouldInvokeClientAsync()
    {
        // Arrange
        this._messageHandlerStub.SetupResponses(HttpStatusCode.OK, OpenAIAssistantResponseContent.CreateThread);

        var client = this.CreateTestClient();

        var thread = new OpenAIAssistantAgentThread(client.GetAssistantClient());

        // Act
        await thread.CreateAsync();

        // Assert
        Assert.Equal("thread_abc123", thread.Id);
        Assert.Empty(this._messageHandlerStub.ResponseQueue);
    }

    /// <summary>
    /// Tests that the CreateAsync method invokes the client and sets the thread ID.
    /// </summary>
    [Fact]
    public async Task CreateWithOptionsShouldInvokeClientAsync()
    {
        // Arrange
        this._messageHandlerStub.SetupResponses(HttpStatusCode.OK, OpenAIAssistantResponseContent.CreateThread);

        var client = this.CreateTestClient();

        var thread = new OpenAIAssistantAgentThread(client.GetAssistantClient(), new ThreadCreationOptions());

        // Act
        await thread.CreateAsync();

        // Assert
        Assert.Equal("thread_abc123", thread.Id);
        Assert.Empty(this._messageHandlerStub.ResponseQueue);
    }

    /// <summary>
    /// Tests that the CreateAsync method invokes the client and sets the thread ID.
    /// </summary>
    [Fact]
    public async Task CreateWithParamsShouldInvokeClientAsync()
    {
        // Arrange
        this._messageHandlerStub.SetupResponses(HttpStatusCode.OK, OpenAIAssistantResponseContent.CreateThread);

        var client = this.CreateTestClient();

        var thread = new OpenAIAssistantAgentThread(client.GetAssistantClient(), [new ChatMessageContent(AuthorRole.User, "Hello")]);

        // Act
        await thread.CreateAsync();

        // Assert
        Assert.Equal("thread_abc123", thread.Id);
        Assert.Empty(this._messageHandlerStub.ResponseQueue);
    }

    /// <summary>
    /// Tests that the DeleteAsync method invokes the client.
    /// </summary>
    [Fact]
    public async Task DeleteShouldInvokeClientAsync()
    {
        // Arrange
        this._messageHandlerStub.SetupResponses(HttpStatusCode.OK, OpenAIAssistantResponseContent.CreateThread);
        this._messageHandlerStub.SetupResponses(HttpStatusCode.OK, OpenAIAssistantResponseContent.DeleteThread);

        var client = this.CreateTestClient();

        var thread = new OpenAIAssistantAgentThread(client.GetAssistantClient());
        await thread.CreateAsync();

        // Act
        await thread.DeleteAsync();

        // Assert
        Assert.Empty(this._messageHandlerStub.ResponseQueue);
    }

    /// <summary>
    /// Tests that the GetMessagesAsync method invokes the client.
    /// </summary>
    [Fact]
    public async Task GetMessagesShouldInvokeClientAsync()
    {
        // Arrange
        this._messageHandlerStub.SetupResponses(HttpStatusCode.OK, OpenAIAssistantResponseContent.ListMessagesPageFinal);
        var client = this.CreateTestClient();
        var thread = new OpenAIAssistantAgentThread(client.GetAssistantClient(), "thread_abc123");

        // Act
        var messages = await thread.GetMessagesAsync().ToListAsync();

        // Assert
        Assert.NotNull(messages);
        Assert.Single(messages);
        Assert.Equal("How does AI work? Explain it in simple terms.", messages[0].Content);
        Assert.Empty(this._messageHandlerStub.ResponseQueue);
    }

    /// <summary>
    /// Tests that the GetMessagesAsync method creates a thread if it does not exist yet.
    /// </summary>
    [Fact]
    public async Task GetMessagesShouldCreateThreadAsync()
    {
        // Arrange
        this._messageHandlerStub.SetupResponses(HttpStatusCode.OK, OpenAIAssistantResponseContent.CreateThread);
        this._messageHandlerStub.SetupResponses(HttpStatusCode.OK, OpenAIAssistantResponseContent.ListMessagesPageFinal);
        var client = this.CreateTestClient();
        var thread = new OpenAIAssistantAgentThread(client.GetAssistantClient());

        // Act
        var messages = await thread.GetMessagesAsync().ToListAsync();

        // Assert
        Assert.NotNull(messages);
        Assert.Single(messages);
        Assert.Equal("How does AI work? Explain it in simple terms.", messages[0].Content);
        Assert.Empty(this._messageHandlerStub.ResponseQueue);
    }

    /// <summary>
    /// Tests that the GetMessagesAsync method throws for a deleted thread.
    /// </summary>
    [Fact]
    public async Task GetMessagesShouldThrowForDeletedThreadAsync()
    {
        // Arrange
        this._messageHandlerStub.SetupResponses(HttpStatusCode.OK, OpenAIAssistantResponseContent.CreateThread);
        this._messageHandlerStub.SetupResponses(HttpStatusCode.OK, OpenAIAssistantResponseContent.DeleteThread);

        var client = this.CreateTestClient();

        var thread = new OpenAIAssistantAgentThread(client.GetAssistantClient());
        await thread.CreateAsync();
        await thread.DeleteAsync();

        // Act
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await thread.GetMessagesAsync().ToListAsync());
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        this._messageHandlerStub.Dispose();
        this._httpClient.Dispose();

        GC.SuppressFinalize(this);
    }

    private OpenAIClient CreateTestClient()
        => OpenAIAssistantAgent.CreateOpenAIClient(apiKey: new ApiKeyCredential("fakekey"), endpoint: null, this._httpClient);
}
