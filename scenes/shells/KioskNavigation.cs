using FreeRehabHub.Domain;

namespace FreeRehabHub.App.Shells;

public static class KioskNavigation
{
    public const string TherapistShellScenePath = "res://scenes/shells/TherapistShell.tscn";
    public const string ChildKioskShellScenePath = "res://scenes/shells/ChildKioskShell.tscn";

    public static string ResolveHomeScenePath(UserRole role)
    {
        return role == UserRole.Child ? ChildKioskShellScenePath : TherapistShellScenePath;
    }
}
