using Content.Client.Message;
using Content.Shared._Impstation.Cosmiccult;
using Content.Shared._Impstation.CosmicCult.Components;
using Content.Shared._Impstation.CosmicCult.Prototypes;
using Robust.Client.AutoGenerated;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;

namespace Content.Client._Impstation.CosmicCult.UI.Monument;
[GenerateTypedNameReferences]
public sealed partial class InfluenceUIBox : BoxContainer
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    private readonly SpriteSystem _sprite;

    public Action? OnGainButtonPressed;

    public enum InfluenceUIBoxState
    {
        UnlockedAndEnoughEntropy = 0,
        UnlockedAndNotEnoughEntropy = 1,
        Owned = 2,
        Locked = 3,
    }

    public readonly InfluenceUIBoxState State;
    public readonly InfluencePrototype Proto;

    public InfluenceUIBox(InfluencePrototype influenceProto, InfluenceUIBoxState state, MonumentBuiState monumentState)
    {
        RobustXamlLoader.Load(this);
        IoCManager.InjectDependencies(this);
        _sprite = _entityManager.System<SpriteSystem>();
        GainButton.StyleClasses.Add("ButtonColorPurpleAndCool");

        InfluenceIcon.Texture = _sprite.Frame0(influenceProto.Icon);
        Name.Text = Loc.GetString(influenceProto.Name);

        State = state;
        Proto = influenceProto;

        var availableEntropy = 0;
        if (_entityManager.TryGetComponent<CosmicCultComponent>(_playerManager.LocalEntity, out var cultComp)) //this feels wrong but seems to be the correct way to do this?
        {
            availableEntropy = cultComp.EntropyBudget;
        }

        switch (state)
        {
            case InfluenceUIBoxState.Owned:
                Status.Text = Loc.GetString("monument-interface-influences-owned");

                GainButton.Disabled = true;
                GainButton.Modulate = Color.Green;
                GainButton.Label.Text = Loc.GetString("monument-interface-influences-purchased");
                GainButton.ToolTip = Loc.GetString("monument-interface-influences-owned-tooltip");

                break;

            case InfluenceUIBoxState.UnlockedAndEnoughEntropy:
                Status.Text = Loc.GetString("monument-interface-influences-unlocked");

                GainButton.Disabled = false;

                break;

            case InfluenceUIBoxState.UnlockedAndNotEnoughEntropy:
                Status.Text = Loc.GetString("monument-interface-influences-unlocked");

                GainButton.Disabled = false;
                GainButton.Modulate = Color.Gray;
                GainButton.ToolTip = Loc.GetString("monument-interface-influences-unlocked-not-enough-entropy-tooltip", ("entropy", influenceProto.Cost - availableEntropy));
                break;

            case InfluenceUIBoxState.Locked:
                Status.Text = Loc.GetString("monument-interface-influences-locked");
                Status.FontColorOverride = Color.White;

                GainButton.Disabled = true;
                GainButton.Modulate = Color.Gray;
                GainButton.Label.Text = Loc.GetString("monument-interface-influences-locked");
                GainButton.ToolTip = Loc.GetString("monument-interface-influences-locked-tooltip");

                Name.FontColorOverride = Color.White;

                InfluenceBox.Modulate = Color.Gray;
                InfluenceIcon.Modulate = Color.Gray;

                Description.Modulate = Color.Gray;

                Type.Modulate = Color.Gray;

                CostText.Modulate = Color.Gray;

                Cost.FontColorOverride = Color.Gray;

                break;
        }

        Type.Text = Loc.GetString(influenceProto.InfluenceType);
        Cost.Text = influenceProto.Cost.ToString();
        Description.SetMarkup(Loc.GetString(influenceProto.Description));

        GainButton.OnPressed += _ => OnGainButtonPressed?.Invoke();
    }
}
