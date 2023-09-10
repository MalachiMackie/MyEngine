using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MyEngine.SourceGenerator.Tests;

public class TemplateTests
{
    [Fact]
    public void Test()
    {
        var template = SourceTemplate.Load(
@"
    {template:MyTemplate}
");

        var replacement =
@"Hi
Hi
    Hi";

        template.SubstitutePart("MyTemplate", replacement);
        var result = template.Build();

        Assert.Equal(@"
    Hi
    Hi
        Hi
", result);
    }
}
