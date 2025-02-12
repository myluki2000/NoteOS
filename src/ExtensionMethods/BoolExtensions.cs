namespace NoteOS.ExtensionMethods;

public static class BoolExtensions {
    public static string ToString(this bool value) {
        return value ? "true" : "false";
    }
}