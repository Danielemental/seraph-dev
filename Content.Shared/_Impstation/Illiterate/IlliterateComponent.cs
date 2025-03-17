namespace Content.Shared._Impstation.Illiterate;

/// <summary>
///     Causes the owner of this component to be unable to read or write on paper.
/// </summary>
[RegisterComponent]
public sealed partial class IlliterateComponent : Component
{
    [DataField]
    public LocId FailMsg = "illiterate-default-msg";
};