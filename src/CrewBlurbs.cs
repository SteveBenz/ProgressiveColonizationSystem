using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static LingoonaGrammarExtensions;

namespace Nerm.Colonization
{
    public static class CrewBlurbs
    {
        internal static Random random = new Random();

        public static List<CrewDescriptor> GetCrewDescriptors(Func<ProtoCrewMember, bool> isInstrumental)
            => FlightGlobals.ActiveVessel.GetVesselCrew().Select(c => FromKsp(c, isInstrumental)).ToList();

        private static CrewDescriptor FromKsp(ProtoCrewMember kspCrew, Func<ProtoCrewMember, bool> isInstrumental)
            => new CrewDescriptor()
            {
                Name = FamiliarName(kspCrew),
                Gender = (Gender)kspCrew.gender, // <- works out (probably by design) that the KSP gender enum mappable in this way.
                IsInstrumental = isInstrumental(kspCrew),
                IsBadass = kspCrew.isBadass
            };

        private static string FamiliarName(ProtoCrewMember kspCrew)
            => kspCrew.name.EndsWith(" Kerman") ? kspCrew.name.Substring(0, kspCrew.name.Length - 7) : kspCrew.name;

        public static string HydroponicBreakthrough(TechTier tier, Func<ProtoCrewMember, bool> isInstrumental)
            => HydroponicBreakthrough(GetCrewDescriptors(isInstrumental), FlightGlobals.ActiveVessel.vesselName, tier);

        internal static string HydroponicBreakthrough(List<CrewDescriptor> crew, string shipName, TechTier tier)
        {
            CrewDescriptor perp = ChoosePerpetrator(crew);
            CrewDescriptor victim = ChooseVictim(crew);

            switch (tier)
            {
                default:
                case TechTier.Tier1:
                    switch (random.Next(2))
                    {
                        case 0:
                            return $"After months of scheming, {victim.Name} finally figured it out.  If they just act like they "
                                  + "like this green mush, and then sell it - I mean really sell it - to the gang at Mission Control, "
                                  + "there's a really solid chance they'll bring them home and let them have pizza again.";
                        default:
                            return $"It was while trying {perp.Name}'s latest creation, Greenish-Goo Surprise, that {victim.Name} "
                                   + $"found that by duck-taping {victim.hisher} nose closed, the stuff was a whole lot easier to get down.  "
                                   + "Thus paving the way to a new era in interplanetary cuisine!";
                    }
                case TechTier.Tier2:
                    return $"{perp.Name} has cracked the code!  If {perp.heshe} just takes the green goo, and then molds it into the "
                        + "shape of a dorito and sprinkles a lot of salt & MSG on it, then secretly places it in the "
                        + "real snack storage in bags that say 'DOITOS' on it, the crew will just pound down bag after bag "
                        + "of the stuff.";
                case TechTier.Tier3:
                    return $"While contemplating the incredible vastness of space and staring at yet another of the green "
                        + $"lumps that {GetGroupDescription(crew, true)} have been trying to pass off as 'chips' that "
                        + $"{victim.Name} surprised everyone with the simple answer:  They needed to turn some of the leftover "
                        + "goo into dip!  It's as simple as that!";
                case TechTier.Tier4:
                    return $"It's still green, it's still gooey, and it still smells a bit like armpit, but {GetGroupDescription(crew)} "
                          + $"have come to the grudging realization that {perp.Name}'s latest creation is likely to be as good as this "
                          + "goop will ever be.";
            }
        }

        public static string FarmingBreakthrough(TechTier tier, Func<ProtoCrewMember, bool> isInstrumental)
            => FarmingBreakthrough(GetCrewDescriptors(isInstrumental), FlightGlobals.ActiveVessel.vesselName, tier);

        internal static string FarmingBreakthrough(List<CrewDescriptor> crew, string shipName, TechTier tier)
        {
            CrewDescriptor perp = ChoosePerpetrator(crew);
            CrewDescriptor victim = ChooseVictim(crew);

            switch (tier)
            {
                default:
                case TechTier.Tier1:
                case TechTier.Tier2:
                case TechTier.Tier3:
                case TechTier.Tier4:
                    return $"It took a while, but {perp.Name} figured out how to get {GetGroupDescription(crew, false)} to "
                           + $"eat more of the crop - {perp.heshe} told the crew {perp.heshe} rerouted the greywater "
                           + "plumbing out of the farm module and into the drill coolant module instead, proving the "
                           + "point, it's not what you sell, but how you sell it that counts.";
            }
        }

        public static string ProductionBreakthrough(TechTier tier, Func<ProtoCrewMember, bool> isInstrumental)
            => ProductionBreakthrough(GetCrewDescriptors(isInstrumental), FlightGlobals.ActiveVessel.GetDisplayName(), tier);

        internal static string ProductionBreakthrough(List<CrewDescriptor> crew, string shipName, TechTier tier)
        {
            CrewDescriptor perp = ChoosePerpetrator(crew);
            CrewDescriptor victim = ChooseVictim(crew);

            switch (tier)
            {
                default:
                case TechTier.Tier1:
                case TechTier.Tier2:
                case TechTier.Tier3:
                case TechTier.Tier4:
                    return $"Truly shocking!  It's been found that by lubricating the gearing in the fertilizer "
                         + $"plant with {perp.Name}'s toe cheese, production goes up 25%!";
            }
        }

