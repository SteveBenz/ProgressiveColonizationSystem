# Progressive Colonization System

This mod extends Kerbal Space Program with a simple life support mechanism and a production model.  The mod is
different from previous mods along the same vein in that it requires very little math to figure out what
your production rates will be, and it requires incremental exploration.

[See the forum thread](https://forum.kerbalspaceprogram.com/index.php?/topic/181852-15x~16x-progressivecolonizationsystem-life-support-colonization-for-more-fun-in-late-game/)
for the full story.


# Development plan

This mod is still in early days, with a few big parts missing.

1. (complete)  Hello World - figure out how to get a mod set up & compiled
2. (complete)  Minimum playability - the ability to launch Kerbals into space that eat food and complain when they don't get it.
   1.  (complete) The ability to research some new parts that do agriponics.
   2.  (complete) The ability to create a station in kerban's SOI and rank up the "Tiers" of agriponics
   3.  (complete) The ability for Kerbals to run out of supply and get grumpy (turn into Tourists)
   4.  (complete) Grumpy Kerbals will get happy again upon return to Kerban
   5.  (complete) Need appropriate crew to do agroponics and research
   6.  (complete) Can't do research on Agriponics T3 in Kerbin's SoI
3. (complete)Basic Progression - the ability to plan a mission to Duna
   1.  (complete) Kerbals can do agriculture on the Mun or Minmus
       1.  (complete) Adapt an Agriculture part from USI
       2.  (complete) Extend the research model to Agriculture & update the editor to show it
       3.  (complete) Can set target body in Editors for an agriculture part
   2.  (complete) Can change resource types for canisters
   3.  (complete) Can manufacture T0 fertilizer
4. Basic Progression - the execution of a mission to Duna
   1.  (complete) Editors can show good data to guide the construction of ships (see GetInfo())
   2.  (complete) Tweak the mass and density of Fertilizer & Supplies
   3.  (complete) Agroponic and Ag modules consume some electricity
   4.  (complete) Actually enforce the crew counts, types & stars (see 2.v)
   5.  (complete) Make the reporting of when you make a tier breakthrough more informative and fun
   6.  IDEA: Make it so that having achieved Tier-X farming on one world makes Tier-X-1 easier to achieve on a new world.
5. Release prep
   1.  (complete) De-fugly the dialogs
   2.  (complete) Get non-dumb name - Progressive Kolonization System?  PKS
   3.  (complete) Figure out how to Release
   4.  (complete) Fix .version file
   5.  (complete) Release to CKAN
   6.  Automate release preparation
6. Advanced Progression - the ability to have extended missions on Duna
   1.  Create the resource gathering mechanic
       1.  (complete) Create a configurable part for storing Stuff
       2.  (complete) Create a KSP Resource for Tiered Stuff resource
       3.  (complete) Readjust the numbers
       4.  (complete)Create a scanning lab (using skin from infrared telescope?)
       5.  (complete)Figure out how to make magic spots and waypoints to them
       6.  (complete)Make the Tier-1+ drill require being at a magic spot to work
       7.  (complete)Actually require that a scanner of the appriate tier be in orbit
       8.  (complete)Rename to "Crush-ins", "Loose Crush-ins"
       9.  (complete)Be able to transfer Crushins to a base
       10. (complete)Make the base able to consume them to make stuff
       11. Put the find resource thing on the scanner
       12. The Crush-Ins warning should be more explicit about what's needed
       13. The Trip-Duration calculator should not be limited by Crush-Ins
       14. Make the scanner require a network of scanner satelites
       15. Make nodes disappear after time or something?
       16. Manually transfer Snacks-4 - "Grab Snacks" "Give Snacks"
   2.  Create shinies resource chain
       1.  (complete) Create shinies factory
       2.  (complete) Create a basic shinies container
       3.  (complete) Create shinies containers that don't let you fill them at the KSP
       4   (complete) Mun/Minmus shinies peak at T1, Duna/Ike/Gilly at T2, T5 only at Dres/Eloo/Moho/Eve
       5.  Make the shinies boost your rep on landing
       6.  Re-implement the checks to make sure Shinies don't actually come from Kerbin
   3.  (complete) Support USI kerbal types
   4.  (complete) Life support dialog works nicely in the editors - can estimate usage and ag production
   5.  (complete) Make the GetInfo()'s show the electric utilization
   6.  (complete) Support some form of resource transfer (resupply / salvage)
7. Play Nice With Others
   1.  Integrate with Extraplanetary Launchpads
   2.  Integrate with Kerbal Alarm Clock?
   3.  (cut - sticking with the game one) Integrate with Toolbar?
   4.  (complete) Integrate with ModuleManager
   5.  (complete) Integrate with Community Resource Kit?
   6.  (complete) Integrate with Community Categories?
   7.  (complete) Integrate with the other base parts mod whose name I forget?
   8.  (complete) Either reskin the stolen parts or depend on their sources and re-configure them
   9.  Write PDF help

   
Rambling About The Numbers

There are a number of bits that need to be tweaked with care.  The following or more or less
a justification of the current state of affairs.

When looking at resources, there's an arbitrary "Unit" - which is a number that's just made up.
Each resource also has a density, which is defined in Resources.cfg, and a unitsPerVolume, that
is used by B9 in TankTypes (PKS-B9TankTypes.cfg).  The other numbers to think about are the crew
requirements (in the PksRequiredCrew module descriptors) and the capacity of the part (in
PksTieredResourceConverter).

A "Unit" is a completely arbitrary unit of measure, and so it's chosen to be the amount of snacks
a Kerbal will consume in a day so that players can get easily eyeball how much stuff to bring.
Similarly, a "Unit" of "Fertilizer" is enough fertilizer to make one unit of snacks.

For Hydroponics, the idea is that the Kerbals' waste is being fed back into the system and
so fertilizer, whatever it is, weighs a small fraction of what a snack does, unit-for-unit.

So, let's make up some numbers:
   
1.  A kerbal consumes 2kg (roughly 4.4 pounds) of snacks per day.
2.  The KRDA-O200 Cargo Container is about as big as maybe 1.5 Mk1 command pods, and I'm
    eyeballing it as being able to hold about 95 days worth of food.  That puts its mass
    at 190kg or .2t.  By comparison, the FL-T200 Fuel Tank is roughly the same volume,
    and it carries 1t of fuel.  That seems a bit awkward at first, but LO2 is a little
    bit denser than water, which is plenty dense, so the number seems workable.
3.  The KRDA-O200 Cargo Container has a base volume of 950 units (which look to be about
    a liter which is 1/1000 of a cubic meter).   If we want to have it fit 95 units,
    that means we need 10 units of volume (liters) for each snack.  That means the
    B9_TANK_TYPE.Resource[name=Snacks*].unitsPerVolume needs to be .1.  The 10liters
    per day thing seems a bit steep, but really the problem is mainly that the cargo
    container would need to have a 
4. As stated, density, RESOURCE_DEFINITION[name=Snacks*].density, is in tons/unit, so
    it's just 2kg*.001ton/kg or .002t/unit

And if we ballpark fertilizer as being a tenth the mass of a snack, and, for entertainments
sake, twice as dense, then its "density" is .0002t/unit and its unitsPerVolume is 10*2*.1 = 2.

The density of the supplies is a bit low, but I think the fix, if one were to make it would
be to modify the tank so that it's got less interior space (insulation, e.g.).  I don't
know for sure how to do that with tank configurations today, but perhaps someday...

Now, as to the labor:  It seems like it ought to take no more than 1 kerbal in 4 to produce
the food.  Also, just by eyeball, the parts that we get in StationPartsExpansionRedux all
look to be about the same size and look to me like it ought to support 2 kerbals each.
So, on the surface of things, that'd seem like we should put the "capacity" of those parts
at 2, and the crew requirement at .5.

Practically speaking, the way the math works out, at early tiers the module can support
almost 5x as many kerbals as it does at the max tier, because each kerbal eats way less of
the produce.

At this point, the best answer seems to just let that ride, even if it does mean that parts
become more labor-intensive as they mature.

For farming, we want to make it about twice as productive so that the crew can produce enough
food while landed to take with them on the return trip.  The planetary greenhouse that comes
from PlanetaryBaseInc looks the part, but it's kindof small looking, so it gets the same
productivity as the greenhouse parts but takes half the manpower.

The drill is kindof a strange egg for the base because in mid-to-late game, we want it to be
sortof a magical part because we want a very small number of kerbals to go out in a rover and
pick some stuff up without having to make a big annoying multi-day time-warp with all the
hassles of the day-night-cycle electrics in KSP.  It would also be kindof nutty if the mass
of the mined stuff was less than the mass of the final product.  Yet we don't want to have
to have hugely massive loads of ore traveling around either.

Best way forward I can see is that there's basic dirt that gets mined up around the base and
special sauce that you go and get.  If you go that way, then you don't need to store what the
drill produces at all, and so you don't have to care what its mass & density are.

For the Shinies factory, it doesn't matter at all what number we choose since we can fudge
for whatever mass and value later.  Since the numbers are so arbitrary, we might as well say
that the productivity of shinies factories equals fertilizer factories.  For no good reason,
we can declare the shines to have half the volume and twice the density of snacks.  Value is
the more important measure, but still, there's no real money problem in KSP, so we might as
well be generous.  The average ship costs ~120000 credits, and if you made an effort to
create shinies at all, you could make 1200 in a mission, so we could call T0 100 cash per
shiny at Tier-0 and ramp it up by doubling it at each tier.  If you take the time to haul a mess
of T5 shinies, you deserve to get paid, I suppose.

