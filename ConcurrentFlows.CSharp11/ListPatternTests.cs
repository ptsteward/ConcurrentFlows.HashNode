using FluentAssertions;

namespace ConcurrentFlows.CSharp11;

public class ListPatternTests
{
    [Fact]
    public void Match_Discard_Pattern()
    {
        var seq = new[] { 1, 2 };
        var result = seq switch
        {
            [_, 2] => true,
            _ => false
        };
        result.Should().BeTrue();
    }

    [Fact]
    public void Match_Slice_Pattern()
    {
        var seq = new[] { 1, 2 };
        var result = seq switch
        {
            [..] => true,
            _ => false
        };
        result.Should().BeTrue();
    }

    [Fact]
    public void Match_Relational_Pattern()
    {
        var seq = new[] { 1, 2 };
        var result = seq switch
        {
            [_, > 1] => true,
            _ => false
        };
        result.Should().BeTrue();
    }

    [Fact]
    public void Match_Relational_Logical_Pattern()
    {
        var seq = new[] { 1, 2 };
        var result = seq switch
        {
            [_, > 1 and < 3] => true,
            _ => false
        };
        result.Should().BeTrue();
    }

    [Fact]
    public void Match_Positional_Pattern()
    {
        var seq = new Person[]
        {
    new("Mike", 23),
    new("Josh", 67)
        };
        var result = seq switch
        {
            [_, ("Josh", _)] => true,
            _ => false
        };
        result.Should().BeTrue();
    }

    [Fact]
    public void Match_Property_Pattern()
    {
        var seq = new Person[]
        {
    new("Mike", 23),
    new("Josh", 67)
        };
        var result = seq switch
        {
            [_, { Age: > 18 }] => true,
            _ => false
        };
        result.Should().BeTrue();
    }

    [Fact]
    public void Match_Var_Pattern()
    {
        var seq = new Person[]
        {
    new("Mike", 23),
    new("Josh", 67)
        };
        var result = seq switch
        {
            [_, var person] => person,
            _ => null
        };
        result.Should().BeEquivalentTo(seq[1]);
    }
}

public sealed record Person(string Name, int Age);