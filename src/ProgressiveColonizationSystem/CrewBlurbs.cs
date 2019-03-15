using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static LingoonaGrammarExtensions;

namespace ProgressiveColonizationSystem
{
    public static class CrewBlurbs
    {
        internal static Random random = new Random();

        private static string[] SillyResourceNames = new string[] {
            "jalapenonite", "unobtanium", "kryptonite", "red hot hoolipidum ore", "indobodobinuminum",
            "only slightly radioactive plutonium", "electric boogalite", "orichalcum", "bobdobbsium" };

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

        internal static string ResourceLocated(double scannerNetQuality)
            => ResourceLocated(scannerNetQuality, GetCrewDescriptors(c => c.trait == "Pilot" || c.trait == "Geologist"),
                               FlightGlobals.ActiveVessel.vesselName);

        internal static string ResourceLocated(double ScannerNetQuality, List<CrewDescriptor> crew, string shipName)
        {
            CrewDescriptor perp = ChoosePerpetrator(crew);
            string resourceName = SillyResourceNames[random.Next(SillyResourceNames.Length - 1)];
            if (ScannerNetQuality == 0)
            {
                return $"{perp.Name} was told this would be an easy job - just click a button and the orbiting "
                     + $"satellites would tell {perp.himher} what the closest glop of stuff was.  But no.  Bill forgot to "
                     + $"put up any scanner satellites so {perp.Name} is gonna have to use manual override.  "
                     + $"Peering through the scope, there seems to be a shiny spot on the horizon...";
            }
            else
            {
                return $"Peering through the scope and randomly twisting the instrument's dials, {perp.Name} "
                     + $"has spotted a rich deposit of {resourceName}.  How those yoohoos down below are going "
                     + $"to get to it is strictly not {perp.hisher} problem.";
            }
        }

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
                        + "shape of a triange and sprinkles a lot of salt & MSG on it, then secretly places it in the "
                        + "real snack storage in bags that say 'DOITOS' on it, the crew will just pound down bag after bag "
                        + "of the stuff.";
                case TechTier.Tier3:
                    return $"While contemplating the incredible vastness of space and staring at yet another of the flat green "
                        + $"lumps that {GetGroupDescription(crew, true)} have been trying to pass off as 'chips' that "
                        + $"{victim.Name} surprised everyone with the simple answer:  They needed to turn some of the leftover "
                        + "goo into dip!  It's as simple as that!";
                case TechTier.Tier4:
                    return $"It's still green, it's still gooey, and it still smells a bit like armpit, but {GetGroupDescription(crew)} "
                          + $"have come to the grudging realization that {perp.Name}'s latest creation is likely to be as good as this "
                          + "goop will ever be.";
            }
        }

        internal static string BoringHydroponicBreakthrough(TechTier tier)
        {
            return $"You can now set the tier of hydroponic parts to {tier.DisplayName()} in the VAB.  You cannot "
                   + "retrofit or fix this part to be a higher tier than it is.  You can keep using it, but it won't "
                   + "be as effective as a new part and you can't do research with it.  The new part will be more effective - "
                   + "you can see just how much so in the VAB/SPH.  Agroponic research is limited by how close you are "
                   + "to Kerbin.  Your max tier is 2 near Kerbin; it can be researched up to 3 at orbits around one "
                   + "planet inside and outside of Kerbin's orbit and the final tier can only be unlocked beyond those "
                   + "planet's orbits.";
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
                    return $"It took a while, but {perp.Name} figured out how to get {GetGroupDescription(crew, false)} to "
                           + $"eat more of the crop - {perp.heshe} told the crew {perp.heshe} rerouted the greywater "
                           + "plumbing out of the farm module and into the drill coolant module instead, proving the "
                           + "point, it's not what you sell, but how you sell it that counts.";
                case TechTier.Tier2:
                case TechTier.Tier3:
                case TechTier.Tier4:
                    return $"{victim.Name} had been plotting against {perp.Name}'s scheme of creating a "
                           + $"hybrid of broccoli, okra, eggplant and prunes for weeks.  {perp.Name} knew something "
                           + $"was in the works; {perp.heshe} had seen how the rest of the crew had been looking at {perp.himher}. "
                           + "But no Kerbal could have anticipated the unexpectedly (and certainly unintentionally) synergistic "
                           + "effect that the fully weaponized robotic nematode had on the sickly mutated plants...\r\n\r\n"
                           + "A vegetable that tastes like chicken!";
            }
        }

        internal static string BoringFarmingBreakthrough(TechTier tier)
        {
            return $"You can now set the tier of farming parts to {tier.DisplayName()} in the VAB.  You cannot "
                   + "retrofit or fix this part to be a higher tier than it is.  You can keep using it, but it won't "
                   + "be as effective as a new part and you can't do research with it.  The new part will be more effective - "
                   + "you can see just how much so in the VAB/SPH.  Farming is a body-specific thing - this breakthrough "
                   + "only applies to the body you're landed at.  Farming also relies on fertilizer - basically you "
                   + "either have to generate an equal tier on the body you're at or bring it from home.";
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
                    return $"{perp.Name} was specially selected for this mission because it's a well known fact around "
                          + $"the astronaut complex that {perp.Name} really likes to look at dirt.  Great things were "
                          + $"expected of {perp.himher} from the beginning...  Maybe a little sooner than they actually "
                          + $"came, but the breakthrough finally happened.  Yes, {perp.Name} has conclusively proven "
                          + "that some of the dirt around here is better for fertilizer, some is better for making "
                          + "shiny things, and some is really only good for wiggling your toes in.";
                case TechTier.Tier2:
                case TechTier.Tier3:
                case TechTier.Tier4:
                    return $"Truly shocking!  It's been found that by lubricating the gearing in the fertilizer "
                         + $"plant with {perp.Name}'s toe cheese, production goes up 25%!";
            }
        }

        internal static string BoringProductionBreakthrough(TechTier tier)
        {
            return $"You can now set the tier of fertilizer plants and stuff scroungers to {tier.DisplayName()} in the VAB.  "
                   + "You cannot retrofit this part to be a higher tier than it is.  You can keep using it, but it won't "
                   + "be as effective as a new part and you can't do research with it.  The new part will be more effective - "
                   + "you can see just how much so in the VAB/SPH.  Production is a body-specific thing - this breakthrough "
                   + "only applies to the body you're landed at.";
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


        internal static string BoringScanningBreakthrough(TechTier tier)
        {
            return $"You can now set the tier of scanners to {tier.DisplayName()} in the VAB.  "
                   + "You cannot retrofit this part to be a higher tier than it is.  You can keep using it, but it won't "
                   + "be as effective as a new part and you can't do research with it.  The new part will be more effective - "
                   + "you can see just how much so in the VAB/SPH.  Scanning is a body-specific thing - this breakthrough "
                   + "only applies to the body you're landed at.";
        }


        public static string ShiniesBreakthrough(TechTier tier, Func<ProtoCrewMember, bool> isInstrumental)
            => ShiniesBreakthrough(GetCrewDescriptors(isInstrumental), FlightGlobals.ActiveVessel.lastBody.name, tier);

        internal static string ShiniesBreakthrough(List<CrewDescriptor> crew, string bodyName, TechTier tier)
        {
            CrewDescriptor perp = ChoosePerpetrator(crew);
            CrewDescriptor victim = ChooseVictim(crew);

            switch (tier)
            {
                default:
                case TechTier.Tier1:
                    return "Once again Duck-Tape is core to a breakthrough in space science! "
                           + $"In one of {perp.Name}'s more brilliant insights, it's been found that "
                           + "by duck-taping closed the doors to the KSP legal department, and then "
                           + "removing all those warning labels that say \"Danger!  May contain "
                           + "alien microbes that could wipe out all life on the planet!\" sales of "
                           + $"{bodyName}-Dirt facial scrub shot through the roof!";
                case TechTier.Tier2:
                case TechTier.Tier3:
                case TechTier.Tier4:
                    return $"Last night was a turning point.  {perp.Name} finally saw the curious formation "
                          + "at sector Zed Zed 9 Plural Zed Alpha really was just a pile of rocks. "
                          + $"Yes.  Certainly.  Just rocks.  Reluctantly, {perp.Name} recalibrated the instruments "
                          + "to actually look for that Stuff the guys on the surface and yeah.  Sure enough. "
                          + "There's stuff down there.  Great.  <sigh>";
            }
        }

        internal static string HungryKerbals(List<ProtoCrewMember> crewInBucket, double daysToGrouchy, bool anyFoodProduction)
            => HungryKerbals(crewInBucket.Select(k => FromKsp(k, _ => false)).ToList(), daysToGrouchy, anyFoodProduction);

        internal static string HungryKerbals(List<CrewDescriptor> crewInBucket, double daysToGrouchy, bool anyFoodProduction)
        {
            if (anyFoodProduction)
            {
                return Yellow($"{GetGroupDescription(crewInBucket)} can't make any snacks!  {capitalize(heshethey(crewInBucket))} can scrounge up old pizza crusts for {(int)(daysToGrouchy + .5)} more days.");
            }
            else
            {
                return $"{GetGroupDescription(crewInBucket)} {isare(crewInBucket)} surviving off a small stash of snacks; {heshethey(crewInBucket)} will be okay for {(int)(daysToGrouchy + .5)} more days.";
            }
        }

        internal static string GrumpyKerbals(List<ProtoCrewMember> crewInBucket, double daysToGrouchy, bool anyFoodProduction)
            => GrumpyKerbals(crewInBucket.Select(k => FromKsp(k, _ => false)).ToList(), daysToGrouchy, anyFoodProduction);

        internal static string GrumpyKerbals(List<CrewDescriptor> crewInBucket, double daysToGrouchy, bool anyFoodProduction)
        {
            if (anyFoodProduction)
            {
                return Yellow($"{GetGroupDescription(crewInBucket)} can't make any snacks!  {capitalize(heshethey(crewInBucket))} are starting to spend more and more time drawing up legal action against KSP than they are working.");
            }
            else
            {
                return Yellow($"{GetGroupDescription(crewInBucket)} {isare(crewInBucket)} can't find any more food!  {capitalize(heshethey(crewInBucket))} need to get home soon!");
            }
        }

        internal static string StarvingKerbals(List<ProtoCrewMember> crewInBucket)
            => StarvingKerbals(crewInBucket.Select(k => FromKsp(k, _ => false)).ToList());

        internal static string StarvingKerbals(List<CrewDescriptor> crewInBucket)
        {
            return Red($"{GetGroupDescription(crewInBucket)} {isare(crewInBucket)} refusing to do any more work and {isare(crewInBucket)} contemplating legal action against KSP!  Get {himherthem(crewInBucket)} home or get some food out here right away!");
        }

        internal static string BoringShiniesBreakthrough(TechTier tier)
        {
            return $"You can now set the tier of shinies to {tier.DisplayName()} in the VAB.  "
                   + "You cannot retrofit this part to be a higher tier than it is.  You can keep using it, but it won't "
                   + "be as effective as a new part and you can't do research with it.  The new part will produce the next "
                   + "tier of shinies, which are more valuable than this tier.  Shinies that are made near Kerbin aren't so "
                   + $"shiny - they're limited to {TechTier.Tier2.DisplayName()} on Kerbin's moons, 3 at bodies in easy "
                   + "reach of Kerbin, and can only get to 4 at difficult bodies";
        }

        public static string Red(string s)
            => $"<color #ff4040>{s}</color>";

        public static string  Yellow(string s)
            => $"<color #ffff00>{s}</color>";

        internal static string heshethey(List<CrewDescriptor> l)
            => (l.Count > 1) ? "they" : l[0].heshe;

        internal static string himherthem(List<CrewDescriptor> l)
            => (l.Count > 1) ? "them" : l[0].himher;

        internal static string isare(List<CrewDescriptor> l)
            => (l.Count == 1) ? "is" : "are";

        internal static string capitalize(string word)
            => $"{char.ToUpper(word[0])}{word.Substring(1)}";


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

            CrewDescriptor perp = ChooseOne(crew, null);
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

        public string himher
            => Gender == Gender.Male ? "him" : "her";
    }
}
