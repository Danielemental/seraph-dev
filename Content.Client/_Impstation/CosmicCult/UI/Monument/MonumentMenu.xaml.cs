using System.Linq;
using System.Numerics;
using Content.Client.Stylesheets;
using Content.Client.UserInterface.Controls;
using Content.Shared._Impstation.CCVar;
using Content.Shared._Impstation.Cosmiccult;
using Content.Shared._Impstation.CosmicCult.Components;
using Content.Shared._Impstation.CosmicCult.Prototypes;
using Robust.Client.AutoGenerated;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client._Impstation.CosmicCult.UI.Monument;
[GenerateTypedNameReferences]
public sealed partial class MonumentMenu : FancyWindow
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IConfigurationManager _config = default!;

    private readonly SpriteSystem _sprite;

    // All glyph prototypes
    private readonly IEnumerable<GlyphPrototype> _glyphPrototypes;
    // All influence prototypes
    private readonly IEnumerable<InfluencePrototype> _influencePrototypes;
    private readonly ButtonGroup _glyphButtonGroup;
    private ProtoId<GlyphPrototype> _selectedGlyphProtoId = string.Empty;
    private HashSet<ProtoId<GlyphPrototype>> _unlockedGlyphProtoIds = [];
    public Action<ProtoId<GlyphPrototype>>? OnSelectGlyphButtonPressed;
    public Action? OnRemoveGlyphButtonPressed;

    public Action<ProtoId<InfluencePrototype>>? OnGainButtonPressed;

    private int _influenceCount = default;

    public MonumentMenu()
    {
        RobustXamlLoader.Load(this);
        IoCManager.InjectDependencies(this);
        _sprite = _entityManager.System<SpriteSystem>();

        // Set the protos. These do not dynamically update so we can just store them right off the bat.
        // If an admin adds a new one in the middle of the round. Too bad*!
        // * You could probably just do this in UpdateState if that is necessary
        _glyphPrototypes = _prototypeManager.EnumeratePrototypes<GlyphPrototype>();
        _influencePrototypes = _prototypeManager.EnumeratePrototypes<InfluencePrototype>();

        _glyphButtonGroup = new ButtonGroup();

        RemoveGlyphButton.OnPressed += _ => OnRemoveGlyphButtonPressed?.Invoke();
        SelectGlyphButton.OnPressed += _ => OnSelectGlyphButtonPressed?.Invoke(_selectedGlyphProtoId);
    }

    /// <remarks>
    /// This is here due to a wierd thing that would happpen when MonumentTier2 or MonumentTier3 would get called from CosmicCultRuleSystem where the BUI state would get processed by the client before the component state.
    /// This fixes it by simply brute-force refreshing the UI if the relevant fields in the comp change. not super clean but It Works:tm:.
    /// </remarks>
    protected override void FrameUpdate(FrameEventArgs args)
    {
        if (!_entityManager.TryGetComponent<CosmicCultComponent>(_playerManager.LocalEntity, out var cultComp))
            return;

        //rebuild the list of unlocked influences if the count changes between frames
        if (cultComp.UnlockedInfluences.Count != _influenceCount)
        {
            UpdateInfluences();
        }

        //update this every frame because why not
        AvailableEntropy.Text = Loc.GetString("monument-interface-entropy-value", ("infused", cultComp.EntropyBudget.ToString()));
    }

    public void UpdateState(MonumentBuiState state)
    {
        _selectedGlyphProtoId = state.SelectedGlyph;
        _unlockedGlyphProtoIds = state.UnlockedGlyphs;

        CultProgressBar.BackgroundStyleBoxOverride = new StyleBoxFlat { BackgroundColor = new Color(15, 17, 30) };
        CultProgressBar.ForegroundStyleBoxOverride = new StyleBoxFlat { BackgroundColor = new Color(91, 62, 124) };

        SelectGlyphButton.StyleClasses.Add("ButtonColorPurpleAndCool");
        RemoveGlyphButton.StyleClasses.Add("ButtonColorPurpleAndCool");

        UpdateBar(state);
        UpdateEntropy(state);
        UpdateGlyphs();
        UpdateInfluences();
    }

    // Update all the entropy fields
    private void UpdateBar(MonumentBuiState state)
    {
        var percentComplete = ((float) state.CurrentProgress / (float) state.TargetProgress) * 100f; //too many parenthesis & probably unnecessary float casts but I'm not taking any chances

        percentComplete = Math.Min(percentComplete, 100);

        CultProgressBar.Value = percentComplete;

        ProgressBarPercentage.Text = Loc.GetString("monument-interface-progress-bar", ("percentage", percentComplete.ToString("0")));
    }

    // Update all the entropy fields
    private void UpdateEntropy(MonumentBuiState state)
    {
        var availableEntropy = "thinking emoji"; //if you see this, problem.
        if (_entityManager.TryGetComponent<CosmicCultComponent>(_playerManager.LocalEntity, out var cultComp))
        {
            availableEntropy = cultComp.EntropyBudget.ToString();
        }

        var entropyToNextStage = Math.Max(state.TargetProgress - state.CurrentProgress, 0);
        var min = entropyToNextStage == 0 ? 0 : 1; //I have no idea what to call this. makes it so that it shows 0 crew for the final stage but at least one at all other times
        var crewToNextStage = (int) Math.Max(Math.Round((double) entropyToNextStage / _config.GetCVar(ImpCCVars.CosmicCultistEntropyValue), MidpointRounding.ToPositiveInfinity), min); //force it to be at least one

        AvailableEntropy.Text = Loc.GetString("monument-interface-entropy-value", ("infused", availableEntropy));
        EntropyUntilNextStage.Text = Loc.GetString("monument-interface-entropy-value", ("infused", entropyToNextStage.ToString()));
        CrewToConvertUntilNextStage.Text = crewToNextStage.ToString();
    }

    // Update all the glyph buttons
    private void UpdateGlyphs()
    {
        var glyphs = _glyphPrototypes.ToList();
        glyphs.Sort((x, y) =>
            string.Compare(x.Name, y.Name, StringComparison.CurrentCultureIgnoreCase));

        GlyphContainer.RemoveAllChildren();
        foreach (var glyph in glyphs)
        {
            var boxContainer = new BoxContainer();
            var unlocked = _unlockedGlyphProtoIds.Contains(glyph.ID);
            var button = new Button
            {
                HorizontalExpand = true,
                StyleClasses = { StyleBase.ButtonSquare },
                ToolTip = Loc.GetString(glyph.Tooltip),
                Group = _glyphButtonGroup,
                Pressed = glyph.ID == _selectedGlyphProtoId,
                Disabled = !unlocked,
                Modulate = !unlocked ? Color.Gray : Color.White,
            };
            button.OnPressed += _ => _selectedGlyphProtoId = glyph.ID;
            var glyphIcon = new TextureRect
            {
                Texture = _sprite.Frame0(glyph.Icon),
                TextureScale = new Vector2(2f, 2f),
                Stretch = TextureRect.StretchMode.KeepCentered,
            };
            boxContainer.AddChild(button);
            button.AddChild(glyphIcon);
            GlyphContainer.AddChild(boxContainer);
        }
    }

    // Update all the influence thingies
    private void UpdateInfluences()
    {
        InfluencesContainer.RemoveAllChildren();

        var influenceUIBoxes = new List<InfluenceUIBox>();
        foreach (var influence in _influencePrototypes)
        {
            var uiBoxState = GetUIBoxStateForInfluence(influence);
            var influenceBox = new InfluenceUIBox(influence, uiBoxState);
            influenceUIBoxes.Add(influenceBox);
            influenceBox.OnGainButtonPressed += () => OnGainButtonPressed?.Invoke(influence.ID);
        }

        //sort the list of UI boxes by state (locked -> owned -> not enough entropy -> enough entropy)
        //then sort alphabetically within those categories
        influenceUIBoxes = influenceUIBoxes.OrderBy(box => box.State)
            .ThenBy(box => box.Proto.ID)
            .ToList();

        foreach (var box in influenceUIBoxes)
        {
            InfluencesContainer.AddChild(box);
        }

        //I probably shouldn't be doing so many duplicate tryComps, but I can't think of a good way to have individual "no cult comp" responses w/ only getting it in one place
        //not a massive issue since this doesn't run often but eh, it's kinda wierd
        if (_entityManager.TryGetComponent<CosmicCultComponent>(_playerManager.LocalEntity, out var cultComp)) //this feels wrong but seems to be the correct way to do this?
            _influenceCount = cultComp.UnlockedInfluences.Count; //early return with locked if there's somehow no cult comp
    }

    private InfluenceUIBox.InfluenceUIBoxState GetUIBoxStateForInfluence(InfluencePrototype influence)
    {
        if (!_entityManager.TryGetComponent<CosmicCultComponent>(_playerManager.LocalEntity, out var cultComp)) //this feels wrong but seems to be the correct way to do this?
            return InfluenceUIBox.InfluenceUIBoxState.Locked; //early return with locked if there's somehow no cult comp

        var unlocked = cultComp.UnlockedInfluences.Contains(influence.ID);
        var owned = cultComp.OwnedInfluences.Any(ownedProtoId => ownedProtoId.Id.Equals(influence.ID));

        //more verbose than it needs to be, but it reads nicer
        if (owned)
        {
            return InfluenceUIBox.InfluenceUIBoxState.Owned;
        }

        if (unlocked)
        {
            //if it's unlocked, do we have enough entropy to buy it?
            return influence.Cost > cultComp.EntropyBudget ? InfluenceUIBox.InfluenceUIBoxState.UnlockedAndNotEnoughEntropy : InfluenceUIBox.InfluenceUIBoxState.UnlockedAndEnoughEntropy;
        }

        return InfluenceUIBox.InfluenceUIBoxState.Locked;
    }
}
