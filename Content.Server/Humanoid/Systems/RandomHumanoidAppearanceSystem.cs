using Content.Server.CharacterAppearance.Components;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Preferences;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow;

namespace Content.Server.Humanoid.Systems;

public sealed class RandomHumanoidAppearanceSystem : EntitySystem
{
    [Dependency] private readonly HumanoidAppearanceSystem _humanoid = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RandomHumanoidAppearanceComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(EntityUid uid, RandomHumanoidAppearanceComponent component, MapInitEvent args)
    {
        // If we have an initial profile/base layer set, do not randomize this humanoid.
        if (!TryComp(uid, out HumanoidAppearanceComponent? humanoid) || !string.IsNullOrEmpty(humanoid.Initial))
        {
            return;
        }

        var profile = HumanoidCharacterProfile.RandomWithSpecies(humanoid.Species);
        var appearance = profile.Appearance;

        List<Marking> markings;
        if (component.Markings != null)
            markings = MarkingsToAdd(component.Markings);
        else
            markings = appearance.Markings;


        var finalAppearance = new HumanoidCharacterAppearance(
            component.Hair ?? appearance.HairStyleId,
            component.HairColor ?? appearance.HairColor,
            component.FacialHair ?? appearance.FacialHairStyleId,
            component.HairColor ?? appearance.HairColor,
            component.EyeColor ?? appearance.EyeColor,
            component.SkinColor ?? appearance.SkinColor,
            markings
        );

        var finalProfile = new HumanoidCharacterProfile()
        {
            Name = profile.Name,
            Age = component.Age ?? profile.Age,
            Species = humanoid.Species,
            Appearance = finalAppearance
        }
        .WithSex(component.Sex ?? profile.Sex)
        .WithGender(component.Gender ?? profile.Gender);

        _humanoid.LoadProfile(uid, finalProfile, humanoid);

        if (component.RandomizeName)
            _metaData.SetEntityName(uid, profile.Name);
    }

    private List<Marking> MarkingsToAdd(Dictionary<string, List<Color>> dict)
    {
        List<Marking> output = [];
        foreach (var keyValuePair in dict)
        {
            var toAdd = new Marking(keyValuePair.Key, keyValuePair.Value);
            output.Add(toAdd);
        }
        return output;
    }
}
