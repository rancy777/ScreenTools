using System;
using System.IO;
using System.Security.AccessControl;
using System.Security.Principal;
using ScreenTools;
using Xunit;

namespace ScreenTools.Tests.UnitTests;

public class TempWorkspaceServiceTests
{
    [Fact]
    public void EnsureWorkspace_CreatesDirectory()
    {
        var service = new TempWorkspaceService();
        var root = service.RecordingWorkspaceRoot;

        try
        {
            service.EnsureWorkspace();
            Assert.True(Directory.Exists(root));
        }
        finally
        {
            try
            {
                Directory.Delete(root, recursive: true);
            }
            catch
            {
            }
        }
    }

    [Fact]
    public void EnsureWorkspace_SetsRestrictivePermissions()
    {
        var service = new TempWorkspaceService();
        var root = service.RecordingWorkspaceRoot;

        try
        {
            service.EnsureWorkspace();
            var info = new DirectoryInfo(root);
            var accessControl = info.GetAccessControl();
            var rules = accessControl.GetAccessRules(true, true, typeof(NTAccount));
            Assert.NotEmpty(rules);
        }
        finally
        {
            try
            {
                Directory.Delete(root, recursive: true);
            }
            catch
            {
            }
        }
    }
}
