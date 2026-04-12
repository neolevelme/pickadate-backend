namespace Pickadate.BuildingBlocks.Domain;

public interface IBusinessRule
{
    bool IsBroken();
    string Message { get; }
}

public class BusinessRuleValidationException : Exception
{
    public IBusinessRule BrokenRule { get; }
    public string Details { get; }

    public BusinessRuleValidationException(IBusinessRule brokenRule)
        : base(brokenRule.Message)
    {
        BrokenRule = brokenRule;
        Details = brokenRule.Message;
    }
}