        public static string ScanningBreakthrough(TechTier tier, Func<ProtoCrewMember, bool> isInstrumental)
            => ScanningBreakthrough(GetCrewDescriptors(isInstrumental), FlightGlobals.ActiveVessel.vesselName, tier);

        internal static string ScanningBreakthrough(List<CrewDescriptor> crew, string shipName, TechTier tier)
        {
            CrewDescriptor perp = ChoosePerpetrator(crew);
            CrewDescriptor victim = ChooseVictim(crew);

            switch (tier)
            {
                default:
                case TechTier.Tier1:
                case TechTier.Tier2:
                case TechTier.Tier3:
                case TechTier.Tier4:
                    return $"At first it was thought to be caused by the cosmic background noise, and then "
                           + $"harmonic distortion from sunspots on Kerbal.  But {perp.Name} knew better.  "
                           + $"After months of painstaking study and analysis it was determined that "
                           + "it was, in fact, a radio signal, beamed from a distant planet in the alpha quadrant "
                           + "at roughly 4-day intervals, an hour at a time, which, when carefully and "
                           + $"meticulously decoded and interpreted by {perp.Name} with help from the entire KSP staff "
                           + "was a documentary series entitled 'The A-Team'.";
            }
        }

        public static string ShiniesBreakthrough(TechTier tier, Func<ProtoCrewMember, bool> isInstrumental)
            => ShiniesBreakthrough(GetCrewDescriptors(isInstrumental), FlightGlobals.ActiveVessel.landedAt, tier);

        internal static string ShiniesBreakthrough(List<CrewDescriptor> crew, string bodyName, TechTier tier)
        {
            CrewDescriptor perp = ChoosePerpetrator(crew);
            CrewDescriptor victim = ChooseVictim(crew);

            switch (tier)
            {
                default:
                case TechTier.Tier1:
                case TechTier.Tier2:
                case TechTier.Tier3:
                case TechTier.Tier4:
                    return "Once again Duck-Tape is core to a breakthrough in space science! "
                           + $"In one of {perp.Name}'s more brilliant insights, it's been found that "
                           + "by duck-taping closed the doors to the KSP legal department, and then "
                           + "removing all those warning labels that say \"Danger!  May contain "
                           + "alien microbes that will wipe out all life on the planet!\" sales of "
                           + $"{bodyName}-dirt facial scrub shot through the roof!";
            }
        }


        public static CrewDescriptor ChoosePerpetrator(List<CrewDescriptor> crew)
            => ChooseOne(crew, true);

        public static CrewDescriptor ChooseVictim(List<CrewDescriptor> crew)
            => ChooseOne(crew, false);

        private static CrewDescriptor ChooseOne(List<CrewDescriptor> crew, bool? isInstrumental)
        {
            // Try to find a badass
            if (random.Next(10) > 0)
            {
                List<CrewDescriptor> badAssPerps = crew.Where(c => c.IsBadass && (!isInstrumental.HasValue || c.IsInstrumental == isInstrumental.Value)).ToList();
                if (badAssPerps.Count > 0)
                {
                    return badAssPerps[random.Next(badAssPerps.Count)];
                }
            }

            List<CrewDescriptor> allPerps = crew.Where(c => !isInstrumental.HasValue || c.IsInstrumental == isInstrumental.Value).ToList();
            return allPerps[random.Next(allPerps.Count)];
        }

        public static string GetGroupDescription(List<CrewDescriptor> crew, bool? isInstrumental)
            => GetGroupDescription(crew.Where(c => !isInstrumental.HasValue || c.IsInstrumental == isInstrumental.Value).ToList());

        public static string GetGroupDescription(List<CrewDescriptor> crew)
        {
            if (crew.Count == 1)
            {
                return crew[0].Name;
            }

            CrewDescriptor perp = ChoosePerpetrator(crew);
            List<CrewDescriptor> gang = crew.Where(c => c != perp).ToList();
            if (crew.Count == 2)
            {
                return $"{perp.Name} and {gang[0].Name}";
            }
            else if (gang.All(c => c.Gender == Gender.Male))
            {
                return $"{perp.Name} and the boys";
            }
            else if (gang.All(c => c.Gender == Gender.Female))
            {
                return $"{perp.Name} and the gals";
            }
            else
            {
                return $"{perp.Name} and the gang";
            }
        }
    }

    public class CrewDescriptor
    {
        public string Name;

        public Gender Gender;

        /// <summary>
        ///   True if the crew member is somehow instrumental in what's happening
        /// </summary>
        public bool IsInstrumental;

        /// <summary>
        ///   True if this is a well-known kerbal
        /// </summary>
        public bool IsBadass;

        public string hisher
            => Gender == Gender.Male ? "his" : "her";

        public string heshe
            => Gender == Gender.Male ? "he" : "she";
    }
}
