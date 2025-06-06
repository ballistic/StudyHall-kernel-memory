// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Extensions.Configuration;
using Microsoft.KernelMemory;
using Microsoft.KM.TestHelpers;

namespace Microsoft.Postgres.FunctionalTests;

public class AdditionalFilteringTests : BaseFunctionalTestCase
{
    private readonly IKernelMemory _memory;

    public AdditionalFilteringTests(IConfiguration cfg, ITestOutputHelper output) : base(cfg, output)
    {
        this._memory = new KernelMemoryBuilder()
            .WithSearchClientConfig(new SearchClientConfig { EmptyAnswer = NotFound })
            //.WithOpenAI(this.OpenAiConfig)
            .WithAzureOpenAITextGeneration(this.AzureOpenAITextConfiguration)
            .WithAzureOpenAITextEmbeddingGeneration(this.AzureOpenAIEmbeddingConfiguration)
            .WithPostgresMemoryDb(this.PostgresConfig)
            .Build<MemoryServerless>();
    }

    [Fact]
    [Trait("Category", "Postgres")]
    public async Task ItFiltersSourcesCorrectly()
    {
        // Arrange
        const string Q = "in one or two words, what colors should I choose?";
        await this._memory.ImportTextAsync("green is a great color", documentId: "1", tags: new TagCollection { { "user", "hulk" } });
        await this._memory.ImportTextAsync("red is a great color", documentId: "2", tags: new TagCollection { { "user", "flash" } });

        // Act + Assert - See only memory about Green color
        var answer = await this._memory.AskAsync(Q, filter: MemoryFilters.ByDocument("1"));
        Assert.Contains("green", answer.Result, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("red", answer.Result, StringComparison.OrdinalIgnoreCase);

        answer = await this._memory.AskAsync(Q, filter: MemoryFilters.ByDocument("1").ByTag("user", "hulk"));
        Assert.Contains("green", answer.Result, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("red", answer.Result, StringComparison.OrdinalIgnoreCase);

        answer = await this._memory.AskAsync(Q, filter: MemoryFilters.ByTag("user", "hulk"));
        Assert.Contains("green", answer.Result, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("red", answer.Result, StringComparison.OrdinalIgnoreCase);

        answer = await this._memory.AskAsync(Q, filters: [MemoryFilters.ByTag("user", "x"), MemoryFilters.ByTag("user", "hulk")]);
        Assert.Contains("green", answer.Result, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("red", answer.Result, StringComparison.OrdinalIgnoreCase);

        // Act + Assert - See only memory about Red color
        answer = await this._memory.AskAsync(Q, filter: MemoryFilters.ByDocument("2"));
        Assert.Contains("red", answer.Result, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("green", answer.Result, StringComparison.OrdinalIgnoreCase);

        answer = await this._memory.AskAsync(Q, filter: MemoryFilters.ByDocument("2").ByTag("user", "flash"));
        Assert.Contains("red", answer.Result, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("green", answer.Result, StringComparison.OrdinalIgnoreCase);

        answer = await this._memory.AskAsync(Q, filter: MemoryFilters.ByTag("user", "flash"));
        Assert.Contains("red", answer.Result, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("green", answer.Result, StringComparison.OrdinalIgnoreCase);

        answer = await this._memory.AskAsync(Q, filters: [MemoryFilters.ByTag("user", "x"), MemoryFilters.ByTag("user", "flash")]);
        Assert.Contains("red", answer.Result, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("green", answer.Result, StringComparison.OrdinalIgnoreCase);

        // Act + Assert - See both memories
        answer = await this._memory.AskAsync(Q, filters: [MemoryFilters.ByTag("user", "hulk"), MemoryFilters.ByTag("user", "flash")]);
        Assert.Contains("green", answer.Result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("red", answer.Result, StringComparison.OrdinalIgnoreCase);

        // Act + Assert - See no memories about colors
        answer = await this._memory.AskAsync(Q, filter: MemoryFilters.ByDocument("1").ByTag("user", "flash"));
        Assert.DoesNotContain("red", answer.Result, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("green", answer.Result, StringComparison.OrdinalIgnoreCase);

        answer = await this._memory.AskAsync(Q, filter: MemoryFilters.ByDocument("2").ByTag("user", "hulk"));
        Assert.DoesNotContain("red", answer.Result, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("green", answer.Result, StringComparison.OrdinalIgnoreCase);

        answer = await this._memory.AskAsync(Q, filter: MemoryFilters.ByTag("user", "x"));
        Assert.DoesNotContain("red", answer.Result, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("green", answer.Result, StringComparison.OrdinalIgnoreCase);
    }
}
