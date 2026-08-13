using System;
using System.Collections.Generic;

namespace Operator.AI;

public sealed class AgentRunGuard
{
    private readonly Dictionary<string, int>
        _toolCallCounts =
            new(StringComparer.Ordinal);

    private int _consecutiveErrors;

    public int MaximumRepeatedToolCalls { get; init; } = 3;

    public int MaximumConsecutiveErrors { get; init; } = 5;

    public bool CanExecuteTool(
        string functionName,
        string arguments,
        out string reason)
    {
        string signature =
            $"{functionName}:{arguments}";

        if (!_toolCallCounts.TryGetValue(
                signature,
                out int count))
        {
            count = 0;
        }

        count++;

        _toolCallCounts[signature] =
            count;

        if (count > MaximumRepeatedToolCalls)
        {
            reason =
                $"Repeated tool call blocked. " +
                $"'{functionName}' was requested " +
                $"{count} times with the same arguments. " +
                $"Choose a different recovery strategy.";

            return false;
        }

        reason = "";

        return true;
    }

    public void RegisterResult(
        string result)
    {
        if (IsFailure(result))
        {
            _consecutiveErrors++;
        }
        else
        {
            _consecutiveErrors = 0;
        }
    }

    public bool TooManyErrors(
        out string reason)
    {
        if (_consecutiveErrors >=
            MaximumConsecutiveErrors)
        {
            reason =
                $"Agent stopped after " +
                $"{_consecutiveErrors} consecutive failures.";

            return true;
        }

        reason = "";

        return false;
    }

    public static bool IsFailure(
        string result)
    {
        if (string.IsNullOrWhiteSpace(result))
        {
            return true;
        }

        return
            result.StartsWith(
                "ERROR",
                StringComparison.OrdinalIgnoreCase)
            ||
            result.StartsWith(
                "NOT_FOUND",
                StringComparison.OrdinalIgnoreCase)
            ||
            result.StartsWith(
                "BLOCKED",
                StringComparison.OrdinalIgnoreCase);
    }
}