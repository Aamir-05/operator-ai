using OpenAI.Responses;

namespace Operator.AI;

#pragma warning disable OPENAI001

public sealed class OpenAIService
{
    private readonly ResponsesClient _client;

    public OpenAIService()
    {
        string? apiKey =
            Environment.GetEnvironmentVariable(
                "OPENAI_API_KEY"
            );

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException(
                "OPENAI_API_KEY was not found."
            );
        }

        _client = new ResponsesClient(apiKey);
    }

    public async Task<string> AskAsync(string prompt)
    {
        ResponseResult response =
            await _client.CreateResponseAsync(
                "gpt-5.6",
                prompt
            );

        return response.GetOutputText();
    }
}

#pragma warning restore OPENAI001