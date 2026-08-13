using System;
using System.Threading;
using System.Threading.Tasks;

namespace Operator.AI;

public sealed class OperatorExecutionHooks
{
    public Func<int, CancellationToken, Task>? BeforePlanningStepAsync { get; init; }

    public Func<string, string, CancellationToken, Task<OperatorToolGateDecision>>? BeforeToolAsync { get; init; }
}

public sealed record OperatorToolGateDecision(bool Allowed, string Reason)
{
    public static OperatorToolGateDecision Continue() => new(true, "");

    public static OperatorToolGateDecision Block(string reason) => new(false, reason);
}
