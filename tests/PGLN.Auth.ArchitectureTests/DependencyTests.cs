using NetArchTest.Rules;

namespace PGLN.Auth.ArchitectureTests;

public sealed class DependencyTests
{
    private static readonly System.Reflection.Assembly DomainAssembly =
        typeof(PGLN.Auth.Domain.AssemblyReference).Assembly;

    private static readonly System.Reflection.Assembly ApplicationAssembly =
        typeof(PGLN.Auth.Application.ApplicationAssemblyReference).Assembly;

    // ========================================================
    // Domain boundaries
    // ========================================================

    [Fact]
    public void Domain_Should_Not_Depend_On_Application()
    {
        AssertNoDependency(
            DomainAssembly,
            "PGLN.Auth.Application");
    }

    [Fact]
    public void Domain_Should_Not_Depend_On_Infrastructure()
    {
        AssertNoDependency(
            DomainAssembly,
            "PGLN.Auth.Infrastructure");
    }

    [Fact]
    public void Domain_Should_Not_Depend_On_EntityFrameworkCore()
    {
        AssertNoDependency(
            DomainAssembly,
            "PGLN.Auth.EntityFrameworkCore");
    }

    [Fact]
    public void Domain_Should_Not_Depend_On_AspNetCore()
    {
        AssertNoDependency(
            DomainAssembly,
            "PGLN.Auth.AspNetCore");
    }

    [Fact]
    public void Domain_Should_Not_Depend_On_Aws()
    {
        AssertNoDependency(
            DomainAssembly,
            "PGLN.Auth.Aws");
    }

    [Fact]
    public void Domain_Should_Not_Depend_On_Azure()
    {
        AssertNoDependency(
            DomainAssembly,
            "PGLN.Auth.Azure");
    }

    // ========================================================
    // Application boundaries
    // ========================================================

    [Fact]
    public void Application_Should_Not_Depend_On_Infrastructure()
    {
        AssertNoDependency(
            ApplicationAssembly,
            "PGLN.Auth.Infrastructure");
    }

    [Fact]
    public void Application_Should_Not_Depend_On_EntityFrameworkCore()
    {
        AssertNoDependency(
            ApplicationAssembly,
            "PGLN.Auth.EntityFrameworkCore");
    }

    [Fact]
    public void Application_Should_Not_Depend_On_AspNetCore()
    {
        AssertNoDependency(
            ApplicationAssembly,
            "PGLN.Auth.AspNetCore");
    }

    [Fact]
    public void Application_Should_Not_Depend_On_Aws()
    {
        AssertNoDependency(
            ApplicationAssembly,
            "PGLN.Auth.Aws");
    }

    [Fact]
    public void Application_Should_Not_Depend_On_Azure()
    {
        AssertNoDependency(
            ApplicationAssembly,
            "PGLN.Auth.Azure");
    }

    // ========================================================
    // Shared assertion
    // ========================================================

    private static void AssertNoDependency(
        System.Reflection.Assembly assembly,
        string forbiddenNamespace)
    {
        var result =
            Types
                .InAssembly(assembly)
                .ShouldNot()
                .HaveDependencyOn(forbiddenNamespace)
                .GetResult();

        Assert.True(
            result.IsSuccessful,
            $"Assembly '{assembly.GetName().Name}' must not depend on '{forbiddenNamespace}'.");
    }
}
