using AiAgentsTeam.Domain.Common;

namespace AiAgentsTeam.Domain.Intent;

public class ClarificationAnswer : Entity
{
    public Guid IntentSessionId { get; private set; }
    public string Question { get; private set; } = default!;
    public string Answer { get; private set; } = default!;

    private ClarificationAnswer() { }

    public ClarificationAnswer(Guid intentSessionId, string question, string answer)
    {
        IntentSessionId = intentSessionId;
        Question = question;
        Answer = answer;
    }
}
