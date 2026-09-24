namespace JiwaMcpServer.Agent.Catalog;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class PlannerDomainAttribute(string name) : Attribute
{
    public string Name { get; } = name;
}
